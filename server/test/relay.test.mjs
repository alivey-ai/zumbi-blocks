// Relay routing test. Usage: start `npx wrangler dev` (default :8787), then `npm test` (or BASE=host:port node test/relay.test.mjs).
import WebSocket from "ws";
import assert from "node:assert/strict";

const BASE = process.env.BASE || "localhost:8787";
const enc = new TextEncoder();

const view = (owner, counter, sub = 0) => (owner + 1) * 2 ** 40 + counter * 4096 + sub;
const SCENE = (h) => 2 ** 51 + h;
function putView(a, o, v) { const dv = new DataView(a.buffer); dv.setUint32(o, v % 4294967296, true); dv.setUint32(o + 4, Math.floor(v / 4294967296), true); }
function frame(...bodies) {
  let n = 0; for (const b of bodies) n += 2 + b.length;
  const f = new Uint8Array(n); let o = 0;
  for (const b of bodies) { f[o++] = b.length & 255; f[o++] = b.length >> 8; f.set(b, o); o += b.length; }
  return f;
}
const rpc = (mode, v, name, args = [0]) => { const nb = enc.encode(name); const b = new Uint8Array(11 + nb.length + args.length); b[0] = 1; b[1] = mode; putView(b, 2, v); b[10] = nb.length; b.set(nb, 11); b.set(args, 11 + nb.length); return b; };
const inst = (v, prefab) => { const b = new Uint8Array(40); b[0] = 2; b[1] = 0; putView(b, 2, v); new DataView(b.buffer).setUint16(10, prefab, true); return b; };
const destroy = (v) => { const b = new Uint8Array(9); b[0] = 3; putView(b, 1, v); return b; };
const i32 = (op, p) => { const b = new Uint8Array(5); b[0] = op; new DataView(b.buffer).setInt32(1, p, true); return b; };
const listing = (listed, name) => { const n = enc.encode(name); return new Uint8Array([8, listed ? 1 : 0, n.length, ...n, 0]); };

class Client {
  constructor(path, headers) {
    this.msgs = []; this.frames = []; this.texts = []; this.waiters = []; this.closed = false;
    this.ws = new WebSocket(`ws://${BASE}${path}`, { headers: { "X-Zb-Test-Ip": `10.${(Math.random() * 250) | 0}.${(Math.random() * 250) | 0}.${(Math.random() * 250) | 0}`, ...(headers || {}) } });
    this.ws.binaryType = "arraybuffer";
    this.ws.on("message", (d, isBinary) => {
      if (isBinary === false) { const raw = Buffer.from(d).toString(); (this.raw ||= []).push(raw); try { this.texts.push(JSON.parse(raw)); } catch { this.texts.push({ bad: true }); } this.pump(); return; }
      const f = new Uint8Array(d);
      let o = 0; const fr = [];
      while (o + 2 <= f.length) { const len = f[o] | (f[o + 1] << 8); o += 2; const m = f.slice(o, o + len); this.msgs.push(m); fr.push(m); o += len; }
      this.frames.push(fr);
      this.pump();
    });
    this.ws.on("close", () => { this.closed = true; this.pump(); });
    this.opened = new Promise((res, rej) => { this.ws.on("open", res); this.ws.on("error", rej); });
  }
  pump() { for (const w of [...this.waiters]) w(); }
  send(...bodies) { this.ws.send(frame(...bodies)); }
  async take(op, ms = 3000) { // wait for next message with this opcode
    const t0 = Date.now();
    for (;;) {
      const i = this.msgs.findIndex((m) => m[0] === op);
      if (i >= 0) return this.msgs.splice(i, 1)[0];
      if (Date.now() - t0 > ms) throw new Error("timeout waiting for op 0x" + op.toString(16) + " have " + this.msgs.map((m) => m[0].toString(16)));
      await new Promise((r) => { const w = () => { this.waiters = this.waiters.filter((x) => x !== w); r(); }; this.waiters.push(w); setTimeout(w, 50); });
    }
  }
  async none(op, ms = 400) { await sleep(ms); assert.equal(this.msgs.filter((m) => m[0] === op).length, 0, "unexpected op 0x" + op.toString(16)); }
  sendText(o) { this.ws.send(typeof o === "string" ? o : JSON.stringify(o)); }
  async takeText(t, ms = 3000) {
    const t0 = Date.now();
    for (;;) {
      const i = this.texts.findIndex((m) => m.t === t);
      if (i >= 0) return this.texts.splice(i, 1)[0];
      if (Date.now() - t0 > ms) throw new Error("timeout waiting for text " + t + " have " + JSON.stringify(this.texts));
      await new Promise((r) => { const w = () => { this.waiters = this.waiters.filter((x) => x !== w); r(); }; this.waiters.push(w); setTimeout(w, 50); });
    }
  }
  count(op) { return this.msgs.filter((m) => m[0] === op).length; }
  close() { this.ws.close(); }
}
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
async function until(fn, ms = 3000) { const t0 = Date.now(); for (;;) { const v = await fn(); if (v || Date.now() - t0 > ms) return v; await sleep(50); } }
const dv = (m) => new DataView(m.buffer, m.byteOffset, m.byteLength);

async function host(extra = "") {
  const h = new Client(`/ws/host?v=2&name=Test+Room${extra.includes("max=") ? "" : "&max=3"}${extra}`); await h.opened;
  const w = await h.take(0x81);
  const code = new TextDecoder().decode(w.subarray(5 + 3, 5 + 3 + w[7]));
  return { h, code, id: dv(w).getInt32(1, true), isHost: w[5] };
}
async function join(code, extra = "") {
  const c = new Client(`/ws/room/${code}?v=2${extra}`); await c.opened; return c;
}

let ok = 0;
const test = async (name, fn) => { try { await fn(); ok++; console.log("ok   " + name); } catch (e) { console.log("FAIL " + name + "\n" + e.stack); process.exitCode = 1; } };

await test("host creates room, gets code and id 0", async () => {
  const { h, code, id, isHost } = await host();
  assert.equal(id, 0); assert.equal(isHost, 1); assert.match(code, /^[A-Z2-9]{5}$/);
  h.close();
});

await test("room list shows listed rooms only, with players", async () => {
  const a = await host(); const b = await host();
  a.h.send(listing(true, "Public One"));
  b.h.send(listing(false, "Private One"));
  await sleep(300);
  const c = await join(a.code); await c.take(0x81); await c.take(0x8a);
  await sleep(300);
  const rooms = await (await fetch(`http://${BASE}/api/rooms`)).json();
  const mine = rooms.find((r) => r.code === a.code);
  assert.ok(mine, "listed room present"); assert.equal(mine.name, "Public One"); assert.equal(mine.players, 2); assert.equal(mine.max, 3);
  assert.ok(!rooms.find((r) => r.code === b.code), "private room hidden");
  a.h.close(); b.h.close(); c.close();
  await sleep(300);
  const after = await (await fetch(`http://${BASE}/api/rooms`)).json();
  assert.ok(!after.find((r) => r.code === a.code), "removed when host leaves");
});

