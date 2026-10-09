// One Durable Object per game room: a dumb relay that mimics the parts of Unity's legacy RakNet
// networking the game relied on (see docs/MULTIPLAYER.md for the wire protocol).
import { DurableObject } from "cloudflare:workers";

// client -> relay
const C_RPC = 1, C_INST = 2, C_DESTROY = 3, C_REMOVE_RPCS = 4, C_DESTROY_PLAYER = 5, C_KICK = 6, C_PING = 7, C_LISTING = 8;
// relay -> client
const S_WELCOME = 0x81, S_RPC = 0x82, S_INST = 0x83, S_DESTROY = 0x84, S_CONNECTED = 0x85, S_DISCONNECTED = 0x86,
  S_REJECT = 0x87, S_PONG = 0x88, S_PING_INFO = 0x89, S_SYNC_DONE = 0x8a;
// reject / close reasons
export const R_NOT_FOUND = 1, R_BAD_PASSWORD = 2, R_FULL = 3, R_HOST_LEFT = 4, R_KICKED = 5, R_RATE = 6, R_PROTO = 7;

const PROTO = 1;
const SCENE_BASE = 2 ** 51; // view ids >= this belong to scene objects (owner = host)
const OWNER_UNIT = 2 ** 40; // instantiated view id = (owner + 1) * 2^40 + counter * 2^12 + sub
const MAX_BODY = 8192; // max size of one message body
const MAX_FRAME = 65536;
const MAX_MSGS_PER_FRAME = 512;
const MAX_BUFFER_ENTRIES = 20000;
const MAX_BUFFER_PER_SENDER = 8000;
const MAX_BUFFER_BYTES = 4 * 1024 * 1024;
const SNAPSHOT_MAX = 1_500_000;
const ALARM_MS = 25_000;
const MAX_PLAYER_ID = 250;

const enc = new TextEncoder();
const dec = new TextDecoder();

function u32(b, o) { return (b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24)) >>> 0; }
function i32(b, o) { return b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24); }
// Wire view id = lo u32 then hi u32 (little endian int64); JS Number is exact below 2^53.
function readView(b, o) { return u32(b, o + 4) * 4294967296 + u32(b, o); }
function rootOf(v) { return v >= SCENE_BASE ? v : Math.floor(v / 4096) * 4096; }
function ownerOf(v) { return v >= SCENE_BASE ? 0 : Math.floor(v / OWNER_UNIT) - 1; }
function putI32(a, o, v) { a[o] = v & 255; a[o + 1] = (v >> 8) & 255; a[o + 2] = (v >> 16) & 255; a[o + 3] = (v >> 24) & 255; }
function putView(a, o, v) { const lo = v % 4294967296; const hi = Math.floor(v / 4294967296); putI32(a, o, lo | 0); putI32(a, o + 4, hi | 0); }
function str8(s, max) { const b = enc.encode(String(s ?? "").slice(0, max)); return b.subarray(0, 255); }

async function ipId(code, ip) {
  const d = await crypto.subtle.digest("SHA-256", enc.encode(`zb:${code}:${ip}`));
  return [...new Uint8Array(d).slice(0, 4)].map((x) => x.toString(16).padStart(2, "0")).join("");
}

export class Room extends DurableObject {
  constructor(ctx, env) {
    super(ctx, env);
    this.meta = null;
    this.buf = []; // {seq, kind: 0 inst | 1 rpc, sender, root, body}
    this.byRoot = new Map(); // root key -> entries
    this.alive = new Set(); // roots with a buffered instantiate
    this.bytes = 0;
    this.seq = 0;
    this.players = new Map(); // id -> ws
    this.rate = new WeakMap();
    this.dirty = false;
    ctx.blockConcurrencyWhile(async () => {
      this.meta = (await ctx.storage.get("meta")) || null;
      const snap = await ctx.storage.get("buf");
      if (this.meta && snap) this.loadSnapshot(snap);
      for (const ws of ctx.getWebSockets()) {
        const a = ws.deserializeAttachment();
        if (a) this.players.set(a.id, ws);
      }
    });
  }

