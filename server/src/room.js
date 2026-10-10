// One Durable Object per game room: a dumb relay that mimics the parts of Unity's legacy RakNet
// networking the game relied on (see docs/MULTIPLAYER.md for the wire protocol).
import { DurableObject } from "cloudflare:workers";

// client -> relay
const C_RPC = 1, C_INST = 2, C_DESTROY = 3, C_REMOVE_RPCS = 4, C_DESTROY_PLAYER = 5, C_KICK = 6, C_PING = 7, C_LISTING = 8, C_TIME = 9, C_ECHO = 10, C_STATE = 11,
  C_META = 16; // 12-15 reserved (phase 1/2), 17-19 reserved (phase 4, docs/design/PHASE3-4-MATCHMAKING-RESILIENCE.md)
// relay -> client
const S_WELCOME = 0x81, S_RPC = 0x82, S_INST = 0x83, S_DESTROY = 0x84, S_CONNECTED = 0x85, S_DISCONNECTED = 0x86,
  S_REJECT = 0x87, S_PONG = 0x88, S_PING_INFO = 0x89, S_SYNC_DONE = 0x8a, S_TIME = 0x8b, S_ECHO = 0x8c, S_STATE = 0x8d,
  S_COUNTDOWN = 0x95; // 0x8e-0x94 reserved (phase 4)
// reject / close reasons
export const R_NOT_FOUND = 1, R_BAD_PASSWORD = 2, R_FULL = 3, R_HOST_LEFT = 4, R_KICKED = 5, R_RATE = 6, R_PROTO = 7;

