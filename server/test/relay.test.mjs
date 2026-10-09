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
  constructor(path) {
    this.msgs = []; this.waiters = []; this.closed = false;
    this.ws = new WebSocket(`ws://${BASE}${path}`);
    this.ws.binaryType = "arraybuffer";
    this.ws.on("message", (d) => {
      const f = new Uint8Array(d);
      let o = 0;
      while (o + 2 <= f.length) { const len = f[o] | (f[o + 1] << 8); o += 2; this.msgs.push(f.slice(o, o + len)); o += len; }
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
  count(op) { return this.msgs.filter((m) => m[0] === op).length; }
  close() { this.ws.close(); }
}
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const dv = (m) => new DataView(m.buffer, m.byteOffset, m.byteLength);

async function host(extra = "") {
  const h = new Client(`/ws/host?v=1&max=3&name=Test+Room${extra}`); await h.opened;
  const w = await h.take(0x81);
  const code = new TextDecoder().decode(w.subarray(5 + 3, 5 + 3 + w[7]));
  return { h, code, id: dv(w).getInt32(1, true), isHost: w[5] };
}
async function join(code, extra = "") {
  const c = new Client(`/ws/room/${code}?v=1${extra}`); await c.opened; return c;
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

console.log(`${ok} tests passed`);
process.exit(process.exitCode || 0);