  // ---- connection setup -------------------------------------------------------------------
  async fetch(request) {
    const url = new URL(request.url);
    const q = url.searchParams;
    const code = q.get("code");
    const isHost = q.get("host") === "1";
    if (isHost && this.meta) return new Response("room code taken", { status: 409 });

    const pair = new WebSocketPair();
    const [client, server] = Object.values(pair);
    this.ctx.acceptWebSocket(server);
    const reply = () => new Response(null, { status: 101, webSocket: client });
    const reject = (reason) => { this.sendNow(server, [this.msg(S_REJECT, [reason])]); try { server.close(1000, "rejected"); } catch {} return reply(); };

    if (Number(q.get("v") || 0) !== PROTO) return reject(R_PROTO);
    const ipid = await ipId(code, q.get("ipid") || "");

    if (isHost) {
      const max = Math.max(2, Math.min(16, parseInt(q.get("max") || "16", 10) || 16));
      this.meta = {
        code, max, nextId: 1, created: Date.now(),
        name: (q.get("name") || "Room " + code).slice(0, 40),
        type: (q.get("type") || "").slice(0, 40),
        pw: (q.get("pw") || "").slice(0, 20),
        comment: "", listed: false,
      };
      this.buf = []; this.byRoot.clear(); this.alive.clear(); this.bytes = 0; this.seq = 0;
      server.serializeAttachment({ id: 0, host: true, ipid });
      this.players.set(0, server);
      await this.ctx.storage.put("meta", this.meta);
      await this.ctx.storage.setAlarm(Date.now() + ALARM_MS);
      this.sendNow(server, [this.welcome(0, true)]);
      return reply();
    }

    if (!this.meta || !this.players.has(0)) return reject(R_NOT_FOUND);
    if (this.meta.pw && (q.get("pw") || "") !== this.meta.pw) return reject(R_BAD_PASSWORD);
    if (this.players.size >= this.meta.max || this.meta.nextId > MAX_PLAYER_ID) return reject(R_FULL);

    const id = this.meta.nextId++;
    await this.ctx.storage.put("meta", this.meta);
    server.serializeAttachment({ id, host: false, ipid });
    this.players.set(id, server);
    this.sendNow(server, [this.welcome(id, false)]);
    // tell the host first (OnPlayerConnected), then replay buffered state to the newcomer
    const host = this.players.get(0);
    if (host) { const ip = str8(ipid, 16); this.sendNow(host, [this.msg(S_CONNECTED, [...i32b(id), ip.length, ...ip])]); }
    this.replayTo(server);
    this.lobbyUpdate();
    return reply();
  }

  welcome(id, isHost) {
    const code = str8(this.meta.code, 8);
    return this.msg(S_WELCOME, [...i32b(id), isHost ? 1 : 0, this.meta.max, code.length, ...code]);
  }

  msg(op, arr) { const a = new Uint8Array(1 + arr.length); a[0] = op; a.set(arr, 1); return a; }

  // one-off send of already built bodies as a single frame
  sendNow(ws, bodies) {
    let n = 0; for (const b of bodies) n += 2 + b.length;
    const f = new Uint8Array(n); let o = 0;
    for (const b of bodies) { f[o++] = b.length & 255; f[o++] = b.length >> 8; f.set(b, o); o += b.length; }
    try { ws.send(f); } catch {}
  }

  replayTo(ws) {
    let chunk = [], size = 0;
    const flush = () => { if (chunk.length) { this.sendNow(ws, chunk); chunk = []; size = 0; } };
    for (const e of this.buf) {
      chunk.push(e.body); size += e.body.length + 2;
      if (size > 120_000) flush();
    }
    chunk.push(new Uint8Array([S_SYNC_DONE]));
    flush();
  }

  // ---- messages ---------------------------------------------------------------------------
  async webSocketMessage(ws, data) {
    if (typeof data === "string" || !this.meta) return;
    const att = ws.deserializeAttachment();
    if (!att) return;
    const frame = new Uint8Array(data);
    if (frame.length > MAX_FRAME) { this.kick(ws, R_RATE, 1009); return; }
    if (!this.takeTokens(ws, att, frame.length)) return;

    const out = new Map(); // ws -> bodies
    const emit = (target, body) => { let l = out.get(target); if (!l) out.set(target, (l = [])); l.push(body); };
    let o = 0, count = 0;
    while (o + 2 <= frame.length && count++ < MAX_MSGS_PER_FRAME) {
      const len = frame[o] | (frame[o + 1] << 8); o += 2;
      if (len < 1 || len > MAX_BODY || o + len > frame.length) break;
      try { this.handle(ws, att, frame.subarray(o, o + len), emit); } catch (e) { console.log("handle error", String(e)); }
      o += len;
    }
    for (const [target, bodies] of out) this.sendNow(target, bodies);
  }

  takeTokens(ws, att, n) {
    const now = Date.now();
    const rate = att.host ? 1_000_000 : 96_000; // bytes per second
    const burst = rate * 2;
    let r = this.rate.get(ws);
    if (!r) { r = { tokens: burst, t: now, bad: 0 }; this.rate.set(ws, r); }
    r.tokens = Math.min(burst, r.tokens + ((now - r.t) / 1000) * rate); r.t = now;
    if (r.tokens < n) { if (++r.bad > 100) this.kick(ws, R_RATE, 1008); return false; }
    r.tokens -= n; r.bad = 0;
    return true;
  }

