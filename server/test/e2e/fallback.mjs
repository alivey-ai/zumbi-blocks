// Fallback test for the WebRTC lane: host A + clients B, C, D play while the lane is sabotaged in three ways.
//   B: normal page, then window.zbrtc.stall(true) (the channel swallows everything, like a dead path; no close event), later released
//   C: ?p2p=drop  (the page closes its own channel 12 s after it opened; it should reconnect)
//   D: ?p2p=block (ICE can never connect; this client must simply stay on the WebSocket relay)
// Checks that state keeps arriving over the WebSocket (no long gap, no teleports, same zombies as the host) and that the lane recovers.
//   CH=chrome BASE=http://localhost:8787/ node fallback.mjs
import { launch, game } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8787/";
const withQ = (u, q) => u + (u.includes("?") ? "&" : "?") + q;
const b = await launch();
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };
const J = (x) => JSON.stringify(x);

const A = await game(b, "A", BASE);
await A.click_(750, 281); await A.click_(750, 331);
await A.click_(850, 185); await A.keyboard.type("Fallback", { delay: 30 });
await A.click_(750, 331); await A.click_(750, 481); await A.waitForTimeout(4000);
const code = (await (await fetch(new URL("/api/rooms", BASE))).json())[0].code;
const joinCode = async (name, q) => {
  const P = await game(b, name, q ? withQ(BASE, q) : BASE);
  await P.click_(750, 281); await P.click_(750, 431); await P.waitForTimeout(800);
  await P.click_(850, 265); await P.keyboard.press("Control+a"); await P.keyboard.type(code, { delay: 30 });
  await P.click_(750, 431); await P.waitForTimeout(3000);
  return P;
};
const B = await joinCode("B"), C = await joinCode("C", "p2p=drop"), D = await joinCode("D", "p2p=block");
await A.click_(640, 360); await A.keyboard.press("Enter"); await A.waitForTimeout(4000);
for (const p of [A, B, C, D]) await p.evaluate(() => setInterval(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugHeal", ""), 500));

// sample every client's snapshot counter + link state until `ms` have passed; returns per page {gap: longest time without new snapshots, teleports, ups}
const watch = async (ms, pages = [B, C, D]) => {
  const t0 = Date.now(); const res = pages.map((p) => ({ p, last: null, lastT: t0, gap: 0, series: [], pts: [] }));
  while (Date.now() - t0 < ms) {
    for (const r of res) {
      const s = await r.p.statsDump(); if (!s) continue;
      const t = Date.now();
      if (r.last === null || s.interp.snaps > r.last) { r.gap = Math.max(r.gap, t - r.lastT); r.lastT = t; r.last = s.interp.snaps; }
      r.series.push(`${Math.round((t - t0) / 1000)}:${s.p2p.links["0"] ? s.p2p.links["0"].up : "-"}`);
      r.pts.push([t, s.interp.snaps]);
      r.tele = s.interp.teleports; r.starved = s.interp.starved; r.flips = s.p2p.flips;
    }
  }
  for (const r of res) {
    r.gap = Math.max(r.gap, Date.now() - r.lastT);
    // The host's zombies/avatar are most of the snapshots, but other clients' avatars keep arriving over the relay, so "no snapshot at all" never
    // happens. Starvation = the longest stretch where the snapshot rate fell below 30 % of this window's median rate.
    const rates = []; for (let i = 1; i < r.pts.length; i++) rates.push((r.pts[i][1] - r.pts[i - 1][1]) / Math.max(1, r.pts[i][0] - r.pts[i - 1][0]));
    const med = [...rates].sort((a, b) => a - b)[rates.length >> 1] || 0;
    let cur = 0, worst = 0; for (let i = 1; i < r.pts.length; i++) { if (rates[i - 1] < 0.3 * med) { cur += r.pts[i][0] - r.pts[i - 1][0]; worst = Math.max(worst, cur); } else cur = 0; }
    r.starveMs = worst; r.medRate = Math.round(med * 1000);
  }
  return res;
};
const lane = async (P) => { const i = await P.rtc(); return i && i.peers["0"] ? i.peers["0"].state : "none"; };
const sameZ = async (P) => { const a = (await A.dump()).zpos, x = (await P.dump()).zpos; return [a.length, a.filter((p) => x.includes(p)).length]; };

// ---- phase 1: everyone settles. B and C get a channel, D (blocked) stays on the relay ----
const w1 = await watch(9000);
const tele0 = Object.fromEntries(w1.map((r) => [r.p.name, r.tele]));
check("B: channel up", (await lane(B)) === "up", await lane(B));
check("C: channel up (before its scheduled drop)", (await lane(C)) === "up", await lane(C));
check("D (?p2p=block): no channel, still receiving state over the WebSocket", (await lane(D)) !== "up" && w1[2].starveMs < 1000, J([await lane(D), w1[2].starveMs]));
{ const [n, same] = await sameZ(D); check("D sees the host's zombies (relay only)", n > 0 && same >= n * 0.6, J([n, same])); }
check("blocked client D: host kept sending state over the WebSocket for it (mixed room)", (A.stats.sent[11] || 0) > 100, J(A.stats.sent[11]));

// ---- phase 2: C drops its own channel (~12 s after it opened) and B's path dies silently ----
console.log("-- stalling B's channel");
await B.evaluate(() => window.zbrtc.stall(true));
const w2 = await watch(8000);
const rB = w2[0], rC = w2[1];
console.log("state starvation (ms below 30% of the normal snapshot rate): B", rB.starveMs, "C", rC.starveMs, "D", w2[2].starveMs, "| median snapshots/s B", rB.medRate, "| teleports B", rB.tele - tele0.B);
console.log("B link up flag over time", rB.series.join(" "));
console.log("C link up flag over time", rC.series.join(" "));
check("B: lane goes down on the silence watchdog; state is starved for no longer than the watchdog plus a bit (< 2.5 s), then flows over the relay", (await lane(B)) !== "up" && rB.starveMs < 2500 && rB.starveMs > 0, J([await lane(B), rB.starveMs]));
check("B: no teleports or starvation spikes from the switch", rB.tele - tele0.B <= 3, J([rB.tele, tele0.B, rB.starved]));
{ const [n, same] = await sameZ(B); check("B sees the same zombies as the host on the relay path", n > 0 && same >= n * 0.6, J([n, same])); }
check("C: dropped and reconnected (lane flipped, up again) with no long gap", rC.flips >= 2 && rC.starveMs < 1000, J([rC.flips, rC.starveMs, await lane(C)]));
console.log("-- releasing B's channel");
await B.evaluate(() => window.zbrtc.stall(false));
const w3 = await watch(7000, [B, C]);
check("B: lane recovers after 3 good probes (hysteresis) and is up again", (await lane(B)) === "up", await lane(B));
check("C: channel up again after its drop", (await lane(C)) === "up", await lane(C));
check("recovery causes no long gap", w3[0].starveMs < 1000 && w3[1].starveMs < 1000, J([w3[0].starveMs, w3[1].starveMs]));
{ const [n, same] = await sameZ(B); check("B agrees with the host again after recovery", n > 0 && same >= n * 0.6, J([n, same])); }
const iB = await B.rtc(), iD = await D.rtc();
console.log("B", J(iB)); console.log("D", J(iD));
check("D never connected: it gave up or is still retrying, never up", !iD.everUp, J(iD));
console.log("logs B:", B.logs.filter((l) => /zbrtc/.test(l)).join(" | ").slice(0, 900));
console.log("logs C:", C.logs.filter((l) => /zbrtc/.test(l)).join(" | ").slice(0, 900));
console.log("logs D:", D.logs.filter((l) => /zbrtc/.test(l)).join(" | ").slice(0, 600));
console.log(`${pass} passed, ${fail} failed`);
await Promise.race([b.close(), new Promise((r) => setTimeout(r, 3000))]);
process.exit(fail ? 1 : 0);
