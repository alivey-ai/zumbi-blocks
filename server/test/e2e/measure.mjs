// Network measurement: host A + client B (+ optional C) play ~25 s; prints WebSocket traffic stats per direction
// (bytes/s, frames/s, messages/s, messages per frame, per-RPC breakdown, state update arrival intervals).
// Works against old and new builds (decodes both RPC and compact state messages).
//   BASE=http://localhost:8787/?netsim=120,30 CH=chrome node measure.mjs
import { launch, game } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8787/";
const WINDOW = Number(process.env.WINDOW || 25000);
const b = await launch();

function newStats() { return { frames: 0, bytes: 0, msgs: 0, byName: {}, bytesByName: {}, sizes: [], perFrame: [], arrivals: {}, t0: Date.now() }; }
function track(page) {
  page.net = { sent: newStats(), recv: newStats() };
  page.on("websocket", (ws) => {
    const handle = (dir) => (f) => {
      const st = page.net[dir]; const now = Date.now(); const buf = Buffer.from(f.payload);
      st.frames++; st.bytes += buf.length + 2; // + ~2 bytes WS header
      let o = 0, n = 0;
      while (o + 2 <= buf.length) {
        const len = buf[o] | (buf[o + 1] << 8); o += 2; if (o + len > buf.length) break;
        const op = buf[o]; let name = "op" + op.toString(16);
        let view = null;
        if (dir === "sent" && op === 1) { const nl = buf[o + 10]; name = "rpc:" + buf.subarray(o + 11, o + 11 + nl).toString(); }
        else if (dir === "recv" && op === 0x82) { const nl = buf[o + 14]; name = "rpc:" + buf.subarray(o + 15, o + 15 + nl).toString(); view = buf.readBigUInt64LE(o + 6).toString(); }
        else if (dir === "sent" && op === 11) { name = "state" + buf[o + 1]; }
        else if (dir === "recv" && op === 0x8d) { name = "state" + buf[o + 1]; view = buf.readBigUInt64LE(o + 2).toString(); }
        else { const known = { 2: "inst", 3: "destroy", 7: "ping", 9: "time", 10: "echo", 0x83: "inst", 0x84: "destroy", 0x88: "pong", 0x89: "pinginfo", 0x8b: "time", 0x8c: "echo" }; if (known[op]) name = known[op]; }
        st.msgs++; n++; st.byName[name] = (st.byName[name] || 0) + 1; st.bytesByName[name] = (st.bytesByName[name] || 0) + len + 2;
        if (view && (name === "rpc:setzumbistuff" || name === "rpc:setposstuff" || name === "state1" || name === "state2")) { (st.arrivals[view] = st.arrivals[view] || []).push(now); }
        o += len;
      }
      st.perFrame.push(n);
    };
    ws.on("framesent", handle("sent")); ws.on("framereceived", handle("recv"));
  });
}
const orig = b.newContext.bind(b);
const pages = [];
const mk = async (name) => { const p = await game(b, name, BASE); return p; };
// track must attach before navigation: wrap newContext
b.newContext = async (...a) => { const c = await orig(...a); const np = c.newPage.bind(c); c.newPage = async () => { const p = await np(); track(p); pages.push(p); return p; }; return c; };