  handle(ws, att, b, emit) {
    const me = att.id;
    switch (b[0]) {
      case C_RPC: {
        if (b.length < 11) return;
        const mode = b[1], view = readView(b, 2), nameLen = b[10];
        if (11 + nameLen > b.length || mode > 6 || mode === 3 || mode === 4) return;
        const name = dec.decode(b.subarray(11, 11 + nameLen));
        const buffered = mode === 5 || mode === 6;
        const root = rootOf(view);
        const out = new Uint8Array(b.length + 4); out[0] = S_RPC; putI32(out, 1, me); out.set(b.subarray(1), 5);

        if (buffered && name === "killmebuffered") {
          // Net effect of instantiate + kill is nothing: drop the object's history. A kill for an object that
          // is already gone is an echo (every peer's OnDestroy re-sends it), so swallow it.
          if (!this.alive.has(root)) return;
          this.dropRoot(root);
        } else if (buffered) {
          if (!this.addBuffer(1, me, root, out)) return;
        }
        if (mode === 0) { if (me !== 0) { const h = this.players.get(0); if (h) emit(h, out); } return; }
        for (const [id, w] of this.players) if (id !== me) emit(w, out);
        return;
      }
      case C_INST: {
        if (b.length !== 40) return;
        const view = readView(b, 2);
        if (ownerOf(view) !== me && !(att.host && view >= SCENE_BASE)) return; // can only create own objects
        const root = rootOf(view);
        const out = new Uint8Array(b.length + 4); out[0] = S_INST; putI32(out, 1, me); out.set(b.subarray(1), 5);
        if (!this.addBuffer(0, me, root, out)) return;
        this.alive.add(root);
        for (const [id, w] of this.players) if (id !== me) emit(w, out);
        return;
      }
      case C_DESTROY: {
        if (b.length < 9) return;
        const root = rootOf(readView(b, 1));
        if (ownerOf(root) !== me && !att.host) return;
        this.dropRoot(root);
        const out = new Uint8Array(9); out[0] = S_DESTROY; putView(out, 1, root);
        for (const [id, w] of this.players) if (id !== me) emit(w, out);
        return;
      }
      case C_REMOVE_RPCS: {
        const p = i32(b, 1);
        if (p !== me && !att.host) return;
        this.removeWhere((e) => e.kind === 1 && e.sender === p);
        return;
      }
      case C_DESTROY_PLAYER: {
        const p = i32(b, 1);
        if (p !== me && !att.host) return;
        const roots = [...this.alive].filter((r) => ownerOf(r) === p);
        for (const r of roots) {
          this.dropRoot(r);
          const out = new Uint8Array(9); out[0] = S_DESTROY; putView(out, 1, r);
          for (const w of this.players.values()) emit(w, out);
        }
        this.removeWhere((e) => e.kind === 1 && e.sender === p);
        return;
      }
      case C_KICK: {
        if (!att.host) return;
        const w = this.players.get(i32(b, 1));
        if (w && i32(b, 1) !== 0) this.kick(w, R_KICKED, 1000);
        return;
      }
      case C_PING: {
        if (b.length < 7) return;
        const pong = new Uint8Array(5); pong[0] = S_PONG; pong.set(b.subarray(1, 5), 1);
        emit(ws, pong);
        if (!att.host) {
          const h = this.players.get(0);
          if (h) { const info = new Uint8Array(7); info[0] = S_PING_INFO; putI32(info, 1, me); info[5] = b[5]; info[6] = b[6]; emit(h, info); }
        }
        return;
      }
      case C_LISTING: {
        if (!att.host || b.length < 3) return;
        const listed = b[1] === 1;
        const nl = b[2]; if (3 + nl >= b.length) return;
        const name = dec.decode(b.subarray(3, 3 + nl)).slice(0, 40);
        const cl = b[3 + nl]; const comment = dec.decode(b.subarray(4 + nl, 4 + nl + cl)).slice(0, 60);
        const type = b.length > 4 + nl + cl ? dec.decode(b.subarray(5 + nl + cl, 5 + nl + cl + b[4 + nl + cl])).slice(0, 40) : this.meta.type;
        Object.assign(this.meta, { listed, name: name || this.meta.name, comment, type });
        this.ctx.storage.put("meta", this.meta);
        this.lobbyUpdate();
        return;
      }
    }
  }

  // ---- buffer -----------------------------------------------------------------------------
  addBuffer(kind, sender, root, body) {
    if (this.buf.length >= MAX_BUFFER_ENTRIES || this.bytes + body.length > MAX_BUFFER_BYTES) return false;
    let mine = 0; for (const e of this.byRoot.get(root) || []) if (e.sender === sender) mine++;
    if (mine > MAX_BUFFER_PER_SENDER) return false;
    const e = { seq: this.seq++, kind, sender, root, body };
    this.buf.push(e); this.bytes += body.length;
    let l = this.byRoot.get(root); if (!l) this.byRoot.set(root, (l = []));
    l.push(e);
    this.dirty = true;
    return true;
  }

