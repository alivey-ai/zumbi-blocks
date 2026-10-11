// Free plan budget measurement (docs/MULTIPLAYER.md "Free plan budget"): host A + client B play with N zombies alive and this prints, per
// socket, the WebSocket frames per second that reach the Durable Object (what the Workers Free plan counts), split by what the frame carries:
//   state  = a frame with at least one C_STATE (positions)
//   rpc    = no state, but an RPC / INST / DESTROY
//   ctrl   = only control messages (TIME, PING, ECHO, META, SCORE, LISTING, VIS)
//   text   = JSON text frames (WebRTC signaling, dc flag, telemetry, keepalive)
// plus messages per second by opcode. Run once with the default page (WebRTC lane on) and once with BASE=...?p2p=0.
// Env: TARGETS="50,150,300", WINDOW=10000 (ms), BASE.
import { launch, game, createRoom, joinByCode, heal } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8799/";
const TARGETS = (process.env.TARGETS || "50,150,300").split(",").map(Number);
const WINDOW = Number(process.env.WINDOW || 10000);
const OPN = { 1: "rpc", 2: "inst", 3: "destroy", 4: "removerpcs", 5: "destroyplayer", 7: "ping", 8: "listing", 9: "time", 10: "echo", 11: "state", 16: "meta", 17: "vis", 20: "score" };
function track(p) {
  p.bud = null;
  p.on("websocket", (ws) => ws.on("framesent", (f) => {
    const s = p.bud; if (!s) return;
    if (typeof f.payload === "string") { s.text++; const k = "txt:" + (/"t":"(\w+)"/.exec(f.payload) || [])[1]; s.ops[k] = (s.ops[k] || 0) + 1; return; }
    const b = Buffer.from(f.payload); let o = 0, st = false, rpc = false;
    while (o + 2 <= b.length) { const len = b[o] | (b[o + 1] << 8); o += 2; const op = b[o]; const k = OPN[op] || "op" + op; s.ops[k] = (s.ops[k] || 0) + 1; if (op === 1 && (b[o + 1] === 5 || b[o + 1] === 6)) s.ops.rpcBuffered = (s.ops.rpcBuffered || 0) + 1; if (op === 1 && process.env.NAMES) { const nm = "rpc:" + b.subarray(o + 11, o + 11 + b[o + 10]).toString() + "/" + b[o + 1]; s.ops[nm] = (s.ops[nm] || 0) + 1; } if (op === 11) st = true; else if (op >= 1 && op <= 5) rpc = true; o += len; }
    if (st) s.state++; else if (rpc) s.rpc++; else s.ctrl++;
  }));
}
const fresh = () => ({ state: 0, rpc: 0, ctrl: 0, text: 0, ops: {} });
const ba = await launch(), bb = await launch();
const wrap = (b) => { const orig = b.newContext.bind(b); b.newContext = async (...a) => { const c = await orig(...a); const np = c.newPage.bind(c); c.newPage = async () => { const p = await np(); track(p); return p; }; return c; }; };
wrap(ba); wrap(bb);
const A = await game(ba, "A", BASE);
const code = await createRoom(A, BASE, "Budget");
const B = await game(bb, "B", BASE);
await joinByCode(B, code);
const send = (p, m, a = "") => p.evaluate(([m, a]) => window.unityInstance.SendMessage("ZbNetRunner", m, a), [m, a]);
await A.click_(640, 360); await A.keyboard.press("Enter"); await B.click_(640, 360); await A.waitForTimeout(8000);
await send(A, "DebugZmax", "2000"); await send(A, "DebugZGuard", "0");
const hb = setInterval(() => { heal(A).catch(() => {}); heal(B).catch(() => {}); }, 500);
const rows = [];
for (const target of TARGETS) {
  for (let i = 0; i < 40 && (await A.dump()).zombies < target; i++) { await send(A, "DebugWaveClock", "119.5"); await A.waitForTimeout(1500); }
  await A.waitForTimeout(3000);
  A.bud = fresh(); B.bud = fresh(); const t0 = Date.now();
  await A.waitForTimeout(WINDOW);
  const dt = (Date.now() - t0) / 1000, a = A.bud, b = B.bud; A.bud = B.bud = null;
  const d = await A.dump(), rtcB = await B.rtc();
  const per = (s) => ({ frames: +((s.state + s.rpc + s.ctrl + s.text) / dt).toFixed(1), state: +(s.state / dt).toFixed(1), rpc: +(s.rpc / dt).toFixed(1), ctrl: +(s.ctrl / dt).toFixed(1), text: +(s.text / dt).toFixed(2),
    ops: Object.fromEntries(Object.entries(s.ops).map(([k, v]) => [k, +(v / dt).toFixed(1)])) });
  const r = { zombies: d.zombies, lane: rtcB && rtcB.peers && rtcB.peers[0] ? rtcB.peers[0].state : "off", host: per(a), client: per(b) };
  r.totalPerS = +(r.host.frames + r.client.frames).toFixed(1);
  rows.push(r); console.log("BUDGET " + JSON.stringify(r));
}
clearInterval(hb);
console.table(rows.map((r) => ({ zombies: r.zombies, lane: r.lane, hostFrames: r.host.frames, hostState: r.host.state, hostRpc: r.host.rpc, hostCtrl: r.host.ctrl, clientFrames: r.client.frames, clientState: r.client.state, clientRpc: r.client.rpc, clientCtrl: r.client.ctrl, total: r.totalPerS })));
await Promise.race([Promise.all([ba.close(), bb.close()]), new Promise((r) => setTimeout(r, 3000))]);
process.exit(0);