const A = await mk("A");
await A.click_(750, 281); await A.click_(750, 331);
await A.click_(850, 185); await A.keyboard.type("Measure", { delay: 30 });
await A.click_(750, 331); await A.click_(750, 481); await A.waitForTimeout(5000);
const rooms = await (await fetch(new URL("/api/rooms", BASE))).json();
const B = await mk("B");
await B.click_(750, 281); await B.click_(750, 381); await B.waitForTimeout(1500);
await B.click_(400, 45); await B.click_(750, 561); await B.waitForTimeout(5000);
console.log("B", JSON.stringify(await B.dump()));
await A.click_(640, 360); await A.keyboard.press("Enter"); await A.waitForTimeout(12000);
// zombies eat idle players within seconds and a dead player stops moving: keep both alive for the whole measurement
for (const p of [A, B]) await p.evaluate(() => setInterval(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugHeal", ""), 500));
console.log("started match; zombies A/B", (await A.dump()).zombies, (await B.dump()).zombies);

for (const p of [A, B]) { p.net.sent = newStats(); p.net.recv = newStats(); }
const w0 = { A: { ...A.wsFrames }, B: { ...B.wsFrames } };
console.log('reset stats');
const s0 = { A: await A.statsDump(), B: await B.statsDump() };
console.log('s0 done'); await A.sampleStart(); await B.sampleStart(); console.log('sampling');
const t0 = Date.now();
await B.click_(640, 360);
const walk = async (p, k1, k2) => { while (Date.now() - t0 < WINDOW) { await p.keyboard.down(k1); await p.waitForTimeout(1500); await p.keyboard.up(k1); await p.keyboard.down(k2); await p.waitForTimeout(1500); await p.keyboard.up(k2); } };
const shoot = async () => { while (Date.now() - t0 < WINDOW) { await B.mouse.down(); await B.waitForTimeout(400); await B.mouse.up(); await B.waitForTimeout(1600); } };
await Promise.all([walk(A, "w", "s"), walk(B, "a", "d"), shoot()]);
const secs = (Date.now() - t0) / 1000;
const w1 = { A: { ...A.wsFrames }, B: { ...B.wsFrames } };
console.log('window done');
const s1 = { A: await A.statsDump(), B: await B.statsDump() };
console.log('s1 done');
const smooth = { A: await A.sampleStop(), B: await B.sampleStop() };

const pct = (a, q) => { if (!a.length) return 0; const s = [...a].sort((x, y) => x - y); return s[Math.min(s.length - 1, Math.floor(q * s.length))]; };
const out = {};
for (const p of [A, B]) for (const dir of ["sent", "recv"]) {
  const st = p.net[dir]; const iv = [];
  for (const v of Object.values(st.arrivals)) for (let i = 1; i < v.length; i++) iv.push(v[i] - v[i - 1]);
  const mean = iv.length ? iv.reduce((a, c) => a + c, 0) / iv.length : 0;
  const sd = iv.length ? Math.sqrt(iv.reduce((a, c) => a + (c - mean) ** 2, 0) / iv.length) : 0;
  const top = Object.entries(st.bytesByName).sort((a, c) => c[1] - a[1]).slice(0, 6).map(([k, v]) => `${k}:${st.byName[k]}msgs/${v}B`);
  out[`${p.name}.${dir}`] = {
    kBps: +(st.bytes / 1024 / secs).toFixed(1), framesPerS: +(st.frames / secs).toFixed(1), msgsPerS: +(st.msgs / secs).toFixed(1),
    msgsPerFrameAvg: +(st.msgs / Math.max(1, st.frames)).toFixed(2), avgMsgBytes: +(st.bytes / Math.max(1, st.msgs)).toFixed(1),
    stateIntervalMs: { n: iv.length, mean: +mean.toFixed(1), sd: +sd.toFixed(1), p50: pct(iv, 0.5), p95: pct(iv, 0.95) }, top,
  };
}
// Cost metric: every WebSocket frame a page sends is one message the Durable Object receives (billed as requests at 20:1 on the paid plan,
// counted against the free quota); frames the DO sends to pages are free. Text frames are the WebRTC signaling and lane flags.
{
  const d = (k, f) => w1[k][f] - w0[k][f];
  const doIn = d("A", "sent") + d("B", "sent"), doOut = d("A", "recv") + d("B", "recv");
  console.log("DO-MSGS", JSON.stringify({ doReceivesPerS: +(doIn / secs).toFixed(1), fromHostPerS: +(d("A", "sent") / secs).toFixed(1), fromClientPerS: +(d("B", "sent") / secs).toFixed(1), doSendsPerS: +(doOut / secs).toFixed(1), textFrames: d("A", "sentText") + d("B", "sentText") }));
}
console.log("window", secs.toFixed(1), "s", "BASE", BASE);
console.log("TRAFFIC", JSON.stringify(out));
const ov = (k) => { const a = s0[k], z = s1[k]; if (!a || !z) return null; const dt = (z.t - a.t) / 1000; return { fps: z.fps, rxKBps: +((z.rx.bytes - a.rx.bytes) / 1024 / dt).toFixed(1), rxMsgsPerS: +((z.rx.msgs - a.rx.msgs) / dt).toFixed(1), rxFramesPerS: +((z.rx.frames - a.rx.frames) / dt).toFixed(1), txKBps: +((z.tx.bytes - a.tx.bytes) / 1024 / dt).toFixed(1), txMsgsPerS: +((z.tx.msgs - a.tx.msgs) / dt).toFixed(1), rtt: z.rtt, clockJitter: z.clockJitter, relayPing: z.relayPing, interp: z.interp, backlog: z.backlog, cfg: z.cfg, p2p: z.p2p && { rxMsgs: z.p2p.rxMsgs - (a.p2p ? a.p2p.rxMsgs : 0), txMsgs: z.p2p.txMsgs - (a.p2p ? a.p2p.txMsgs : 0), links: z.p2p.links, flips: z.p2p.flips } }; };
console.log("OVERLAY-STATS", JSON.stringify({ A: ov("A"), B: ov("B") }, null, 1));

// Trajectory fidelity: what a remote peer rendered vs what the owner actually did (same wall clock). For every entity that
// moved, find the constant delay tau that best explains the rendered path and report the residual RMS error (metres).
function interpAt(tr, t) { const T = tr.t0, p = tr.p; let lo = 0, hi = p.length / 3 - 1; if (t < T + p[0] || t > T + p[hi * 3]) return null; while (hi - lo > 1) { const m = (lo + hi) >> 1; if (T + p[m * 3] <= t) lo = m; else hi = m; } const a = T + p[lo * 3], b = T + p[hi * 3], f = b > a ? (t - a) / (b - a) : 0; return [p[lo * 3 + 1] + (p[hi * 3 + 1] - p[lo * 3 + 1]) * f, p[lo * 3 + 2] + (p[hi * 3 + 2] - p[lo * 3 + 2]) * f]; }
function compare(owner, observer) {
  const res = [];
  for (const o of observer.filter((t) => !t.mine)) {
    const tr = owner.find((t) => t.mine && t.id === o.id); if (!tr) continue;
    let len = 0; for (let i = 1; i < tr.p.length / 3; i++) len += Math.hypot(tr.p[i * 3 + 1] - tr.p[(i - 1) * 3 + 1], tr.p[i * 3 + 2] - tr.p[(i - 1) * 3 + 2]);
    if (len < 8) continue;
    let best = { rms: 1e9, tau: 0 };
    for (let tau = 0; tau <= 600; tau += 5) {
      let sum = 0, n = 0;
      for (let i = 0; i < o.p.length / 3; i++) { const t = o.t0 + o.p[i * 3]; const q = interpAt(tr, t - tau); if (!q) continue; sum += (o.p[i * 3 + 1] - q[0]) ** 2 + (o.p[i * 3 + 2] - q[1]) ** 2; n++; }
      if (n > 20 && Math.sqrt(sum / n) < best.rms) best = { rms: Math.sqrt(sum / n), tau };
    }
    if (best.rms < 1e8) res.push({ id: o.id, pathM: +len.toFixed(0), rmsM: +best.rms.toFixed(3), tauMs: best.tau });
  }
  const avg = (k) => res.length ? +(res.reduce((a, c) => a + c[k], 0) / res.length).toFixed(3) : null;
  const med = (k) => { const v = res.map((r) => r[k]).sort((x, y) => x - y); return v.length ? v[v.length >> 1] : null; };
  return { entities: res.length, medianRmsM: med("rmsM"), medianTauMs: med("tauMs"), meanRmsM: avg("rmsM"), meanTauMs: avg("tauMs"), all: res.map((r) => `${r.rmsM}@${r.tauMs}`).join(" ") };
}
const zs = (a) => a.filter((t) => t.z), ps = (a) => a.filter((t) => !t.z);
console.log("TRAJECTORY zombies (host -> client B)", JSON.stringify(compare(zs(smooth.A), zs(smooth.B))));
console.log("TRAJECTORY player B (client B -> host)", JSON.stringify(compare(ps(smooth.B), ps(smooth.A))));
console.log("TRAJECTORY player A (host -> client B)", JSON.stringify(compare(ps(smooth.A), ps(smooth.B))));
await Promise.race([b.close(), new Promise((r) => setTimeout(r, 3000))]);
process.exit(0);