await test("join: welcome id, host notified, bad code / password / full", async () => {
  const { h, code } = await host("&pw=sekrit");
  const bad = await join(code, "&pw=nope"); const rej = await bad.take(0x87); assert.equal(rej[1], 2);
  const c1 = await join(code, "&pw=sekrit"); const w1 = await c1.take(0x81); assert.equal(dv(w1).getInt32(1, true), 1); assert.equal(w1[5], 0);
  const ev = await h.take(0x85); assert.equal(dv(ev).getInt32(1, true), 1);
  const c2 = await join(code, "&pw=sekrit"); const w2 = await c2.take(0x81); assert.equal(dv(w2).getInt32(1, true), 2);
  const c3 = await join(code, "&pw=sekrit"); assert.equal((await c3.take(0x87))[1], 3); // max 3 incl. host
  const nf = await join("ZZZZZ"); assert.equal((await nf.take(0x87))[1], 1);
  const proto = new Client(`/ws/room/${code}?v=99`); await proto.opened; assert.equal((await proto.take(0x87))[1], 7);
  [h, c1, c2].forEach((x) => x.close());
});

await test("rpc routing: server / others / all / buffered", async () => {
  const { h, code } = await host();
  const a = await join(code); await a.take(0x8a); await h.take(0x85);
  const b = await join(code); await b.take(0x8a); await h.take(0x85);
  const V = SCENE(7);
  // client -> Server: host only
  a.send(rpc(0, V, "ping_server"));
  const r = await h.take(0x82); assert.equal(dv(r).getInt32(1, true), 1); await b.none(0x82);
  // host Others: both clients
  h.send(rpc(1, V, "others")); await a.take(0x82); await b.take(0x82);
  // a All: host + b, not a
  a.send(rpc(2, V, "all")); await h.take(0x82); await b.take(0x82); await a.none(0x82);
  // a OthersBuffered: host + b, buffered for late joiner
  a.send(rpc(5, V, "buffered1")); await h.take(0x82); await b.take(0x82);
  const c = await join(code, ""); // full (host,a,b)
  assert.equal((await c.take(0x87))[1], 3);
  b.close(); await sleep(200);
  const dis = await h.take(0x86); assert.equal(dv(dis).getInt32(1, true), 2);
  const d = await join(code); await d.take(0x81);
  const rep = await d.take(0x82); assert.equal(dv(rep).getInt32(1, true), 1); await d.take(0x8a);
  [h, a, d].forEach((x) => x.close());
});

await test("buffered instantiate + rpc replay, destroy and killmebuffered compaction", async () => {
  const { h, code } = await host();
  const v1 = view(0, 1), v2 = view(0, 2), v3 = view(0, 3);
  h.send(inst(v1, 5), inst(v2, 6), inst(v3, 7), rpc(6, v1, "setskin"), rpc(6, v2, "killmebuffered"), rpc(6, v1, "killmebuffered"));
  h.send(destroy(v3));
  await sleep(200);
  const late = await join(code); await late.take(0x81);
  // everything was killed/destroyed: nothing but SYNC_DONE
  await late.take(0x8a); assert.equal(late.count(0x82) + late.count(0x83), 0);
  // alive object replays instantiate then its rpc, in order
  h.send(inst(view(0, 9), 11), rpc(6, view(0, 9), "setskin"), rpc(6, view(0, 9, 5), "childrpc"));
  await late.take(0x83); await late.take(0x82); await late.take(0x82);
  const late2 = await join(code); await late2.take(0x81);
  await late2.take(0x8a);
  const types = late2.msgs.map((m) => m[0]);
  assert.deepEqual(types.sort(), [0x82, 0x82, 0x83]);
  [h, late, late2].forEach((x) => x.close());
});

await test("client cannot instantiate with someone else's view id; owner cleanup via destroy-player", async () => {
  const { h, code } = await host();
  const a = await join(code); await a.take(0x8a); await h.take(0x85);
  a.send(inst(view(0, 1), 1)); await h.none(0x83); // spoofed owner 0
  a.send(inst(view(1, 1), 2)); const i = await h.take(0x83); assert.equal(dv(i).getInt32(1, true), 1);
  h.send(i32(5, 1)); // DestroyPlayerObjects(1)
  const d = await h.take(0x84); assert.equal(dv(d).getUint32(1, true), (view(1, 1)) % 4294967296);
  [h, a].forEach((x) => x.close());
});

await test("kick and host leaving close everything cleanly", async () => {
  const { h, code } = await host();
  const a = await join(code); await a.take(0x8a); await h.take(0x85);
  const b = await join(code); await b.take(0x8a); await h.take(0x85);
  h.send(i32(6, 1)); // kick player 1
  assert.equal((await a.take(0x87))[1], 5);
  await h.take(0x86);
  h.close();
  assert.equal((await b.take(0x87))[1], 4);
  await sleep(300);
  const gone = await join(code); assert.equal((await gone.take(0x87))[1], 1); // room is gone
});

await test("ping/pong and ping info reaches host", async () => {
  const { h, code } = await host();
  const a = await join(code); await a.take(0x8a); await h.take(0x85);
  a.send(new Uint8Array([7, 1, 2, 3, 4, 44, 0]));
  const p = await a.take(0x88); assert.deepEqual([...p.subarray(1)], [1, 2, 3, 4]);
  const info = await h.take(0x89); assert.equal(dv(info).getInt32(1, true), 1); assert.equal(info[5], 44);
  [h, a].forEach((x) => x.close());
});


// ---- feel layer (protocol 2): frame batching, sender clock, compact state messages, end-to-end echo ----
const stamp = (ms) => { const b = new Uint8Array(5); b[0] = 9; new DataView(b.buffer).setUint32(1, ms, true); return b; };
const state = (kind, v, payloadLen) => { const b = new Uint8Array(10 + payloadLen); b[0] = 11; b[1] = kind; putView(b, 2, v); for (let i = 0; i < payloadLen; i++) b[10 + i] = (i * 7 + kind) & 255; return b; };
const echo = (type, target, t) => { const b = new Uint8Array(10); b[0] = 10; b[1] = type; new DataView(b.buffer).setInt32(2, target, true); new DataView(b.buffer).setUint32(6, t, true); return b; };