const PROTO = 2;
const SCENE_BASE = 2 ** 51; // view ids >= this belong to scene objects (owner = host)
const OWNER_UNIT = 2 ** 40; // instantiated view id = (owner + 1) * 2^40 + counter * 2^12 + sub
const MAX_BODY = 8192; // max size of one message body
const MAX_FRAME = 65536;
// body length of the compact state messages by kind (C_STATE: op, kind, view i64, payload)
const STATE_LEN = { 1: 24, 2: 38, 3: 22 };
const MAX_MSGS_PER_FRAME = 512;
// per socket message budget (messages per second, burst = 2 s): the host sends hundreds a second at 20 Hz state, a client far fewer
const MSG_RATE_HOST = 3000, MSG_RATE_CLIENT = 600;
const MAX_BUFFER_ENTRIES = 20000;
const MAX_BUFFER_PER_SENDER = 8000;
const MAX_BUFFER_BYTES = 4 * 1024 * 1024;
const SNAPSHOT_MAX = 1_500_000;
const ALARM_MS = 25_000;
const MAX_PLAYER_ID = 250;
// Phase 2 (docs/design/PHASE2-WEBRTC.md): JSON text frames on the same socket carry WebRTC signaling and the
// "data channel is up" flag. Binary frames are untouched.
const MAX_TEXT = 8192;
const SIG_KINDS = new Set(["offer", "answer", "ice", "bye"]);
const SIG_PER_10S = 60;

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
    this.dcUp = new Set(); // ids of clients whose host->client state travels over their WebRTC data channel
    this.sigRate = new WeakMap();
    this.rate = new WeakMap();
    this.msgRate = new WeakMap();
    this.dirty = false;
    ctx.blockConcurrencyWhile(async () => {
      this.meta = (await ctx.storage.get("meta")) || null;
      const snap = await ctx.storage.get("buf");
      if (this.meta && snap) this.loadSnapshot(snap);
      for (const ws of ctx.getWebSockets()) {
        const a = ws.deserializeAttachment();
        if (a) { this.players.set(a.id, ws); if (a.dc && !a.host) this.dcUp.add(a.id); }
      }
    });
  }

  // ---- read-only RPC for /api/room/:code and /api/ping/:code (never creates storage) ---------------
  info() {
    const m = this.meta;
    if (!m || !this.players.has(0)) return { exists: false };
    return { exists: true, name: m.name, type: m.type, players: this.players.size, max: m.max, pw: !!m.pw, started: !!m.started, ended: !!m.ended,
      wave: m.wave || 0, quick: !!m.quick, full: this.players.size >= m.max || m.nextId > MAX_PLAYER_ID };
  }
  ping() { return 1; }

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
    const rtc = q.get("rtc") === "1";

    if (isHost) {
      const max = Math.max(2, Math.min(16, parseInt(q.get("max") || "16", 10) || 16));
      this.meta = {
        code, max, nextId: 1, created: Date.now(),
        name: (q.get("name") || "Room " + code).slice(0, 40),
        type: (q.get("type") || "").slice(0, 40),
        pw: (q.get("pw") || "").slice(0, 20),
        comment: "", listed: false,
        // phase 3: lobby metadata. colo/country/lat/lon come from the Worker (request.cf), never from the client.
        colo: (q.get("colo") || "").slice(0, 8), country: (q.get("country") || "").slice(0, 2),
        lat: Number(q.get("lat")) || 0, lon: Number(q.get("lon")) || 0,
        wave: 0, started: false, ended: false, alive: 0, quick: false,
      };
      this.buf = []; this.byRoot.clear(); this.alive.clear(); this.bytes = 0; this.seq = 0;
      server.serializeAttachment({ id: 0, host: true, ipid, rtc });
      this.players.set(0, server);
      await this.ctx.storage.put("meta", this.meta);
      await this.ctx.storage.setAlarm(Date.now() + ALARM_MS);
      this.sendNow(server, [this.welcome(0, true)]);
      if (rtc) this.sendText(server, { t: "hello", id: 0, host: true });
      return reply();
    }

    if (!this.meta || !this.players.has(0)) return reject(R_NOT_FOUND);
    if (this.meta.pw && (q.get("pw") || "") !== this.meta.pw) return reject(R_BAD_PASSWORD);
    if (this.players.size >= this.meta.max || this.meta.nextId > MAX_PLAYER_ID) return reject(R_FULL);

    const id = this.meta.nextId++;
    await this.ctx.storage.put("meta", this.meta);
    server.serializeAttachment({ id, host: false, ipid, rtc });
    this.players.set(id, server);
    this.sendNow(server, [this.welcome(id, false)]);
    if (rtc) this.sendText(server, { t: "hello", id, host: false });
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

  sendText(ws, obj) { try { ws.send(JSON.stringify(obj)); } catch {} }

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
    if (!this.meta) return;
    const att = ws.deserializeAttachment();
    if (!att) return;
    if (typeof data === "string") { this.handleText(ws, att, data); return; }
    const frame = new Uint8Array(data);
    if (frame.length > MAX_FRAME) { this.kick(ws, R_RATE, 1009); return; }
    if (!this.takeTokens(ws, att, frame.length)) return;

    const out = new Map(); // ws -> bodies
    const emit = (target, body) => { let l = out.get(target); if (!l) out.set(target, (l = [])); l.push(body); };
    const ctx = { stamp: null }; // sender clock of this frame (C_TIME), passed on to every recipient
    let o = 0, count = 0;
    while (o + 2 <= frame.length && count++ < MAX_MSGS_PER_FRAME) {
      const len = frame[o] | (frame[o + 1] << 8); o += 2;
      if (len < 1 || len > MAX_BODY || o + len > frame.length) break;
      if (!this.takeMsg(ws, att)) { o += len; continue; } // over budget: skip the message (cheap), 100 in a row close the socket
      try { this.handle(ws, att, frame.subarray(o, o + len), emit, ctx); } catch (e) { console.log("handle error", String(e)); }
      o += len;
    }
    // One WebSocket frame per recipient for everything this frame produced; with the sender's clock in front so the
    // recipient can interpolate on the sender's timeline (S_TIME: i32 sender, u32 clock).
    for (const [target, bodies] of out) {
      if (ctx.stamp && target !== ws) {
        const t = new Uint8Array(9); t[0] = S_TIME; putI32(t, 1, att.id); t.set(ctx.stamp, 5);
        bodies.unshift(t);
      }
      this.sendNow(target, bodies);
    }
  }

  // WebRTC signaling and data channel flag (JSON text frames). Everything malformed is dropped silently.
  handleText(ws, att, text) {
    if (!att.rtc || text.length > MAX_TEXT) return;
    if (!this.takeTokens(ws, att, text.length)) return;
    let m;
    try { m = JSON.parse(text); } catch { return; }
    if (!m || typeof m !== "object") return;
    if (m.t === "sig") {
      const now = Date.now();
      let r = this.sigRate.get(ws);
      if (!r || now - r.t0 > 10000) { r = { t0: now, n: 0 }; this.sigRate.set(ws, r); }
      if (++r.n > SIG_PER_10S) return;
      const to = m.to;
      if (!Number.isInteger(to) || to === att.id || !SIG_KINDS.has(m.k)) return;
      if (!att.host && to !== 0) return; // star only: clients may signal the host and nobody else
      const w = this.players.get(to);
      if (!w) return;
      const ta = w.deserializeAttachment();
      if (!ta || !ta.rtc) return;
      const out = { t: "sig", from: att.id, gen: Number.isInteger(m.gen) ? m.gen : 0, k: m.k };
      if (typeof m.sdp === "string") out.sdp = m.sdp;
      if (m.c && typeof m.c === "object") out.c = m.c;
      this.sendText(w, out);
    } else if (m.t === "dc") {
      if (att.host) {
        // the host may only clear a client's flag (it noticed the channel died), never set it
        if (m.up === false && Number.isInteger(m.to)) this.setDc(m.to, false);
      } else this.setDc(att.id, m.up === true);
    } else if (m.t === "stat") {
      // connection telemetry from a client (result, candidate type, ms)
      console.log("p2p-stat", JSON.stringify({ id: att.id, r: String(m.r || "").slice(0, 16), c: String(m.c || "").slice(0, 16), ms: Number(m.ms) | 0 }));
    }
  }

  setDc(id, up) {
    if (id === 0) return;
    const w = this.players.get(id);
    if (!w) { this.dcUp.delete(id); return; }
    const a = w.deserializeAttachment();
    if (!a || !a.rtc) return;
    if (up) this.dcUp.add(id); else this.dcUp.delete(id);
    if (!!a.dc !== up) { a.dc = up; w.serializeAttachment(a); }
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

  // message count bucket (next to the byte bucket): a flood of tiny messages costs CPU even when it is few bytes
  takeMsg(ws, att) {
    const now = Date.now();
    const rate = att.host ? MSG_RATE_HOST : MSG_RATE_CLIENT, burst = rate * 2;
    let m = this.msgRate.get(ws);
    if (!m) { m = { tokens: burst, t: now, bad: 0 }; this.msgRate.set(ws, m); }
    m.tokens = Math.min(burst, m.tokens + ((now - m.t) / 1000) * rate); m.t = now;
    if (m.tokens < 1) { if (++m.bad > 100) this.kick(ws, R_RATE, 1008); return false; }
    m.tokens -= 1; m.bad = 0;
    return true;
  }

  handle(ws, att, b, emit, ctx = {}) {
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
      case C_TIME: {
        if (b.length === 5) ctx.stamp = b.slice(1, 5);
        return;
      }
      case C_STATE: {
        // compact position updates (what used to be RPCs setzumbistuff / setposstuff / updateposition, mode Others, unbuffered)
        if (b.length < 2 || STATE_LEN[b[1]] !== b.length) return;
        const out = new Uint8Array(b.length); out.set(b); out[0] = S_STATE;
        for (const [id, w] of this.players) if (id !== me && !(me === 0 && this.dcUp.has(id))) emit(w, out);
        return;
      }
      case C_ECHO: {
        // end to end ping: clients may only echo with the host, the host with anyone. body: u8 type, i32 target, u32 t
        if (b.length !== 10 || b[1] > 1) return;
        const target = i32(b, 2);
        if (target === me || (me !== 0 && target !== 0)) return;
        const w = this.players.get(target);
        if (!w) return;
        const out = new Uint8Array(10); out[0] = S_ECHO; putI32(out, 1, me); out[5] = b[1]; out.set(b.subarray(6, 10), 6);
        emit(w, out);
        return;
      }
      case C_META: {
        // host only: u16 wave, u8 flags (1 started, 2 ended), u8 alive, [u8 countdown seconds, Quick Play auto start]. Malformed = ignored.
        if (!att.host || (b.length !== 5 && b.length !== 6)) return;
        const wave = Math.min(9999, b[1] | (b[2] << 8)), flags = b[3] & 3, alive = b[4], cd = b.length === 6 ? Math.min(60, b[5]) : 0;
        const m = this.meta;
        const changed = m.wave !== wave || m.started !== !!(flags & 1) || m.ended !== !!(flags & 2) || m.alive !== alive;
        Object.assign(m, { wave, started: !!(flags & 1), ended: !!(flags & 2), alive });
        if (cd > 0 || this.cdOn) {
          // the host sends this every second while the countdown runs; tell everybody else (a late joiner gets the next tick)
          const out = new Uint8Array([S_COUNTDOWN, cd]);
          for (const [id, w] of this.players) if (id !== me) emit(w, out);
          this.cdOn = cd > 0;
        }
        if (changed) { this.ctx.storage.put("meta", m); this.lobbyUpdate(); }
        return;
      }
      case C_LISTING: {
        if (!att.host || b.length < 3) return;
        const listed = b[1] === 1;
        const nl = b[2]; if (3 + nl >= b.length) return;
        const name = dec.decode(b.subarray(3, 3 + nl)).slice(0, 40);
        const cl = b[3 + nl]; const comment = dec.decode(b.subarray(4 + nl, 4 + nl + cl)).slice(0, 60);
        const type = b.length > 4 + nl + cl ? dec.decode(b.subarray(5 + nl + cl, 5 + nl + cl + b[4 + nl + cl])).slice(0, 40) : this.meta.type;
        Object.assign(this.meta, { listed, name: name || this.meta.name, comment, type, quick: comment === "Quick Play" });
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
    this.dcUp.delete(att.id);
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
    this.meta = null; this.buf = []; this.byRoot.clear(); this.alive.clear(); this.players.clear(); this.dcUp.clear(); this.bytes = 0;
    for (const w of sockets) { this.sendNow(w, [this.msg(S_REJECT, [reason])]); try { w.close(1000, "room closed"); } catch {} }
    try { await this.ctx.storage.deleteAlarm(); await this.ctx.storage.deleteAll(); } catch {}
    if (listed) this.lobby().remove(code).catch(() => {});
  }

  lobby() { return this.env.LOBBY.get(this.env.LOBBY.idFromName("lobby")); }

  lobbyUpdate() {
    const m = this.meta;
    if (!m) return;
    if (!m.listed) { this.lobby().remove(m.code).catch(() => {}); return; }
    this.lobby().upsert({
      code: m.code, name: m.name, players: this.players.size, max: m.max, type: m.type, pw: !!m.pw, comment: m.comment,
      wave: m.wave || 0, started: !!m.started, ended: !!m.ended, created: m.created, colo: m.colo || "", country: m.country || "",
      lat: m.lat || 0, lon: m.lon || 0, quick: !!m.quick,
    }).catch(() => {});
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
