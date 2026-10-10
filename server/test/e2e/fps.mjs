// Performance + bandwidth check with many zombies alive: host A and client B (each its own headless Chrome with GPU, `CH=chrome`). Used to pick the
// zombie crash-guard ceiling of Timed waves (ZbMenu.ZOMBIE_CEILING, docs/MULTIPLAYER.md). The ceiling is lifted with DebugZmax, the frame rate guard
// is switched off with DebugZGuard("0"), waves are advanced with DebugWaveClock. For every target count it prints the fps of host and client and
// the traffic over a 6 s window: bytes/s and messages/s the host sends (ZBSTATS tx: WebSocket to the relay; dc: WebRTC lane), what the client
// receives, and WebSocket frames/s sent to the relay (= Durable Object messages, the free-plan quantity).
// Env: TARGETS="50,100,150,200,300" (default), SOLO=1 (host only), BASE (add ?p2p=0 to force everything through the relay).
import { launch, game, createRoom, joinByCode, heal } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8787/";
const TARGETS = (process.env.TARGETS || "50,100,150,200,300").split(",").map(Number);
const ba = await launch(), bb = process.env.SOLO ? null : await launch();
const A = await game(ba, "A", BASE);
const code = await createRoom(A, BASE, "Fps1");
const B = bb ? await game(bb, "B", BASE) : null;
if (B) await joinByCode(B, code);
const send = (p, m, a = "") => p.evaluate(([m, a]) => window.unityInstance.SendMessage("ZbNetRunner", m, a), [m, a]);
await A.click_(640, 360); await A.keyboard.press("Enter"); if (B) await B.click_(640, 360); await A.waitForTimeout(6000);
await send(A, "DebugZmax", "2000"); await send(A, "DebugZGuard", "0");
const hb = setInterval(() => { heal(A).catch(() => {}); if (B) heal(B).catch(() => {}); }, 500);
const avg = (xs) => Math.round(xs.reduce((a, b) => a + b, 0) / Math.max(1, xs.length));
const rows = [];
for (const target of TARGETS) {
  for (let i = 0; i < 40 && (await A.dump()).zombies < target; i++) { await send(A, "DebugWaveClock", "119.5"); await A.waitForTimeout(1500); }
  await A.waitForTimeout(3000);
  const a0 = await A.statsDump(), b0 = B ? await B.statsDump() : null, f0 = { a: A.wsFrames.sent, b: B ? B.wsFrames.sent : 0, ar: A.wsFrames.recv }, t0 = Date.now();
  const fa = [], fb = [];
  for (let i = 0; i < 6; i++) { await A.waitForTimeout(1000); fa.push((await A.statsDump()).fps); if (B) fb.push((await B.statsDump()).fps); }
  const a1 = await A.statsDump(), b1 = B ? await B.statsDump() : null, dt = (Date.now() - t0) / 1000;
  const d = await A.dump(), db = B ? await B.dump() : null;
  const r = {
    alive: d.zombies, clientSees: db ? db.zombies : "-", hostFps: avg(fa), hostFpsMin: Math.round(Math.min(...fa)), clientFps: B ? avg(fb) : "-", clientFpsMin: B ? Math.round(Math.min(...fb)) : "-",
    hostWsTxKBs: +((a1.tx.bytes - a0.tx.bytes) / dt / 1024).toFixed(1), hostWsTxMsgs: Math.round((a1.tx.msgs - a0.tx.msgs) / dt),
    hostDcTxKBs: +((a1.p2p.txBytes - a0.p2p.txBytes) / dt / 1024).toFixed(1),
    clientRxKBs: b1 ? +(((b1.rx.bytes - b0.rx.bytes) + (b1.p2p.rxBytes - b0.p2p.rxBytes)) / dt / 1024).toFixed(1) : "-",
    doMsgsPerS: Math.round(((A.wsFrames.sent - f0.a) + (B ? B.wsFrames.sent - f0.b : 0)) / dt), hostWsRecvFramesPerS: Math.round((A.wsFrames.recv - f0.ar) / dt),
  };
  rows.push(r); console.log("FPS " + JSON.stringify(r));
}
clearInterval(hb);
console.table(rows);
await Promise.race([Promise.all([ba.close(), bb && bb.close()]), new Promise((r) => setTimeout(r, 3000))]);
process.exit(0);