await test("batching: everything from one frame reaches each recipient as one frame, in order, led by the sender's clock", async () => {
  const { h, code } = await host();
  const a = await join(code); await a.take(0x8a); await h.take(0x85);
  const b = await join(code); await b.take(0x8a); await h.take(0x85);
  a.frames.length = 0; b.frames.length = 0;
  const V = SCENE(11);
  h.send(stamp(123456), state(1, V, 14), rpc(1, V, "r1"), state(2, V, 28), rpc(1, V, "r2"), state(3, V, 12));
  await a.take(0x8d); await b.take(0x8d); await sleep(150);
  for (const c of [a, b]) {
    assert.equal(c.frames.length, 1, "one WebSocket frame per recipient");
    assert.deepEqual(c.frames[0].map((m) => m[0]), [0x8b, 0x8d, 0x82, 0x8d, 0x82, 0x8d], "S_TIME first, then the messages in send order");
    const t = c.frames[0][0]; assert.equal(dv(t).getInt32(1, true), 0); assert.equal(dv(t).getUint32(5, true), 123456);
  }
  // a frame without clock stays without S_TIME
  a.frames.length = 0;
  h.send(rpc(1, V, "noclock")); await sleep(200); assert.equal(a.frames.length, 1); assert.equal(a.frames[0].map((m) => m[0]).join(), "130");
  // a client frame going to two different recipients (host + other client): one frame each
  h.frames.length = 0; b.frames.length = 0;
  a.send(stamp(5), rpc(2, V, "x1"), rpc(2, V, "x2"), rpc(2, V, "x3"));
  await h.take(0x82); await b.take(0x82); await sleep(150);
  assert.equal(h.frames.length, 1); assert.equal(b.frames.length, 1);
  assert.equal(h.frames[0].length, 4); assert.equal(dv(h.frames[0][0]).getInt32(1, true), 1);
  [h, a, b].forEach((x) => x.close());
});

await test("batching: a large frame (many messages) is routed completely", async () => {
  const { h, code } = await host();
  const a = await join(code); await a.take(0x8a); await h.take(0x85);
  a.frames.length = 0;
  const bodies = [stamp(1)]; for (let i = 0; i < 400; i++) bodies.push(state(1, SCENE(100 + i), 14));
  h.send(...bodies);
  await sleep(500);
  assert.equal(a.count(0x8d), 400); assert.equal(a.frames.length, 1);
  [h, a].forEach((x) => x.close());
});

await test("state messages: forwarded to the others only, unbuffered, bad lengths dropped", async () => {
  const { h, code } = await host();
  const a = await join(code); await a.take(0x8a); await h.take(0x85);
  const b = await join(code); await b.take(0x8a); await h.take(0x85);
  const V = SCENE(21);
  const s1 = state(1, V, 14), s2 = state(2, V, 28), s3 = state(3, V, 12);
  a.send(s1, s2, s3);
  for (const [i, s] of [s1, s2, s3].entries()) {
    const gh = await h.take(0x8d), gb = await b.take(0x8d);
    assert.deepEqual([...gh.subarray(1)], [...s.subarray(1)], "payload kind " + (i + 1) + " to host");
    assert.deepEqual([...gb.subarray(1)], [...s.subarray(1)]);
  }
  await a.none(0x8d, 200);
  a.send(state(1, V, 13), state(4, V, 14), new Uint8Array([11]));
  await h.none(0x8d, 300);
  b.close(); await sleep(200);
  const late = await join(code); await late.take(0x8a); assert.equal(late.count(0x8d), 0, "state is not replayed to late joiners");
  [h, a, b, late].forEach((x) => x.close());
});

await test("echo: client <-> host only, sender id stamped, payload preserved", async () => {
  const { h, code } = await host();
  const a = await join(code); await a.take(0x8a); await h.take(0x85);
  const b = await join(code); await b.take(0x8a); await h.take(0x85);
  a.send(echo(0, 0, 0xdeadbeef));
  const e = await h.take(0x8c); assert.equal(dv(e).getInt32(1, true), 1); assert.equal(e[5], 0); assert.equal(dv(e).getUint32(6, true), 0xdeadbeef);
  h.send(echo(1, 1, 777));
  const r = await a.take(0x8c); assert.equal(dv(r).getInt32(1, true), 0); assert.equal(r[5], 1); assert.equal(dv(r).getUint32(6, true), 777);
  a.send(echo(0, 2, 1)); // client to another client: not allowed
  b.send(echo(0, 1, 1));
  await b.none(0x8c, 250); await a.none(0x8c, 10); await h.none(0x8c, 10);
  h.send(echo(0, 9, 1), echo(0, 0, 1)); // unknown target / to self
  await sleep(200); assert.equal(h.count(0x8c) + a.count(0x8c) + b.count(0x8c), 0);
  [h, a, b].forEach((x) => x.close());
});

// ---- Phase 2: WebRTC signaling + data channel flag (text frames) --------------------------------------------
await test("rtc: hello only with rtc=1, legacy clients never get text frames", async () => {
  const { h, code } = await host("&rtc=1&max=5");
  const hh = await h.takeText("hello"); assert.deepEqual(hh, { t: "hello", id: 0, host: true });
  const a = await join(code, "&rtc=1"); const ha = await a.takeText("hello"); assert.deepEqual(ha, { t: "hello", id: 1, host: false });
  const old = await join(code); await old.take(0x8a); await sleep(200); assert.equal(old.texts.length, 0);
  [h, a, old].forEach((x) => x.close());
});

await test("rtc: sig routing, star only, from stamped, malformed / oversize / rate limited dropped", async () => {
  const { h, code } = await host("&rtc=1&max=5"); await h.takeText("hello");
  const a = await join(code, "&rtc=1"); await a.takeText("hello");
  const b = await join(code, "&rtc=1"); await b.takeText("hello");
  const legacy = await join(code); await legacy.take(0x8a);
  a.sendText({ t: "sig", to: 0, gen: 3, k: "offer", sdp: "v=0", from: 99 });
  const s = await h.takeText("sig"); assert.equal(s.from, 1); assert.equal(s.gen, 3); assert.equal(s.k, "offer"); assert.equal(s.sdp, "v=0");
  h.sendText({ t: "sig", to: 1, gen: 3, k: "answer", sdp: "v=1" });
  const ans = await a.takeText("sig"); assert.equal(ans.from, 0); assert.equal(ans.k, "answer");
  a.sendText({ t: "sig", to: 2, gen: 1, k: "offer", sdp: "x" }); // client to client
  a.sendText({ t: "sig", to: 0, gen: 1, k: "nope" });             // bad kind
  a.sendText("{not json"); a.sendText({ t: "sig", to: "0", k: "ice" });
  a.sendText({ t: "sig", to: 0, gen: 1, k: "ice", sdp: "y".repeat(9000) }); // oversize
  h.sendText({ t: "sig", to: 3, gen: 1, k: "offer" });            // host to a legacy client (no rtc)
  h.sendText({ t: "sig", to: 77, gen: 1, k: "offer" });           // unknown target
  await sleep(300);
  assert.equal(b.texts.length, 0); assert.equal(h.texts.length, 0); assert.equal(legacy.texts.length, 0);
  for (let i = 0; i < 80; i++) a.sendText({ t: "sig", to: 0, gen: 1, k: "ice", c: { candidate: "c" + i } });
  await sleep(500);
  const got = h.texts.filter((m) => m.t === "sig").length; assert.ok(got <= 60 && got >= 50, "rate limited to ~60/10s, got " + got);
  [h, a, b, legacy].forEach((x) => x.close());
});

