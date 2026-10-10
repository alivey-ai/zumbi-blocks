// One Durable Object per game room: a dumb relay that mimics the parts of Unity's legacy RakNet
// networking the game relied on (see docs/MULTIPLAYER.md for the wire protocol).
//
// Protocol 2 sockets behave exactly as before phase 4. Protocol 3 sockets (phase 4, docs/design/PHASE3-4-MATCHMAKING-RESILIENCE.md) add:
//   * a resume token and a grace period: a socket that dies without a clean close keeps its player in "limbo" (20 s clients, 15 s host);
//     `/ws/room/CODE?v=3&rejoin=ID.TOKEN&seq=N` takes the slot back and replays what was missed (S_SEQ numbers, tombstones, count based resend)
//   * host states (S_HOST_STATE: away, hidden tab, restarting) and the host-bound queue while the host is away
//   * "claim restart" (Level B): when the host is gone for good a remaining player claims the room code and the group continues in a new match
import { DurableObject } from "cloudflare:workers";

// client -> relay
const C_RPC = 1, C_INST = 2, C_DESTROY = 3, C_REMOVE_RPCS = 4, C_DESTROY_PLAYER = 5, C_KICK = 6, C_PING = 7, C_LISTING = 8, C_TIME = 9, C_ECHO = 10, C_STATE = 11,
  C_META = 16, C_VIS = 17; // 12-15 reserved (phase 1/2); 18 C_SNAP and 19 C_ADOPTED are reserved for host migration level C (not built)
// relay -> client
const S_WELCOME = 0x81, S_RPC = 0x82, S_INST = 0x83, S_DESTROY = 0x84, S_CONNECTED = 0x85, S_DISCONNECTED = 0x86,
  S_REJECT = 0x87, S_PONG = 0x88, S_PING_INFO = 0x89, S_SYNC_DONE = 0x8a, S_TIME = 0x8b, S_ECHO = 0x8c, S_STATE = 0x8d,
  S_SEQ = 0x8e, S_HOST_STATE = 0x8f, // phase 4; 0x90-0x94 reserved (host change / snapshot / roster, level C), unused
  S_COUNTDOWN = 0x95;
// reject / close reasons
export const R_NOT_FOUND = 1, R_BAD_PASSWORD = 2, R_FULL = 3, R_HOST_LEFT = 4, R_KICKED = 5, R_RATE = 6, R_PROTO = 7,
  R_REPLACED = 8, R_HOST_AWAY = 9, R_EXPIRED = 10;
// S_HOST_STATE states
const H_OK = 0, H_AWAY = 1, H_HIDDEN = 2, H_RESTART = 3;

