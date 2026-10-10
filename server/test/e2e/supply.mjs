// Radio / airdrop test: one S.O.S call must drop exactly ONE supply crate that lands as exactly ONE bag.
// Regression test for "Network.Destroy(viewID) did nothing offline -> sc_suplly respawned a bag every frame".
// Uses the DebugSupply hook (the radio's asksuplly path) and the supply/crates counters of DebugDump.
//   CH=chrome BASE=http://localhost:8787/ node supply.mjs
import { launch, game } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8787/";
const b = await launch();
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };
// (development builds append a stack trace to console lines: parse the first line only)
const dumpOf = async (P) => { const n = P.logs.length; await P.evaluate(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugDump", "")); for (let i = 0; i < 40; i++) { await P.waitForTimeout(100); const l = P.logs.slice(n).find((x) => x.startsWith("ZBDUMP ")); if (l) return JSON.parse(l.slice(7).split("\n")[0]); } return null; };
const heal = (P) => P.evaluate(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugHeal", ""));
const call = (P) => P.evaluate(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugSupply", ""));

// Samples for up to 45 s: the crate falls ~4 s of game time (slower in a low fps headless tab), lands as a bag, and is destroyed.
// Keeps sampling 6 s after the first bag appears so a crate that is not destroyed has time to respawn bags.
async function sample(read, base, P0) {
  let peak = 0, peakBags = 0, landed = -1, last = null;
  for (let t = 0; t < 45; t++) {
    await new Promise((r) => setTimeout(r, 1000));
    await heal(P0); last = await read(); if (!last) continue;
    peak = Math.max(peak, last.supply); peakBags = Math.max(peakBags, last.bags); if (process.env.VERBOSE) console.log("  t=" + t, JSON.stringify({ supply: last.supply, bags: last.bags, hp: last.hp, zombies: last.zombies }));
    if (landed < 0 && last.bags > base.bags) landed = t;
    if (landed >= 0 && t >= landed + 6) break;
  }
  return { peak, peakBags, last };
}
async function drop(P, label) {
  const d0 = await dumpOf(P);
  await call(P);
  const { peak, last: d1 } = await sample(() => dumpOf(P), d0, P);
  console.log(label, JSON.stringify({ before: [d0.supply, d0.bags], peakFalling: peak, after: [d1.supply, d1.bags] }));
  return { d0, d1, peak };
}

// --- single player ---
{
  const P = await game(b, "S", BASE);
  await P.click_(750, 281); await P.click_(750, 281); await P.waitForTimeout(6000);
  await P.click_(640, 360); await P.keyboard.press("Enter"); await P.waitForTimeout(8000);
  const r = await drop(P, "single");
  check("single: exactly one falling crate was spawned", r.peak === 1, r.peak);
  check("single: the falling crate is gone after landing", r.d1.supply === 0, r.d1.supply);
  check("single: exactly one bag landed", r.d1.bags - r.d0.bags === 1, r.d1.bags - r.d0.bags);
  await P.context().close();
}

// --- host + 1 client; the client calls the supply (RPC to the host) ---
{
  const A = await game(b, "A", BASE);
  await A.click_(750, 281); await A.click_(750, 331);
  await A.click_(850, 185); await A.keyboard.type("Supply Room", { delay: 30 });
  await A.click_(750, 481); // unlisted (B joins by code)
  await A.waitForTimeout(5000);
  const rooms = [{ code: (await dumpOf(A)).code }];
  const B = await game(b, "B", BASE);
  await B.click_(750, 281); await B.click_(750, 431); await B.waitForTimeout(800);
  await B.click_(850, 265); await B.keyboard.press("Control+a"); await B.keyboard.type(rooms[rooms.length - 1].code, { delay: 30 });
  await B.click_(750, 431); await B.waitForTimeout(5000);
  await A.click_(640, 360); await A.keyboard.press("Enter"); await A.waitForTimeout(10000);
  const n0 = await dumpOf(A), m0 = await dumpOf(B);
  await call(B);
  // sample both pages once a second: host and client must each see one falling crate and then exactly one bag
  let peakA = 0, peakB = 0, bagsA = 0, bagsB = 0, bagsAllB = 0, landed = -1, a, c;
  for (let t = 0; t < 60; t++) {
    await new Promise((r) => setTimeout(r, 1000));
    await heal(A); await heal(B);
    a = await dumpOf(A); c = await dumpOf(B); if (!a || !c) continue;
    peakA = Math.max(peakA, a.supply); peakB = Math.max(peakB, c.supply); bagsA = Math.max(bagsA, a.bags); bagsB = Math.max(bagsB, c.bags); bagsAllB = Math.max(bagsAllB, c.bagsAll ?? c.bags);
    if (process.env.VERBOSE) console.log("  t=" + t, JSON.stringify({ host: [a.supply, a.bags], client: [c.supply, c.bags, c.bagsAll] }));
    if (landed < 0 && (a.bags > 0 || c.bags > 0)) landed = t;
    if (landed >= 0 && t >= landed + 6) break;
  }
  const n1 = a, m1 = c;
  console.log("mp", JSON.stringify({ host: [n0.supply, n0.bags, peakA, n1.supply, n1.bags], client: [m0.supply, m0.bags, peakB, m1.supply, m1.bags] }));
  check("mp: exactly one falling crate on the host", peakA === 1, peakA);
  check("mp: the client sees exactly one falling crate", peakB === 1, peakB);
  check("mp: falling crate gone on host and client", n1.supply === 0 && m1.supply === 0, J(n1.supply, m1.supply));
  check("mp: exactly one bag on the host (peak)", bagsA === 1 && n1.bags === 1, J(bagsA, n1.bags));
  check("mp: the client sees exactly one bag (peak and now)", bagsB === 1 && m1.bags === 1 && bagsAllB === 1, J(bagsB, m1.bags, bagsAllB));
  // the client opens the bag (RPC openbag, the owner = host acts): the bag must vanish on both
  await B.evaluate(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugOpenBag", ""));
  let gone = false, a2, c2;
  for (let t = 0; t < 15 && !gone; t++) { await new Promise((r) => setTimeout(r, 1000)); await heal(A); await heal(B); a2 = await dumpOf(A); c2 = await dumpOf(B); gone = a2 && c2 && a2.bags === 0 && c2.bags === 0; }
  check("mp: client opens the bag, it disappears on host and client", gone, J(a2 && a2.bags, c2 && c2.bags));
  await A.context().close(); await B.context().close();
}
function J(...a) { return JSON.stringify(a); }
console.log(`${pass} passed, ${fail} failed`);
await Promise.race([b.close(), new Promise((r) => setTimeout(r, 3000))]);
process.exit(fail ? 1 : 0);