await test("rtc: dc flag moves host state off the websocket of that client only; rpc unaffected; flag cleared by client / host / close", async () => {
  const { h, code } = await host("&rtc=1&max=5"); await h.takeText("hello");
  const a = await join(code, "&rtc=1"); await a.take(0x8a); await h.take(0x85);
  const b = await join(code, "&rtc=1"); await b.take(0x8a); await h.take(0x85);
  const V = SCENE(31);
  h.send(state(1, V, 14)); await a.take(0x8d); await b.take(0x8d);        // no flags yet: both get it
  a.sendText({ t: "dc", up: true }); await sleep(200);
  h.send(state(1, V, 14)); await b.take(0x8d); await a.none(0x8d, 250);   // a is on the data channel now
  h.send(rpc(1, V, "hit")); await a.take(0x82); await b.take(0x82);       // RPCs still go through the relay
  b.send(state(1, V, 14)); await h.take(0x8d); await a.take(0x8d);         // client state is unaffected
  h.sendText({ t: "dc", to: 1, up: true }); await sleep(100);            // the host cannot set flags...
  b.sendText({ t: "dc", up: true }); await sleep(100); h.send(state(1, V, 14)); await a.none(0x8d, 200); await b.none(0x8d, 10);
  b.sendText({ t: "dc", up: false }); await sleep(100); h.send(state(1, V, 14)); await b.take(0x8d); await a.none(0x8d, 150);
  h.sendText({ t: "dc", to: 1, up: false }); await sleep(100);           // ...but may clear them
  h.send(state(1, V, 14)); await a.take(0x8d); await b.take(0x8d);
  a.sendText({ t: "dc", up: true }); await sleep(100); a.close(); await sleep(200);
  const a2 = await join(code, "&rtc=1"); await a2.take(0x8a); await h.take(0x85); // new id, flag of the old one is gone
  h.send(state(1, V, 14)); await a2.take(0x8d);
  const legacy = await join(code); await legacy.take(0x8a); legacy.sendText({ t: "dc", up: true }); await sleep(100); // no rtc: ignored
  h.send(state(1, V, 14)); await legacy.take(0x8d);
  [h, b, a2, legacy].forEach((x) => x.close());
});

const meta = (wave, flags, alive, cd) => { const b = new Uint8Array(cd === undefined ? 5 : 6); b[0] = 16; b[1] = wave & 255; b[2] = wave >> 8; b[3] = flags; b[4] = alive; if (cd !== undefined) b[5] = cd; return b; };
const T = "Zumbiblocks test";
const api = async (p) => { const r = await fetch(`http://${BASE}${p}`); return { status: r.status, body: await r.json() }; };

await test("phase 3: /api/room/:code info, no secrets, 404 for unknown / closed rooms", async () => {
  const a = await host("&pw=secret&type=" + encodeURIComponent(T)); await sleep(200);
  let r = await api(`/api/room/${a.code.toLowerCase()}`);
  assert.equal(r.status, 200); assert.equal(r.body.exists, true); assert.equal(r.body.players, 1); assert.equal(r.body.pw, true); assert.equal(r.body.type, T);
  assert.equal(r.body.started, false); assert.equal(r.body.wave, 0);
  assert.ok(!JSON.stringify(r.body).includes("secret"), "no password in the response");
  r = await api("/api/room/ZZZZZ"); assert.equal(r.status, 404); assert.equal(r.body.exists, false);
  r = await api("/api/room/bad%20code!"); assert.equal(r.status, 404);
  const ping = await api(`/api/ping/${a.code}`); assert.equal(ping.status, 200);
  a.h.close(); await sleep(300);
  r = await api(`/api/room/${a.code}`); assert.equal(r.status, 404);
});

await test("phase 3: C_META updates the room list (wave, started, ended), only from the host, malformed ignored; no lat/lon published", async () => {
  const a = await host("&type=" + encodeURIComponent(T)); a.h.send(listing(true, "Meta Room")); await sleep(200);
  const c = await join(a.code); await c.take(0x8a);
  c.send(meta(9, 1, 1)); await sleep(200);
  let row = (await (await fetch(`http://${BASE}/api/rooms`)).json()).find((r) => r.code === a.code);
  assert.equal(row.wave, 0, "a client cannot report meta");
  a.h.send(new Uint8Array([16, 1, 2])); a.h.send(new Uint8Array([16, 3, 0, 1, 2, 3, 4, 5, 6])); await sleep(200);
  row = (await (await fetch(`http://${BASE}/api/rooms`)).json()).find((r) => r.code === a.code);
  assert.equal(row.wave, 0, "malformed ignored");
  a.h.send(meta(7, 1, 2)); await sleep(300);
  row = (await (await fetch(`http://${BASE}/api/rooms`)).json()).find((r) => r.code === a.code);
  assert.equal(row.wave, 7); assert.equal(row.started, true); assert.equal(row.ended, false); assert.ok(row.age >= 0);
  assert.ok(!("lat" in row) && !("lon" in row) && !("created" in row), "no host location in the public list");
  const info = (await api(`/api/room/${a.code}`)).body; assert.equal(info.wave, 7); assert.equal(info.started, true);
  a.h.send(meta(7, 3, 0)); await sleep(300);
  assert.equal((await api(`/api/room/${a.code}`)).body.ended, true);
  a.h.close(); c.close();
});

await test("phase 3: auto start countdown reaches the other players (host sends it, relay forwards)", async () => {
  const a = await host("&type=" + encodeURIComponent(T)); const c = await join(a.code); await c.take(0x8a);
  a.h.send(meta(0, 0, 1, 9)); const m = await c.take(0x95); assert.equal(m[1], 9);
  a.h.send(meta(0, 0, 1, 0)); const m2 = await c.take(0x95); assert.equal(m2[1], 0);
  a.h.send(meta(0, 1, 1, 0)); await c.none(0x95, 300);
  c.send(meta(0, 0, 1, 5)); await a.h.none(0x95, 300);
  a.h.close(); c.close();
});