const PROTO = 3; // accepted: 2 (behaves as before) and 3
const GRACE_CLIENT_S = 20, GRACE_HOST_S = 15;
const STALE_MS = 12_000; // a visible protocol 3 socket that sends nothing for this long is treated as dropped
const CLAIM_PRIORITY_MS = 10_000; // after a restart starts only the chosen successor may claim; after this any remaining player may
const RESTART_TOTAL_MS = 45_000;  // nobody claimed: the room closes
const TOMB_MS = 60_000, TOMB_MAX = 4000;
const HOSTQ_MAX = 300, HOSTQ_BYTES = 128 * 1024;
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
function newToken() { return [...crypto.getRandomValues(new Uint8Array(16))].map((x) => x.toString(16).padStart(2, "0")).join(""); }
function seqMsg(seq) { const m = new Uint8Array(5); m[0] = S_SEQ; putI32(m, 1, seq | 0); return m; }
function i32b(v) { return [v & 255, (v >> 8) & 255, (v >> 16) & 255, (v >> 24) & 255]; }

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
    this.players = new Map(); // id -> ws (connected sockets only)
    this.limbo = new Map(); // id -> {tok, since, ipid, rtc, host, bufOps}: protocol 3 players whose socket died, slot reserved for the grace period
    this.wsInfo = new WeakMap(); // ws -> {id, v}
    this.lastSeen = new Map(); // id -> ms of the last frame (staleness)
    this.vis = new Map(); // id -> false while that player's tab is hidden (never limboed for silence)
    this.bufOps = new Map(); // id -> buffered ops parsed from that player (count based resend after a resume); -1 = unknown (after a wake)
    this.lastRtt = new Map(); // id -> relay rtt reported by the client's ping (successor choice)
    this.pendingToHost = []; // bodies for the host while it is away
    this.pendingBytes = 0;
    this.tomb = []; // {root, instSeq, t}: destroyed roots, replayed as S_DESTROY to a resuming client that had them
    this.lastSweep = 0;
    this.dcUp = new Set(); // ids of clients whose host->client state travels over their WebRTC data channel
    this.sigRate = new WeakMap();
    this.rate = new WeakMap();
    this.msgRate = new WeakMap();
    this.dirty = false;
    ctx.blockConcurrencyWhile(async () => {
      this.meta = (await ctx.storage.get("meta")) || null;
      const snap = await ctx.storage.get("buf");
      if (this.meta && snap) this.loadSnapshot(snap);
      const lb = await ctx.storage.get("limbo");
      if (this.meta && lb) this.limbo = new Map(lb);
      const now = Date.now();
      for (const ws of ctx.getWebSockets()) {
        const a = ws.deserializeAttachment();
        if (a && !a.gone) {
          this.players.set(a.id, ws); this.wsInfo.set(ws, { id: a.id, v: a.v || 2 });
          this.bufOps.set(a.id, -1); this.lastSeen.set(a.id, now);
          if (a.dc && !a.host) this.dcUp.add(a.id);
        }
      }
    });
  }

  // ---- read-only RPC for /api/room/:code and /api/ping/:code (never creates storage) ---------------
  info() {
    const m = this.meta;
    if (!m) return { exists: false };
    const hostHere = this.players.has(0);
    if (!hostHere && !this.limbo.has(0) && !m.restart) return { exists: false };
    const n = m.restart ? 0 : this.players.size + this.limbo.size;
    const hostState = hostHere ? (this.vis.get(0) === false ? "hidden" : "ok") : m.restart ? "restarting" : "away";
    const out = { exists: true, name: m.name, type: m.type, players: n, max: m.max, pw: !!m.pw, started: !!m.started, ended: !!m.ended,
      wave: m.wave || 0, quick: !!m.quick, full: n >= m.max || m.nextId > MAX_PLAYER_ID, hostState, epoch: m.epoch || 0 };
    if (m.restart) out.restart = { successor: m.restart.successor, claimOpen: Date.now() - m.restart.since > this.claimMs() };
    return out;
  }
  ping() { return 1; }

  // ---- connection setup -------------------------------------------------------------------
  async fetch(request) {
    const url = new URL(request.url);
    const q = url.searchParams;
    const code = q.get("code");
    const isHost = q.get("host") === "1";
    const claim = q.get("claim");
    if (isHost && this.meta && !claim) return new Response("room code taken", { status: 409 });

    const pair = new WebSocketPair();
    const [client, server] = Object.values(pair);
    this.ctx.acceptWebSocket(server);
    const reply = () => new Response(null, { status: 101, webSocket: client });
    const reject = (reason) => { this.sendNow(server, [this.msg(S_REJECT, [reason])]); try { server.close(1000, "rejected"); } catch {} return reply(); };

    const ver = Number(q.get("v") || 0);
    if (ver !== 2 && ver !== PROTO) return reject(R_PROTO);
    const v3 = ver === PROTO;
    const ipid = await ipId(code, q.get("ipid") || "");
    const rtc = q.get("rtc") === "1";

    if (claim) return this.claimRoom(server, reply, reject, claim, v3, ipid, rtc);

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
        epoch: 0, bufGen: newToken().slice(0, 8), restart: null,
      };
      // local tests only (wrangler dev --var ZB_TEST:1): short grace / silence times
      if (this.env.ZB_TEST) { const g = Number(q.get("grace")), st = Number(q.get("tstale")); if (g > 0) this.meta.grace = g; if (st > 0) this.meta.staleMs = st; const cl = Number(q.get("tclaim")); if (cl > 0) this.meta.claimMs = cl; }
      this.resetBuffer();
      const tok = v3 ? newToken() : "";
      server.serializeAttachment({ id: 0, host: true, ipid, rtc, v: v3 ? 3 : 2, tok });
      this.players.set(0, server); this.wsInfo.set(server, { id: 0, v: v3 ? 3 : 2 }); this.bufOps.set(0, 0); this.lastSeen.set(0, Date.now());
      await this.ctx.storage.put("meta", this.meta);
      await this.ctx.storage.setAlarm(Date.now() + ALARM_MS);
      this.sendNow(server, [this.welcome(0, true, v3 ? { tok, flags: 0, accepted: 0 } : null)]);
      if (rtc) this.sendText(server, { t: "hello", id: 0, host: true });
      return reply();
    }

    if (!this.meta) return reject(R_NOT_FOUND);
    const rejoin = v3 ? q.get("rejoin") : null;
    if (rejoin) return this.resume(server, reply, reject, rejoin, Number(q.get("seq") || 0) >>> 0, v3);

    // a reloaded tab frees the slot its previous incarnation left in limbo
    const stale = v3 ? q.get("stale") : null;
    if (stale) { const [sid, stok] = stale.split("."); const rec = this.limbo.get(parseInt(sid, 10)); if (rec && !rec.host && rec.tok === stok) this.finalizeDrop(parseInt(sid, 10)); }

    if (this.meta.restart || (!this.players.has(0) && this.limbo.has(0))) return reject(R_HOST_AWAY);
    if (!this.players.has(0)) return reject(R_NOT_FOUND);
    if (this.meta.pw && (q.get("pw") || "") !== this.meta.pw) return reject(R_BAD_PASSWORD);
    if (this.players.size + this.limbo.size >= this.meta.max || this.meta.nextId > MAX_PLAYER_ID) return reject(R_FULL);

    const id = this.meta.nextId++;
    await this.ctx.storage.put("meta", this.meta);
    const tok = v3 ? newToken() : "";
    server.serializeAttachment({ id, host: false, ipid, rtc, v: v3 ? 3 : 2, tok });
    this.players.set(id, server); this.wsInfo.set(server, { id, v: v3 ? 3 : 2 }); this.bufOps.set(id, 0); this.lastSeen.set(id, Date.now());
    this.sendNow(server, [this.welcome(id, false, v3 ? { tok, flags: 0, accepted: 0 } : null)]);
    if (rtc) this.sendText(server, { t: "hello", id, host: false });
    if (v3 && this.vis.get(0) === false) this.sendNow(server, [this.msg(S_HOST_STATE, [H_HIDDEN, 0])]);
    // tell the host first (OnPlayerConnected), then replay buffered state to the newcomer
    const host = this.players.get(0);
    if (host) { const ip = str8(ipid, 16); this.sendNow(host, [this.msg(S_CONNECTED, [...i32b(id), ip.length, ...ip])]); }
    this.replayTo(server, v3);
    this.lobbyUpdate();
    return reply();
  }

  // Take a player slot back with the token from the last WELCOME. All of this runs without an await after the lookups, so
  // nothing can interleave between "slot restored" and "missed state replayed".
  resume(server, reply, reject, rejoin, clientSeq, v3) {
    const m = this.meta;
    const [sid, tok] = rejoin.split(".");
    const id = parseInt(sid, 10);
    let rec = this.limbo.get(id);
    const old = this.players.get(id);
    if (!rec && old) {
      // the relay has not noticed that the old socket is dead yet: the token proves it is the same player
      const oa = old.deserializeAttachment();
      if (oa && !oa.gone && oa.v === 3 && oa.tok && oa.tok === tok) rec = { tok, since: Date.now(), ipid: oa.ipid, rtc: oa.rtc, host: oa.host, bufOps: this.bufOps.get(id) ?? -1, bufGen: m.bufGen };
    }
    if (!Number.isInteger(id) || !rec || rec.tok !== tok || (rec.bufGen && rec.bufGen !== m.bufGen) || clientSeq > this.seq) return reject(R_EXPIRED);
    if (old) { // retire the old socket without dropping the player
      try { const oa = old.deserializeAttachment() || {}; old.serializeAttachment({ ...oa, gone: true }); old.close(4001, "replaced"); } catch {}
      this.players.delete(id);
    }
    this.limbo.delete(id); this.persistLimbo();
    const ntok = newToken();
    server.serializeAttachment({ id, host: !!rec.host, ipid: rec.ipid, rtc: !!rec.rtc, v: 3, tok: ntok });
    this.players.set(id, server); this.wsInfo.set(server, { id, v: 3 }); this.lastSeen.set(id, Date.now());
    this.bufOps.set(id, rec.bufOps);
    this.sendNow(server, [this.welcome(id, !!rec.host, { tok: ntok, flags: 1, accepted: rec.bufOps < 0 ? 0xffffffff : rec.bufOps })]);
    if (rec.rtc) this.sendText(server, { t: "hello", id, host: !!rec.host, resumed: true });
    if (rec.host) {
      // what the host missed: players that left while it was away, and the clients' requests to the server
      this.flushHostQueue(server);
      this.broadcastHostState(this.vis.get(0) === false ? H_HIDDEN : H_OK, 0);
    } else if (this.vis.get(0) === false) this.sendNow(server, [this.msg(S_HOST_STATE, [H_HIDDEN, 0])]);
    this.replayResume(server, id, clientSeq);
    this.lobbyUpdate();
    this.scheduleAlarm();
    return reply();
  }

  // Level B: a remaining player claims the room code and becomes player 0 of a fresh match.
  claimRoom(server, reply, reject, claim, v3, ipid, rtc) {
    const m = this.meta;
    if (!m || !v3) return reject(R_NOT_FOUND);
    if (!m.restart) return reject(R_REPLACED); // somebody else already claimed it (or the host came back)
    const [, sid, tok] = claim.split(".");
    const id = parseInt(sid, 10);
    const r = m.restart;
    const member = r.members.find((x) => x.id === id && x.tok === tok);
    if (!member) return reject(R_EXPIRED);
    if (id !== r.successor && Date.now() - r.since < this.claimMs()) return reject(R_HOST_AWAY);
    // fresh room state: new buffer, ids start again, the old match is gone. Code, password, name and listing stay.
    this.resetBuffer();
    this.limbo.clear(); this.persistLimbo();
    this.pendingToHost = []; this.pendingBytes = 0; this.tomb = [];
    this.vis.clear(); this.bufOps.clear(); this.lastRtt.clear(); this.dcUp.clear();
    Object.assign(m, { restart: null, nextId: 1, started: false, ended: false, wave: 0, alive: 0, bufGen: newToken().slice(0, 8) });
    const ntok = newToken();
    server.serializeAttachment({ id: 0, host: true, ipid, rtc, v: 3, tok: ntok });
    this.players.set(0, server); this.wsInfo.set(server, { id: 0, v: 3 }); this.bufOps.set(0, 0); this.lastSeen.set(0, Date.now());
    this.ctx.storage.put("meta", m);
    this.sendNow(server, [this.welcome(0, true, { tok: ntok, flags: 2, accepted: 0 })]);
    if (rtc) this.sendText(server, { t: "hello", id: 0, host: true });
    this.lobbyUpdate();
    this.scheduleAlarm();
    return reply();
  }

  claimMs() { return (this.meta && this.meta.claimMs) || CLAIM_PRIORITY_MS; }
  graceS(host) { return (this.meta && this.meta.grace) || (host ? GRACE_HOST_S : GRACE_CLIENT_S); }

  resetBuffer() { this.buf = []; this.byRoot.clear(); this.alive.clear(); this.bytes = 0; this.dirty = true; }

  // WELCOME: i32 id, u8 isHost, u8 max, str8 code; protocol 3 appends: str8 token, u8 graceSec, u8 epoch, u8 flags (1 resumed, 2 restart claim),
  // i32 hostId (always 0, the role pointer exists only for level C), u32 acceptedBufferedOps (0xffffffff = unknown)
  welcome(id, isHost, ext) {
    const code = str8(this.meta.code, 8);
    const a = [...i32b(id), isHost ? 1 : 0, this.meta.max, code.length, ...code];
    if (ext) {
      const t = str8(ext.tok, 40);
      a.push(t.length, ...t, this.graceS(isHost), (this.meta.epoch || 0) & 255, ext.flags, ...i32b(0), ...i32b(ext.accepted | 0));
    }
    return this.msg(S_WELCOME, a);
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

  verOf(ws) { const i = this.wsInfo.get(ws); return i ? i.v : 2; }

  // fresh joiner: the whole buffer. Protocol 3 also learns the newest sequence number.
  replayTo(ws, v3) {
    let chunk = [], size = 0, maxSeq = -1;
    const flush = () => { if (chunk.length) { this.sendNow(ws, chunk); chunk = []; size = 0; } };
    for (const e of this.buf) {
      chunk.push(e.body); size += e.body.length + 2; if (e.seq > maxSeq) maxSeq = e.seq;
      if (size > 120_000) flush();
    }
    if (v3 && maxSeq >= 0) chunk.push(seqMsg(maxSeq));
    chunk.push(new Uint8Array([S_SYNC_DONE]));
    flush();
  }

  // resuming player: buffered entries it has not seen (never its own: it already applied them), objects destroyed meanwhile
  replayResume(ws, id, clientSeq) {
    let chunk = [], size = 0, maxSeq = clientSeq;
    const flush = () => { if (chunk.length) { this.sendNow(ws, chunk); chunk = []; size = 0; } };
    const sent = new Set();
    for (const t of this.tomb) {
      if (t.instSeq > clientSeq || sent.has(t.root)) continue; // the client never had it
      sent.add(t.root);
      const d = new Uint8Array(9); d[0] = S_DESTROY; putView(d, 1, t.root);
      chunk.push(d); size += 11;
    }
    for (const e of this.buf) {
      if (e.seq <= clientSeq || e.sender === id) continue;
      chunk.push(e.body); size += e.body.length + 2; if (e.seq > maxSeq) maxSeq = e.seq;
      if (size > 120_000) flush();
    }
    chunk.push(seqMsg(maxSeq));
    chunk.push(new Uint8Array([S_SYNC_DONE]));
    flush();
  }

  persistLimbo() { this.ctx.storage.put("limbo", [...this.limbo]); }

  broadcastHostState(state, secs) {
    const m = this.msg(S_HOST_STATE, [state, secs]);
    for (const [id, w] of this.players) if (id !== 0 && this.verOf(w) === 3) this.sendNow(w, [m]);
  }

  queueToHost(body) {
    this.pendingToHost.push(body); this.pendingBytes += body.length;
    while (this.pendingToHost.length > HOSTQ_MAX || this.pendingBytes > HOSTQ_BYTES) this.pendingBytes -= this.pendingToHost.shift().length;
  }
  flushHostQueue(ws) {
    const q = this.pendingToHost; this.pendingToHost = []; this.pendingBytes = 0;
    let chunk = [], size = 0;
    for (const b of q) { chunk.push(b); size += b.length + 2; if (size > 100_000) { this.sendNow(ws, chunk); chunk = []; size = 0; } }
    if (chunk.length) this.sendNow(ws, chunk);
  }

  // ---- messages ---------------------------------------------------------------------------
  async webSocketMessage(ws, data) {
    if (!this.meta) return;
    const att = ws.deserializeAttachment();
    if (!att || att.gone || this.players.get(att.id) !== ws) return;
    const now = Date.now();
    this.lastSeen.set(att.id, now);
    this.sweep(now);
    if (!this.meta || this.players.get(att.id) !== ws) return;
    if (typeof data === "string") { this.handleText(ws, att, data); return; }
    const frame = new Uint8Array(data);
    if (frame.length > MAX_FRAME) { this.kick(ws, R_RATE, 1009); return; }
    if (!this.takeTokens(ws, att, frame.length)) return;

    const out = new Map(); // ws -> bodies
    const emit = (target, body) => { let l = out.get(target); if (!l) out.set(target, (l = [])); l.push(body); };
    const ctx = { stamp: null, seqOf: new Map() }; // sender clock of this frame (C_TIME), passed on to every recipient; body -> buffer seq
    let o = 0, count = 0;
    while (o + 2 <= frame.length && count++ < MAX_MSGS_PER_FRAME) {
      const len = frame[o] | (frame[o + 1] << 8); o += 2;
      if (len < 1 || len > MAX_BODY || o + len > frame.length) break;
      if (!this.takeMsg(ws, att)) { o += len; continue; } // over budget: skip the message (cheap), 100 in a row close the socket
      try { this.handle(ws, att, frame.subarray(o, o + len), emit, ctx); } catch (e) { console.log("handle error", String(e)); }
      o += len;
    }
    // One WebSocket frame per recipient for everything this frame produced; with the sender's clock in front so the
    // recipient can interpolate on the sender's timeline (S_TIME: i32 sender, u32 clock). Protocol 3 recipients get the
    // newest buffer sequence number of the frame at its end (S_SEQ), which is what a resume asks from.
    for (const [target, bodies] of out) {
      if (ctx.stamp && target !== ws) {
        const t = new Uint8Array(9); t[0] = S_TIME; putI32(t, 1, att.id); t.set(ctx.stamp, 5);
        bodies.unshift(t);
      }
      if (ctx.seqOf.size && this.verOf(target) === 3) {
        let ms = -1;
        for (const b of bodies) { const s = ctx.seqOf.get(b); if (s !== undefined && s > ms) ms = s; }
        if (ms >= 0) bodies.push(seqMsg(ms));
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
    // buffered operations are counted per player: after a resume the relay tells the client how many it got, the client re-sends the rest
    if (att.v === 3 && (b[0] === C_INST || b[0] === C_DESTROY || b[0] === C_REMOVE_RPCS || b[0] === C_DESTROY_PLAYER || (b[0] === C_RPC && (b[1] === 5 || b[1] === 6)))) {
      const c = this.bufOps.get(me);
      if (c !== -1) this.bufOps.set(me, ((c || 0) + 1) >>> 0);
    }
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
          const e = this.addBuffer(1, me, root, out);
          if (!e) return;
          ctx.seqOf?.set(out, e.seq);
        }
        if (mode === 0) {
          if (me !== 0) { const h = this.players.get(0); if (h) emit(h, out); else if (this.limbo.has(0)) this.queueToHost(out); }
          return;
        }
        for (const [id, w] of this.players) if (id !== me) emit(w, out);
        return;
      }
      case C_INST: {
        if (b.length !== 40) return;
        const view = readView(b, 2);
        if (ownerOf(view) !== me && !(att.host && view >= SCENE_BASE)) return; // can only create own objects
        const root = rootOf(view);
        const out = new Uint8Array(b.length + 4); out[0] = S_INST; putI32(out, 1, me); out.set(b.subarray(1), 5);
        const e = this.addBuffer(0, me, root, out);
        if (!e) return;
        ctx.seqOf?.set(out, e.seq);
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
        this.lastRtt.set(me, b[5] | (b[6] << 8));
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
      case C_VIS: {
        // protocol 3: u8 visible, u8 fps, u16 rtt. A hidden tab does not run the game (browsers pause it), so it must neither be dropped for
        // silence nor look healthy: the host's state is broadcast, a hidden client is only remembered.
        if (att.v !== 3 || b.length !== 5) return;
        const visible = b[1] !== 0;
        const was = this.vis.get(me) === false;
        if (visible) this.vis.delete(me); else this.vis.set(me, false);
        if (att.host && was === visible) this.broadcastHostState(visible ? H_OK : H_HIDDEN, 0);
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
  // returns the new entry, or null when a limit refused it
  addBuffer(kind, sender, root, body) {
    if (this.buf.length >= MAX_BUFFER_ENTRIES || this.bytes + body.length > MAX_BUFFER_BYTES) return null;
    let mine = 0; for (const e of this.byRoot.get(root) || []) if (e.sender === sender) mine++;
    if (mine > MAX_BUFFER_PER_SENDER) return null;
    const e = { seq: this.seq++, kind, sender, root, body };
    this.buf.push(e); this.bytes += body.length;
    let l = this.byRoot.get(root); if (!l) this.byRoot.set(root, (l = []));
    l.push(e);
    this.dirty = true;
    return e;
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
    // remember the destruction for a player that is in limbo right now (it will miss the live S_DESTROY / killmebuffered)
    const inst = l.find((e) => e.kind === 0);
    const now = Date.now();
    this.tomb.push({ root, instSeq: inst ? inst.seq : l[0].seq, t: now });
    while (this.tomb.length > TOMB_MAX || (this.tomb.length && now - this.tomb[0].t > TOMB_MS)) this.tomb.shift();
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

  // Snapshot format 2: "ZB" 2, u32 next seq, then per entry kind u8, sender i32, seq u32, length i32, body. The old format (no header, no seq)
  // is still read; its entries are renumbered, which changes bufGen so no limbo token survives it.
  saveSnapshot(force) {
    if ((!this.dirty && !force) || !this.meta) return;
    this.dirty = false;
    let n = 7; for (const e of this.buf) n += 13 + e.body.length;
    if (n > SNAPSHOT_MAX) return;
    const a = new Uint8Array(n); a[0] = 0x5a; a[1] = 0x42; a[2] = 2; putI32(a, 3, this.seq | 0); let o = 7;
    for (const e of this.buf) { a[o++] = e.kind; putI32(a, o, e.sender); o += 4; putI32(a, o, e.seq | 0); o += 4; putI32(a, o, e.body.length); o += 4; a.set(e.body, o); o += e.body.length; }
    this.ctx.storage.put("buf", a);
  }

  loadSnapshot(a) {
    a = new Uint8Array(a);
    let o = 0;
    const v2 = a.length >= 7 && a[0] === 0x5a && a[1] === 0x42 && a[2] === 2;
    if (v2) { this.seq = u32(a, 3); o = 7; } else if (this.meta) this.meta.bufGen = newToken().slice(0, 8);
    while (o + (v2 ? 13 : 9) <= a.length) {
      const kind = a[o], sender = i32(a, o + 1); let seq, len;
      if (v2) { seq = u32(a, o + 5); len = i32(a, o + 9); o += 13; } else { seq = this.seq++; len = i32(a, o + 5); o += 9; }
      const body = a.slice(o, o + len); o += len;
      const root = rootOf(readView(body, 6));
      const e = { seq, kind, sender, root, body };
      this.buf.push(e); this.bytes += len;
      let l = this.byRoot.get(root); if (!l) this.byRoot.set(root, (l = []));
      l.push(e);
      if (kind === 0) this.alive.add(root);
    }
    // entries written after the last snapshot are gone: jump the counter so new entries never reuse a number a client may already have seen
    if (v2) this.seq += 65536;
  }

  // ---- lifecycle --------------------------------------------------------------------------
  // forced close (kick, flood): no grace, no token
  kick(ws, reason, code) {
    this.sendNow(ws, [this.msg(S_REJECT, [reason])]);
    try { ws.close(code, "closed"); } catch {}
    // webSocketClose is not guaranteed to fire for a server-initiated close: clean up now.
    this.dropPlayer(ws);
  }

  // The player is gone for good.
  dropPlayer(ws) {
    const att = ws.deserializeAttachment();
    if (!att || att.gone || !this.meta) return;
    if (this.players.get(att.id) !== ws) return;
    if (att.host) { this.players.delete(0); this.hostGone(); return; }
    this.finalizeDrop(att.id);
  }

  // remove a client slot (connected or in limbo) and tell the host
  finalizeDrop(id) {
    this.players.delete(id); this.limbo.delete(id); this.dcUp.delete(id); this.vis.delete(id);
    this.bufOps.delete(id); this.lastRtt.delete(id); this.lastSeen.delete(id);
    if (id === 0) return;
    this.persistLimbo();
    const body = this.msg(S_DISCONNECTED, i32b(id));
    const h = this.players.get(0);
    if (h) this.sendNow(h, [body]); else if (this.limbo.has(0)) this.queueToHost(body);
    this.lobbyUpdate();
  }

  // A protocol 3 socket died without a clean close (or went silent while visible): keep the slot for the grace period.
  enterLimbo(ws, att) {
    if (this.players.get(att.id) !== ws) return;
    const id = att.id;
    this.players.delete(id); this.dcUp.delete(id); this.vis.delete(id);
    try { ws.serializeAttachment({ ...att, gone: true }); } catch {}
    this.limbo.set(id, { tok: att.tok, since: Date.now(), ipid: att.ipid, rtc: !!att.rtc, host: !!att.host, bufOps: this.bufOps.get(id) ?? -1, bufGen: this.meta.bufGen });
    this.persistLimbo();
    this.saveSnapshot(true);
    if (att.host) this.broadcastHostState(H_AWAY, this.graceS(true));
    this.lobbyUpdate();
    this.scheduleAlarm();
  }

  // Host gone for good (clean leave, grace over, kicked by a flood): continue with a remaining player (level B) or close the room.
  hostGone() {
    if (!this.meta) return;
    const cands = [...this.players].filter(([id, w]) => id !== 0 && this.verOf(w) === 3);
    if (!cands.length) { this.closeRoom(R_HOST_LEFT); return; }
    // best candidate: visible tab first, then the lowest relay rtt, then the lowest id
    cands.sort((a, b) => ((this.vis.get(a[0]) === false) - (this.vis.get(b[0]) === false)) || ((this.lastRtt.get(a[0]) ?? 9999) - (this.lastRtt.get(b[0]) ?? 9999)) || a[0] - b[0]);
    const members = cands.map(([id, w]) => ({ id, tok: (w.deserializeAttachment() || {}).tok }));
    const m = this.meta;
    m.epoch = ((m.epoch || 0) + 1) & 255;
    m.restart = { successor: cands[0][0], since: Date.now(), members };
    m.started = false; m.ended = false; m.wave = 0;
    // every player is told and disconnected: the old match is over for everybody. Players of protocol 3 find the new match by polling /api/room/CODE.
    const sockets = [...this.players.entries()].filter(([id]) => id !== 0);
    this.players.clear(); this.limbo.clear(); this.persistLimbo(); this.dcUp.clear(); this.vis.clear(); this.pendingToHost = []; this.pendingBytes = 0;
    for (const [, w] of sockets) {
      try { const a = w.deserializeAttachment() || {}; w.serializeAttachment({ ...a, gone: true }); } catch {}
      if (this.verOf(w) === 3) this.sendNow(w, [this.msg(S_HOST_STATE, [H_RESTART, 0]), this.msg(S_REJECT, [R_HOST_LEFT])]);
      else this.sendNow(w, [this.msg(S_REJECT, [R_HOST_LEFT])]);
      try { w.close(1000, "host left"); } catch {}
    }
    this.ctx.storage.put("meta", m);
    this.lobbyUpdate();
    this.scheduleAlarm();
  }

  // Called for every incoming frame (at most once a second) and by the alarm: silence detection, limbo and restart expiry.
  sweep(now) {
    if (!this.meta || now - this.lastSweep < 1000) return;
    this.lastSweep = now;
    for (const [id, rec] of [...this.limbo]) {
      if (now - rec.since < this.graceS(rec.host) * 1000) continue;
      this.limbo.delete(id);
      if (rec.host) { this.persistLimbo(); this.hostGone(); if (!this.meta) return; } else this.finalizeDrop(id);
    }
    const r = this.meta.restart;
    if (r && now - r.since > RESTART_TOTAL_MS) { this.closeRoom(R_HOST_LEFT); return; }
    // visible protocol 3 sockets that went silent (a dead network does not always close the socket)
    for (const [id, w] of [...this.players]) {
      const i = this.wsInfo.get(w);
      if (!i || i.v !== 3 || this.vis.get(id) === false) continue;
      const seen = this.lastSeen.get(id);
      if (seen === undefined) { this.lastSeen.set(id, now); continue; }
      if (now - seen > (this.meta.staleMs || STALE_MS)) {
        const a = w.deserializeAttachment();
        if (a && !a.gone) { this.enterLimbo(w, a); try { w.close(4002, "silent"); } catch {} }
      }
    }
  }

  // intentional leaves (close code 1000 / 1001: Disconnect button, tab closed) and all protocol 2 sockets end the player at once;
  // anything else (1005/1006, no close frame) is a network drop and gets the grace period.
  socketGone(ws, code) {
    const att = ws.deserializeAttachment();
    if (!att || att.gone || !this.meta || this.players.get(att.id) !== ws) return;
    if (att.v === 3 && att.tok && code !== 1000 && code !== 1001 && code !== 1008 && code !== 1009) this.enterLimbo(ws, att);
    else this.dropPlayer(ws);
  }

  async webSocketClose(ws, code, reason) {
    try { ws.close(code, reason); } catch {}
    this.socketGone(ws, code);
  }

  async webSocketError(ws) { this.socketGone(ws, 1006); }

  async closeRoom(reason) {
    if (!this.meta) return;
    const code = this.meta.code, listed = this.meta.listed;
    const sockets = [...this.players.values()];
    this.meta = null; this.buf = []; this.byRoot.clear(); this.alive.clear(); this.players.clear(); this.limbo.clear(); this.dcUp.clear(); this.bytes = 0;
    this.vis.clear(); this.bufOps.clear(); this.pendingToHost = []; this.pendingBytes = 0; this.tomb = [];
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
      code: m.code, name: m.name, players: m.restart ? 0 : this.players.size + this.limbo.size, max: m.max, type: m.type, pw: !!m.pw, comment: m.comment,
      wave: m.wave || 0, started: !!m.started, ended: !!m.ended, created: m.created, colo: m.colo || "", country: m.country || "",
      lat: m.lat || 0, lon: m.lon || 0, quick: !!m.quick,
    }).catch(() => {});
  }

  // next wake-up: the normal 25 s heartbeat, earlier when a grace period or a restart window ends
  scheduleAlarm() {
    const now = Date.now();
    let at = now + ALARM_MS;
    for (const rec of this.limbo.values()) at = Math.min(at, rec.since + this.graceS(rec.host) * 1000 + 100);
    const r = this.meta && this.meta.restart;
    if (r) at = Math.min(at, r.since + this.claimMs() + 100, r.since + RESTART_TOTAL_MS + 100);
    this.ctx.storage.setAlarm(Math.max(at, now + 50)).catch(() => {});
  }

  async alarm() {
    if (!this.meta) return;
    const now = Date.now();
    this.lastSweep = 0; this.sweep(now);
    if (!this.meta) return;
    if (!this.players.has(0) && !this.limbo.has(0) && !this.meta.restart) { await this.closeRoom(R_HOST_LEFT); return; }
    this.saveSnapshot();
    this.lobbyUpdate();
    this.scheduleAlarm();
  }
}