  dropRoot(root) {
    this.alive.delete(root);
    const l = this.byRoot.get(root);
    if (!l) return;
    this.byRoot.delete(root);
    const dead = new Set(l);
    this.buf = this.buf.filter((e) => !dead.has(e));
    for (const e of l) this.bytes -= e.body.length;
    this.dirty = true;
  }

  removeWhere(pred) {
    const keep = [];
    for (const e of this.buf) {
      if (pred(e)) {
        this.bytes -= e.body.length;
        const l = this.byRoot.get(e.root);
        if (l) { const i = l.indexOf(e); if (i >= 0) l.splice(i, 1); if (!l.length) this.byRoot.delete(e.root); }
      } else keep.push(e);
    }
    this.buf = keep; this.dirty = true;
  }

  saveSnapshot() {
    if (!this.dirty || !this.meta) return;
    this.dirty = false;
    let n = 0; for (const e of this.buf) n += 9 + e.body.length;
    if (n > SNAPSHOT_MAX) return;
    const a = new Uint8Array(n); let o = 0;
    for (const e of this.buf) { a[o++] = e.kind; putI32(a, o, e.sender); o += 4; putI32(a, o, e.body.length); o += 4; a.set(e.body, o); o += e.body.length; }
    this.ctx.storage.put("buf", a);
  }

  loadSnapshot(a) {
    a = new Uint8Array(a);
    let o = 0;
    while (o + 9 <= a.length) {
      const kind = a[o], sender = i32(a, o + 1), len = i32(a, o + 5); o += 9;
      const body = a.slice(o, o + len); o += len;
      const root = rootOf(readView(body, 6));
      const e = { seq: this.seq++, kind, sender, root, body };
      this.buf.push(e); this.bytes += len;
      let l = this.byRoot.get(root); if (!l) this.byRoot.set(root, (l = []));
      l.push(e);
      if (kind === 0) this.alive.add(root);
    }
  }

  // ---- lifecycle --------------------------------------------------------------------------
  kick(ws, reason, code) {
    this.sendNow(ws, [this.msg(S_REJECT, [reason])]);
    try { ws.close(code, "closed"); } catch {}
    // webSocketClose is not guaranteed to fire for a server-initiated close: clean up now.
    this.dropPlayer(ws);
  }

  dropPlayer(ws) {
    const att = ws.deserializeAttachment();
    if (!att || !this.meta) return;
    if (att.host) { this.closeRoom(R_HOST_LEFT); return; }
    if (this.players.get(att.id) !== ws) return;
    this.players.delete(att.id);
    const h = this.players.get(0);
    if (h) this.sendNow(h, [this.msg(S_DISCONNECTED, i32b(att.id))]);
    this.lobbyUpdate();
  }

  async webSocketClose(ws, code, reason) {
    try { ws.close(code, reason); } catch {}
    this.dropPlayer(ws);
  }

  async webSocketError(ws) { this.dropPlayer(ws); }

  async closeRoom(reason) {
    if (!this.meta) return;
    const code = this.meta.code, listed = this.meta.listed;
    const sockets = [...this.players.values()];
    this.meta = null; this.buf = []; this.byRoot.clear(); this.alive.clear(); this.players.clear(); this.bytes = 0;
    for (const w of sockets) { this.sendNow(w, [this.msg(S_REJECT, [reason])]); try { w.close(1000, "room closed"); } catch {} }
    try { await this.ctx.storage.deleteAlarm(); await this.ctx.storage.deleteAll(); } catch {}
    if (listed) this.lobby().remove(code).catch(() => {});
  }

  lobby() { return this.env.LOBBY.get(this.env.LOBBY.idFromName("lobby")); }

  lobbyUpdate() {
    const m = this.meta;
    if (!m) return;
    if (!m.listed) { this.lobby().remove(m.code).catch(() => {}); return; }
    this.lobby().upsert({ code: m.code, name: m.name, players: this.players.size, max: m.max, type: m.type, pw: !!m.pw, comment: m.comment }).catch(() => {});
  }

  async alarm() {
    if (!this.meta) return;
    if (!this.players.has(0)) { await this.closeRoom(R_HOST_LEFT); return; }
    this.saveSnapshot();
    this.lobbyUpdate();
    await this.ctx.storage.setAlarm(Date.now() + ALARM_MS);
  }
}

function i32b(v) { return [v & 255, (v >> 8) & 255, (v >> 16) & 255, (v >> 24) & 255]; }