await test("phase 3: /api/quickplay offers joinable rooms of the same build only, best first", async () => {
  const TY = "Zumbiblocks qp" + Date.now();
  const mk = async (name, extra, players, m) => {
    const r = await host("&max=4&type=" + encodeURIComponent(TY) + extra); r.h.send(listing(true, name)); const cl = [];
    for (let i = 1; i < players; i++) { const c = await join(r.code, extra.includes("pw=") ? "&pw=x" : ""); await c.take(0x8a); cl.push(c); }
    if (m) r.h.send(m); r.cl = cl; return r;
  };
  const one = await mk("QP one", "", 1);
  const three = await mk("QP three", "", 3, meta(2, 1, 3));
  const full = await mk("QP full", "", 4);
  const pw = await mk("QP pw", "&pw=x", 3);
  const ended = await mk("QP ended", "", 2, meta(5, 3, 0));
  const other = await host("&max=4&type=" + encodeURIComponent("Zumbiblocks other")); other.h.send(listing(true, "QP other build"));
  await sleep(500);
  const r = await api("/api/quickplay?type=" + encodeURIComponent(TY));
  assert.equal(r.status, 200);
  assert.deepEqual(r.body.map((x) => x.name), ["QP three", "QP one"], JSON.stringify(r.body.map((x) => x.name)));
  assert.ok(r.body.every((x) => !x.pw && x.players < x.max && x.type === TY));
  assert.deepEqual((await api("/api/quickplay?type=nope")).body, []);
  for (const x of [one, three, full, pw, ended, other]) { x.h.close(); (x.cl || []).forEach((c) => c.close()); }
});


// ---- phase 4 step 1: rate and size limits (needs `wrangler dev --var ZB_TEST:1`: the test IP header is honoured only then) -----------
const ipH = (ip) => ({ "X-Zb-Test-Ip": ip });
await test("limits: /api/* is limited per IP (429 + retry-after), other IPs and ping are unaffected, normal list polling never trips", async () => {
  const ip = "10.1.1." + (Math.random() * 200 | 0);
  // a client with the list open: 1 list + 16 pings per 10 s = 100 requests a minute (ping has its own, larger bucket)
  for (let i = 0; i < 100; i++) assert.equal((await fetch(`http://${BASE}/api/ping/ABCDE`, { headers: ipH(ip) })).status, 200);
  let first429 = -1;
  for (let i = 0; i < 260; i++) { const r = await fetch(`http://${BASE}/api/rooms`, { headers: ipH(ip) }); if (r.status === 429 && first429 < 0) { first429 = i; assert.ok(Number(r.headers.get("retry-after")) > 0); } }
  assert.ok(first429 >= 200 && first429 <= 240, "limit kicks in around 240/min, got " + first429);
  assert.equal((await fetch(`http://${BASE}/api/rooms`, { headers: ipH("10.7.7.7") })).status, 200, "other IP unaffected");
  assert.equal((await fetch(`http://${BASE}/api/ping/ABCDE`, { headers: ipH(ip) })).status !== 429, true, "ping has its own bucket");
});

await test("limits: new rooms 20/min per IP (REJECT 6 on a normal socket), other IPs fine, joining from the same NAT is not affected", async () => {
  const ip = "10.3.3." + (Math.random() * 200 | 0);
  const hs = [];
  for (let i = 0; i < 20; i++) { const h = new Client("/ws/host?v=2&max=3", ipH(ip)); await h.opened; await h.take(0x81); hs.push(h); }
  const over = new Client("/ws/host?v=2&max=3", ipH(ip)); await over.opened;
  assert.equal((await over.take(0x87))[1], 6);
  const other = new Client("/ws/host?v=2&max=3", ipH("10.4.4.4")); await other.opened; await other.take(0x81);
  // same NAT as the exhausted creator: joining an existing room uses another bucket
  const r = new Client("/ws/room/ZZZZZ?v=2", ipH(ip)); await r.opened;
  assert.equal((await r.take(0x87))[1], 1, "not rate limited, plain room-not-found");
  r.close();
  hs.push(other); hs.forEach((x) => x.close());
});

await test("limits: joins 90/min and rejoins 180/min are separate buckets", async () => {
  const ip = "10.5.5." + (Math.random() * 200 | 0);
  let limited = -1;
  for (let i = 0; i < 100; i++) { const c = new Client("/ws/room/ZZZZZ?v=2", ipH(ip)); await c.opened; const r = await c.take(0x87); if (r[1] === 6 && limited < 0) limited = i; c.close(); }
  assert.ok(limited >= 85 && limited <= 92, "join limit around 90, got " + limited);
  const rj = new Client("/ws/room/ZZZZZ?v=2&rejoin=1.abcd", ipH(ip)); await rj.opened;
  assert.notEqual((await rj.take(0x87))[1], 6, "rejoin bucket is not exhausted by first joins");
  rj.close();
});

await test("limits: a flood of tiny messages is dropped, then the socket is closed; the host and other clients are unaffected", async () => {
  const { h, code } = await host();
  const a = await join(code); await a.take(0x8a); await h.take(0x85);
  const b = await join(code); await b.take(0x8a); await h.take(0x85);
  const ping = new Uint8Array([7, 0, 0, 0, 0, 0]);
  const burst = Array.from({ length: 500 }, () => ping);
  for (let i = 0; i < 40 && !a.closed; i++) { try { a.send(...burst); } catch { break; } await sleep(5); }
  const rej = await a.take(0x87, 4000); assert.equal(rej[1], 6);
  await sleep(300);
  assert.ok(a.closed, "flooder closed");
  // normal client still works
  b.send(rpc(0, SCENE(1), "hello")); await h.take(0x82);
  [h, b].forEach((x) => x.close());
});


// ---- phase 4 steps 3-5: protocol 3 (resume, limbo, host states, claim restart) --------------------------------------------------------
// the test knobs grace=, tstale=, tclaim= exist only with `wrangler dev --var ZB_TEST:1`
const td = new TextDecoder();
function parseWelcome(m) {
  const d = dv(m); const cl = m[7]; const o = 8 + cl;
  const out = { id: d.getInt32(1, true), isHost: m[5], max: m[6], code: td.decode(m.subarray(8, o)), ext: null, len: m.length };
  if (m.length > o) { const tl = m[o]; const tok = td.decode(m.subarray(o + 1, o + 1 + tl)); const p = o + 1 + tl;
    out.ext = { tok, grace: m[p], epoch: m[p + 1], flags: m[p + 2], hostId: d.getInt32(p + 3, true), accepted: d.getUint32(p + 7, true) }; }
  return out;
}
async function host3(extra = "") {
  const h = new Client(`/ws/host?v=3&name=P4${extra.includes("max=") ? "" : "&max=4"}&grace=2&tstale=2500&tclaim=1500${extra}`); await h.opened;
  const w = parseWelcome(await h.take(0x81));
  return { h, code: w.code, w, tok: w.ext.tok };
}
async function join3(code, extra = "") { const c = new Client(`/ws/room/${code}?v=3${extra}`); await c.opened; const w = parseWelcome(await c.take(0x81)); return { c, w, tok: w.ext && w.ext.tok }; }
async function resume3(code, id, tok, seq, extra = "") { const c = new Client(`/ws/room/${code}?v=3&rejoin=${id}.${tok}&seq=${seq}${extra}`); await c.opened; return c; }
const seqOf = (m) => dv(m).getUint32(1, true);
const info = async (code) => (await fetch(`http://${BASE}/api/room/${code}`)).json();
const vis = (v) => new Uint8Array([17, v ? 1 : 0, 60, 0, 0]);
async function drain(c, ms = 300) { await sleep(ms); const l = c.msgs.splice(0); return l; }

await test("p4: protocol 3 welcome carries token / grace / epoch; protocol 2 welcome is unchanged", async () => {
  const { h, code, w } = await host3();
  assert.equal(w.ext.grace, 2); assert.equal(w.ext.flags, 0); assert.equal(w.ext.tok.length, 32); assert.equal(w.ext.hostId, 0);
  const a = await join3(code); assert.equal(a.w.id, 1); assert.equal(a.w.ext.tok.length, 32); assert.notEqual(a.tok, w.ext.tok);
  const v2 = await join(code); const w2 = await v2.take(0x81); assert.equal(w2.length, 8 + w2[7], "v2 welcome has no extension");
  const hd = await host(); const d = await hd.h.take.call(hd.h, 0x8e, 200).catch(() => null); assert.equal(d, null);
  [h, a.c, v2, hd.h].forEach((x) => x.close());
});

await test("p4: client socket dies -> slot kept, host not told, resume replays only what was missed (not own entries), token rotates", async () => {
  const { h, code } = await host3(); await h.take(0x81).catch(() => {});
  const a = await join3(code); await h.take(0x85); await a.c.take(0x8a);
  const A = view(0, 1), B = view(0, 2), C = view(1, 1);
  h.send(inst(A, 3)); await a.c.take(0x83); const s1 = seqOf(await a.c.take(0x8e));
  a.c.send(inst(C, 4)); await h.take(0x83); // a's own buffered op
  a.c.send(rpc(5, C, "own_rpc")); await h.take(0x82);
  a.c.ws.terminate(); await sleep(400);
  await h.none(0x86, 300);                     // host is not told
  h.send(rpc(5, A, "missed_rpc")); h.send(inst(B, 5)); await sleep(150);
  const c2 = await resume3(code, 1, a.tok, s1);
  const w = parseWelcome(await c2.take(0x81));
  assert.equal(w.id, 1); assert.equal(w.ext.flags & 1, 1); assert.notEqual(w.ext.tok, a.tok); assert.equal(w.ext.accepted, 2, "relay parsed 2 buffered ops from this player");
  const m1 = await c2.take(0x82); assert.equal(td.decode(m1.subarray(15, 15 + m1[14])), "missed_rpc");
  const m2 = await c2.take(0x83); assert.equal(dv(m2).getInt32(1, true), 0);
  assert.ok(seqOf(await c2.take(0x8e)) > s1); await c2.take(0x8a);
  await c2.none(0x83, 200); // no replay of A (already had it) nor of its own C
  assert.equal(h.count(0x85), 0); await h.none(0x85, 100);
  const again = await resume3(code, 1, a.tok, s1); assert.equal((await again.take(0x87))[1], 10, "old token is dead");
  // the resumed socket works both ways
  h.send(rpc(1, SCENE(9), "live")); await c2.take(0x82); c2.send(rpc(0, SCENE(9), "toserver")); await h.take(0x82);
  [h, c2].forEach((x) => x.close());
});

await test("p4: an object destroyed while the client was away is destroyed on resume (tombstone)", async () => {
  const { h, code } = await host3();
  const a = await join3(code); await h.take(0x85); await a.c.take(0x8a);
  const A = view(0, 7);
  h.send(inst(A, 3)); await a.c.take(0x83); const s = seqOf(await a.c.take(0x8e));
  a.c.ws.terminate(); await sleep(300);
  h.send(rpc(5, A, "killmebuffered")); await sleep(100);
  const c2 = await resume3(code, 1, a.tok, s); await c2.take(0x81);
  const d = await c2.take(0x84); assert.equal(readViewT(d, 1), A);
  await c2.take(0x8a);
  [h, c2].forEach((x) => x.close());
});
function readViewT(b, o) { const d = dv(b); return d.getUint32(o + 4, true) * 4294967296 + d.getUint32(o, true); }

await test("p4: grace over -> player dropped for real (host told once), resume refused; limbo counts toward capacity until then", async () => {
  const { h, code } = await host3("&max=3");
  const a = await join3(code); await h.take(0x85); const b = await join3(code); await h.take(0x85);
  a.c.ws.terminate(); await sleep(300);
  const full = new Client(`/ws/room/${code}?v=3`); await full.opened; assert.equal((await full.take(0x87))[1], 3, "limbo slot is reserved");
  assert.equal((await info(code)).players, 3);
  await sleep(2600);
  const dis = await h.take(0x86, 1500); assert.equal(dv(dis).getInt32(1, true), 1); assert.equal(h.count(0x86), 0);
  const late = await resume3(code, 1, a.tok, 0); assert.equal((await late.take(0x87))[1], 10);
  assert.equal((await info(code)).players, 2);
  const fresh = await join3(code); assert.equal(fresh.w.id, 3, "ids are never reused");
  [h, b.c, fresh.c].forEach((x) => x.close());
});

await test("p4: intentional leave (close 1000 / 1001) skips limbo", async () => {
  const { h, code } = await host3();
  const a = await join3(code); await h.take(0x85);
  a.c.ws.close(1000, "bye"); const dis = await h.take(0x86, 1000); assert.equal(dv(dis).getInt32(1, true), 1);
  const b = await join3(code); await h.take(0x85); b.c.ws.close(1001, "going away"); await h.take(0x86, 1000);
  h.close();
});

await test("p4: v2 sockets keep today's behaviour (no grace; v2 host leaving closes the room)", async () => {
  const { h, code } = await host();
  const c = await join(code); await c.take(0x8a); await h.take(0x85);
  c.ws.terminate(); const dis = await h.take(0x86, 1000); assert.equal(dv(dis).getInt32(1, true), 1);
  const d = await join(code); await d.take(0x8a);
  h.ws.terminate(); assert.equal((await d.take(0x87, 2000))[1], 4);
  assert.equal((await info(code)).exists, false);
  d.close();
});

await test("p4: host socket dies -> clients see 'away', joins are refused (9), requests to the host are queued and delivered on resume", async () => {
  const { h, code, tok } = await host3();
  const a = await join3(code); await h.take(0x85); await a.c.take(0x8a);
  const b = await join3(code); await h.take(0x85); await b.c.take(0x8a);
  h.ws.terminate(); await sleep(300);
  const hs = await a.c.take(0x8f); assert.equal(hs[1], 1); assert.equal(hs[2], 2);
  assert.equal((await info(code)).hostState, "away");
  const j = new Client(`/ws/room/${code}?v=3`); await j.opened; assert.equal((await j.take(0x87))[1], 9);
  a.c.send(rpc(0, SCENE(3), "toServer1")); b.c.send(rpc(0, SCENE(3), "toServer2"));
  b.c.ws.close(1000, "bye"); await sleep(200);
  const h2 = await resume3(code, 0, tok, 0);
  const w = parseWelcome(await h2.take(0x81)); assert.equal(w.id, 0); assert.equal(w.isHost, 1);
  const got = []; for (let i = 0; i < 3; i++) got.push((await h2.take(i < 2 ? 0x82 : 0x86)));
  assert.ok(await a.c.take(0x8f)); // back to ok
  assert.equal((await info(code)).hostState, "ok");
  const j2 = await join3(code); assert.ok(j2.w.id >= 3);
  h2.send(rpc(1, SCENE(4), "afterwards")); await a.c.take(0x82);
  [h2, a.c, j2.c].forEach((x) => x.close());
});

await test("p4: host leaves for good (clean close) with protocol 3 players -> claim restart: successor claims the code, others rejoin a fresh match", async () => {
  const { h, code } = await host3();
  const a = await join3(code); await h.take(0x85); const b = await join3(code); await h.take(0x85);
  a.c.send(new Uint8Array([7, 0, 0, 0, 0, 90, 0])); b.c.send(new Uint8Array([7, 0, 0, 0, 0, 20, 0])); await sleep(200); // b has the lower rtt
  h.send(listing(true, "Restart Me")); h.send(new Uint8Array([16, 4, 0, 1, 2])); await sleep(200);
  h.ws.close(1000, "bye");
  for (const c of [a.c, b.c]) { const hs = await c.take(0x8f); assert.equal(hs[1], 3); assert.equal((await c.take(0x87))[1], 4); }
  const i1 = await info(code); assert.equal(i1.exists, true); assert.equal(i1.hostState, "restarting"); assert.equal(i1.restart.successor, 2); assert.equal(i1.restart.claimOpen, false); assert.equal(i1.epoch, 1);
  assert.equal(i1.restart.wave, 4, "level B+: the last wave of the started match is kept for the successor");
  const nj = new Client(`/ws/room/${code}?v=3`); await nj.opened; assert.equal((await nj.take(0x87))[1], 9);
  // the other player may not claim during the successor's head start
  const early = new Client(`/ws/host?v=3&claim=${code}.1.${a.tok}`); await early.opened; assert.equal((await early.take(0x87))[1], 9);
  const bad = new Client(`/ws/host?v=3&claim=${code}.2.nottoken`); await bad.opened; assert.equal((await bad.take(0x87))[1], 10);
  // the successor claims
  const nh = new Client(`/ws/host?v=3&claim=${code}.2.${b.tok}`); await nh.opened;
  const w = parseWelcome(await nh.take(0x81)); assert.equal(w.id, 0); assert.equal(w.isHost, 1); assert.equal(w.ext.flags & 2, 2); assert.equal(w.code, code); assert.equal(w.ext.epoch, 1);
  const i2 = await info(code); assert.equal(i2.hostState, "ok"); assert.equal(i2.epoch, 1); assert.equal(i2.name, "Restart Me"); assert.equal(i2.started, false); assert.equal(i2.wave, 0);
  const late = new Client(`/ws/host?v=3&claim=${code}.1.${a.tok}`); await late.opened; assert.equal((await late.take(0x87))[1], 8, "already claimed");
  const rooms = await (await fetch(`http://${BASE}/api/rooms`)).json(); assert.ok(rooms.find((r) => r.code === code), "stays listed");
  const again = await join3(code); assert.equal(again.w.id, 1, "ids start again"); await nh.take(0x85);
  const r = await resume3(code, 1, a.tok, 0); assert.equal((await r.take(0x87))[1], 10, "old match tokens are dead");
  [nh, again.c].forEach((x) => x.close());
});

await test("p4: successor does not claim -> after the head start any remaining player may; nobody claims -> room closes; a lone host leaving closes at once", async () => {
  const { h, code } = await host3();
  const a = await join3(code); await h.take(0x85); const b = await join3(code); await h.take(0x85);
  h.ws.close(1000, "bye"); await a.c.take(0x87); await b.c.take(0x87);
  const i1 = await info(code); const other = i1.restart.successor === 1 ? { id: 2, tok: b.tok } : { id: 1, tok: a.tok };
  assert.equal(i1.restart.wave, 0, "no started match, no wave");
  await sleep(1700);
  assert.equal((await info(code)).restart.claimOpen, true);
  const nh = new Client(`/ws/host?v=3&claim=${code}.${other.id}.${other.tok}`); await nh.opened; assert.equal(parseWelcome(await nh.take(0x81)).id, 0);
  nh.close(); await sleep(200);
  const { h: lone, code: c2 } = await host3(); lone.ws.close(1000, "bye"); await sleep(300);
  assert.equal((await info(c2)).exists, false);
});

await test("p4: host socket dies and does not come back -> grace over -> claim restart", async () => {
  const { h, code } = await host3();
  const a = await join3(code); await h.take(0x85); await a.c.take(0x8a);
  h.ws.terminate();
  await a.c.take(0x8f);                              // away
  const hs = await a.c.take(0x8f, 4500); assert.equal(hs[1], 3);
  assert.equal((await a.c.take(0x87))[1], 4);
  assert.equal((await info(code)).hostState, "restarting");
  const nh = new Client(`/ws/host?v=3&claim=${code}.1.${a.tok}`); await nh.opened; assert.equal(parseWelcome(await nh.take(0x81)).id, 0); nh.close();
});

await test("p4: hidden host tab is announced (S_HOST_STATE hidden), hidden tabs are never limboed for silence, visible silent ones are", async () => {
  const { h, code } = await host3();
  const a = await join3(code); await h.take(0x85); await a.c.take(0x8a);
  const b = await join3(code); await h.take(0x85); await b.c.take(0x8a);
  h.send(vis(false)); const s1 = await a.c.take(0x8f); assert.equal(s1[1], 2); await b.c.take(0x8f);
  assert.equal((await info(code)).hostState, "hidden");
  const late = await join3(code); assert.equal((await late.c.take(0x8f))[1], 2, "late joiner learns it too");
  h.send(vis(true)); assert.equal((await a.c.take(0x8f))[1], 0);
  // a is hidden and silent, b is visible and silent; the host keeps sending so the relay sweeps
  a.c.send(vis(false)); await sleep(100);
  for (let i = 0; i < 12; i++) { h.send(new Uint8Array([7, 0, 0, 0, 0, 0, 0])); await sleep(250); }
  await h.none(0x86, 100);
  const rb = await resume3(code, 2, b.tok, 0); // b was silent while visible: in limbo, resumable (its old socket was closed by the relay)
  assert.equal(parseWelcome(await rb.take(0x81)).ext.flags & 1, 1);
  assert.ok(!a.c.closed, "hidden a stays connected");
  [h, a.c, rb, late.c].forEach((x) => x.close());
});

await test("p4: resume with an unknown seq (newer than the relay has) or a nonsense token is refused", async () => {
  const { h, code } = await host3();
  const a = await join3(code); await h.take(0x85);
  a.c.ws.terminate(); await sleep(200);
  const bad = await resume3(code, 1, a.tok, 999999); assert.equal((await bad.take(0x87))[1], 10);
  const junk = new Client(`/ws/room/${code}?v=3&rejoin=zzz`); await junk.opened; assert.equal((await junk.take(0x87))[1], 10);
  const ok = await resume3(code, 1, a.tok, 0); assert.equal(parseWelcome(await ok.take(0x81)).ext.flags & 1, 1);
  [h, ok].forEach((x) => x.close());
});

await test("p4: a reloaded tab frees its old limbo slot with stale=ID.TOKEN", async () => {
  const { h, code } = await host3("&max=2");
  const a = await join3(code); await h.take(0x85);
  a.c.ws.terminate(); await sleep(300);
  const blocked = new Client(`/ws/room/${code}?v=3`); await blocked.opened; assert.equal((await blocked.take(0x87))[1], 3);
  const n = await join3(code, `&stale=1.${a.tok}`); assert.ok(n.w.id > 1);
  [h, n.c].forEach((x) => x.close());
});

// ---- free plan budget (docs/MULTIPLAYER.md "Free plan budget") ----------------------------------------------------------------------
await test("budget: a batch with several game frames: each recipient gets the clock reading in front of the messages that followed it", async () => {
  const { h, code } = await host();
  const a = await join(code); await a.take(0x8a); await h.take(0x85);
  const b = await join(code); await b.take(0x8a); await h.take(0x85);
  a.frames.length = 0; b.frames.length = 0; h.frames.length = 0;
  const V = SCENE(21);
  h.send(stamp(1000), state(1, V, 14), rpc(1, V, "f1"), stamp(1066), state(1, V, 14), stamp(1133), rpc(1, V, "f3"));
  await a.take(0x82); await sleep(200);
  for (const c of [a, b]) {
    assert.equal(c.frames.length, 1, "still one WebSocket frame per recipient");
    const f = c.frames[0];
    assert.deepEqual(f.map((m) => m[0]), [0x8b, 0x8d, 0x82, 0x8b, 0x8d, 0x8b, 0x82]);
    assert.deepEqual(f.filter((m) => m[0] === 0x8b).map((m) => dv(m).getUint32(5, true)), [1000, 1066, 1133]);
  }
  // a client batch whose first game frame only had a request for the host: the other client gets only the clock reading that applies to it
  h.frames.length = 0; b.frames.length = 0;
  a.send(stamp(50), rpc(0, V, "toHost"), stamp(116), rpc(1, V, "toAll"));
  await b.take(0x82); await sleep(200);
  assert.deepEqual(h.frames[0].map((m) => m[0]), [0x8b, 0x82, 0x8b, 0x82]);
  assert.deepEqual(b.frames[0].map((m) => m[0]), [0x8b, 0x82]); assert.equal(dv(b.frames[0][0]).getUint32(5, true), 116); assert.equal(dv(b.frames[0][0]).getInt32(1, true), 1);
  [h, a, b].forEach((x) => x.close());
});

await test("budget: a frame with 1500 messages (300 zombies at 20 Hz flushed at 4 Hz) is routed completely", async () => {
  const { h, code } = await host();
  const a = await join(code); await a.take(0x8a); await h.take(0x85);
  a.frames.length = 0; a.msgs.length = 0;
  const bodies = [stamp(1)]; for (let i = 0; i < 1500; i++) bodies.push(state(1, SCENE(1000 + i), 14));
  h.send(...bodies);
  await sleep(700);
  assert.equal(a.count(0x8d), 1500);
  [h, a].forEach((x) => x.close());
});

await test("budget: keepalive 'zbka' is answered by the runtime and keeps a visible but otherwise silent socket out of limbo", async () => {
  const { h, code } = await host3();
  const a = await join3(code); await h.take(0x85); await a.c.take(0x8a);
  a.c.ws.send("zbka");
  await until(() => (a.c.raw || []).includes("zbka"), 2000);
  assert.ok((a.c.raw || []).includes("zbka"), "auto response");
  // tstale is 2.5 s: a sends only keepalives for 5 s while the host keeps the relay busy (so it sweeps)
  for (let i = 0; i < 20; i++) { a.c.ws.send("zbka"); h.send(new Uint8Array([7, 0, 0, 0, 0, 0, 0])); await sleep(250); }
  await h.none(0x86, 100);
  assert.ok(!a.c.closed, "a kept its socket");
  [h, a.c].forEach((x) => x.close());
});

await test("budget: when the Durable Object quota is used up the Worker answers JSON 503 'quota' and sockets get REJECT 11", async () => {
  const qh = { headers: { cookie: "zbquota=1" } };
  for (const p of ["/api/rooms", "/api/quickplay?type=x", "/api/room/ABCDE", "/api/ping/ABCDE"]) {
    const r = await fetch(`http://${BASE}${p}`, qh); assert.equal(r.status, 503, p);
    const j = await r.json(); assert.equal(j.error, "quota"); assert.ok(j.resetAt > Date.now() && j.resetAt % 86400000 === 0, "next 00:00 UTC");
  }
  const s1 = new Client(`/ws/host?v=3&name=Q`, { cookie: "zbquota=1" }); await s1.opened; assert.equal((await s1.take(0x87))[1], 11);
  const s2 = new Client(`/ws/room/ABCDE?v=3`, { cookie: "zbquota=1" }); await s2.opened; assert.equal((await s2.take(0x87))[1], 11);
  assert.equal((await fetch(`http://${BASE}/api/leaderboard`, qh)).status, 200, "the leaderboard (D1) does not need a Durable Object");
});

await test("budget: the room list is cached at the edge for a few seconds (x-zb-cache: 1 turns the cache on in local tests)", async () => {
  const h1 = { headers: { "x-zb-cache": "1" } };
  await sleep(5200);
  const r1 = await fetch(`http://${BASE}/api/rooms`, h1); await r1.json();
  const r2 = await fetch(`http://${BASE}/api/rooms`, h1); await r2.json();
  const r3 = await fetch(`http://${BASE}/api/quickplay?type=x`, h1); await r3.json();
  assert.equal(r1.headers.get("x-zb-cached"), "0"); assert.equal(r2.headers.get("x-zb-cached"), "1"); assert.equal(r3.headers.get("x-zb-cached"), "1", "Quick Play reads the same cached rows");
});

console.log(`${ok} tests passed`);
process.exit(process.exitCode || 0);
