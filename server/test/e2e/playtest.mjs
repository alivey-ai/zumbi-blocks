// Playtest features: timed waves (default) vs Classic, wave number + timer on the HUD, teammates' health.
import { launch, game, createRoom, joinFromList, until, sleep, heal } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8787/";
const b = await launch();
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };
const J = (x) => JSON.stringify(x);
const send = (p, m, a = "") => p.evaluate(([m, a]) => window.unityInstance.SendMessage("ZbNetRunner", m, a), [m, a]);

async function match(name, mode, loot, solo) {
  const A = await game(b, "A", BASE);
  if (mode) await send(A, "DebugWaves", mode);
  if (loot) await send(A, "DebugLoot", loot);
  const code = await createRoom(A, BASE, name);
  let B = null;
  if (!solo) { B = await game(b, "B", BASE); await joinFromList(B); }
  await A.click_(640, 360); await A.keyboard.press("Enter"); if (B) await B.click_(640, 360); await A.waitForTimeout(7000);
  return { A, B, code };
}

console.log("== Timed waves (default)");
{
  const { A, B } = await match("Timed1");
  const hb = setInterval(() => { heal(A).catch(() => {}); heal(B).catch(() => {}); }, 700);
  let da = await A.dump(), db = await B.dump();
  check("host: timed mode on, wave 1 running, timer counting down (1..120 s)", da.timed === 1 && da.wave === 1 && da.waveleft >= 1 && da.waveleft <= 120 && da.zombies > 3, J([da.timed, da.wave, da.waveleft, da.zombies]));
  check("client: HUD wave number equals the host's and the timer is synced", db.mdwave === 1 && db.mdstarted === 1 && db.waveleft >= 1 && Math.abs(db.waveleft - da.waveleft) <= 3, J([db.mdwave, db.waveleft, da.waveleft]));
  await A.shot("../../../docs/img/hud-wave-timer.png");
  // timer runs out with zombies left: wave 2 starts, the zombies of wave 1 stay
  const z0 = da.zombies;
  await send(A, "DebugWaveClock", "116");
  const w2 = await until(async () => { const d = await A.dump(); return d.wave === 2 ? d : null; }, 15000, 500);
  check("timer ran out: wave 2 started although zombies were left", !!w2 && w2.zombies >= z0, J([w2 && w2.wave, w2 && w2.zombies, z0]));
  await B.waitForTimeout(1500); db = await B.dump(); da = await A.dump();
  check("client HUD shows wave 2 and a fresh timer", db.mdwave === 2 && db.waveleft > 100, J([db.mdwave, db.waveleft]));
  // timed mode: the timer is armed again for wave 2 (the original would not arm it after a timed-out wave)
  await send(A, "DebugWaveClock", "116");
  const w3 = await until(async () => { const d = await A.dump(); return d.wave === 3 ? d : null; }, 15000, 500);
  check("timer is armed for every wave: wave 3 also starts by the clock", !!w3, J(await A.dump()));
  // late joiner gets the timer too
  clearInterval(hb);
  await A.context().close(); await B.context().close();
}

console.log("== Classic waves (original rules)");
{
  const { A, B } = await match("Classic1", "classic");
  const hb = setInterval(() => { heal(A).catch(() => {}); heal(B).catch(() => {}); }, 700);
  let da = await A.dump(), db = await B.dump();
  check("classic: no timer shown (waveleft -1), wave 1", da.timed === 0 && da.waveleft === -1 && db.waveleft === -1 && da.wave === 1, J([da.timed, da.waveleft, db.waveleft, da.wave]));
  await send(A, "DebugWaveClock", "116"); // the original's one-time 120 s timer (armed while the previous wave was cleared)
  const w2 = await until(async () => { const d = await A.dump(); return d.wave === 2 ? d : null; }, 15000, 500);
  check("classic: the original's first timeout still starts wave 2", !!w2, J(await A.dump()));
  await send(A, "DebugWaveClock", "116");
  await A.waitForTimeout(9000);
  da = await A.dump();
  check("classic: after a timed-out wave the timer is NOT armed again (original behaviour): still wave 2 with zombies left", da.wave === 2 && da.zombies > 3, J([da.wave, da.zombies]));
  clearInterval(hb);
  await A.context().close(); await B.context().close();
}

console.log("== Teammates' health");
{
  const { A, B } = await match("Team1");
  const hb = setInterval(() => { heal(A).catch(() => {}); }, 700); // B is hurt on purpose
  await B.waitForTimeout(1500);
  let da = await A.dump();
  const mate = (d) => (d.team || []).find((m) => m.id === 1);
  check("host sees client B in the team list with full health", !!mate(da) && mate(da).hp >= 60 && !mate(da).dead, J(da.team));
  check("B sees the host in its team list", (await B.dump()).team.some((m) => m.id === 0 && m.hp >= 90), J((await B.dump()).team));
  await send(B, "DebugHurt", "40");
  const hurt = await until(async () => { const m = mate(await A.dump()); return m && m.hp > 0 && m.hp <= 65 ? m : null; }, 6000, 400);
  check("B takes damage: the host's list shows B's health dropping live", !!hurt, J((await A.dump()).team));
  await A.shot("../../../docs/img/team-health.png");
  await send(B, "DebugHurt", "1000");
  const dead = await until(async () => { const m = mate(await A.dump()); return m && m.dead ? m : null; }, 8000, 400);
  check("B dies: shown as down", !!dead, J((await A.dump()).team));
  clearInterval(hb);
  await B.context().close(); await A.waitForTimeout(3000);
  check("B leaves: removed from the list", !mate(await A.dump()), J((await A.dump()).team));
  await A.context().close();
}

console.log("== Zombies per wave scale with the players (Timed), Classic unchanged");
{
  const S = await match("Solo1", null, null, true);
  const hs = setInterval(() => heal(S.A).catch(() => {}), 700);
  const solo = await S.A.dump();
  check("1 player, Timed: wave 1 has the original 15 zombies", solo.wave === 1 && solo.zombies === 15, J([solo.wave, solo.zombies]));
  clearInterval(hs); await S.A.context().close();
  const T = await match("Two1");
  const ht = setInterval(() => { heal(T.A).catch(() => {}); heal(T.B).catch(() => {}); }, 700);
  const two = await T.A.dump();
  check("2 players, Timed: wave 1 has 1.5x the zombies (round(15 x 1.5) = 22 or 23)", two.wave === 1 && two.zombies >= 22 && two.zombies <= 23, J([two.wave, two.zombies]));
  clearInterval(ht); await T.A.context().close(); await T.B.context().close();
  const C = await match("Cls2", "classic");
  const hc = setInterval(() => { heal(C.A).catch(() => {}); heal(C.B).catch(() => {}); }, 700);
  const cls = await C.A.dump();
  check("2 players, Classic: still the original 15", cls.wave === 1 && cls.zombies === 15, J([cls.wave, cls.zombies]));
  clearInterval(hc); await C.A.context().close(); await C.B.context().close();
}

console.log("== Loot: More (scales with the players, grenades) vs Classic");
{
  const { A, B } = await match("Loot1");
  const hb = setInterval(() => { heal(A).catch(() => {}); heal(B).catch(() => {}); }, 700);
  let d = await A.dump();
  check("More loot, 2 players: one item every ~7 s, cap 50, 50% real items, 10% backpacks, grenades in the list",
    d.loot && Math.abs(d.loot.rate - 1 / 7) < 0.01 && d.loot.max === 50 && d.loot.probmin === 50 && d.loot.bp === 5 && d.loot.kinds === 10 && d.loot.gren === 2 && d.loot.spawners === 2, J(d.loot));
  await B.context().close();
  const one = await until(async () => { const x = await A.dump(); return x.loot && x.loot.spawners === 1 && x.loot.max === 40 ? x : null; }, 15000, 500);
  check("second player leaves: cap 40, one item every 10 s", !!one && Math.abs(one.loot.rate - 0.1) < 0.005, J(one && one.loot));
  // grenades spawn as map loot and can be picked up like any ground item
  let g = 0;
  for (let i = 0; i < 80 && !g; i++) { await send(A, "DebugLootNow", "100"); await A.waitForTimeout(250); g = (await A.dump()).gren; }
  check("a grenade spawned as map loot", g > 0, J([g, (await A.dump()).grounditems]));
  const before = (await A.dump()).items;
  await send(A, "DebugGrab", "grenadeground"); await A.waitForTimeout(1200);
  const after = await A.dump();
  const gotId = after.items.filter((x, i) => x !== before[i]);
  check("the grenade was picked up into the inventory (item list changed, ground item gone)", gotId.length >= 1 && after.gren < g, J([before, after.items, g, after.gren]));
  clearInterval(hb); await A.context().close();
  const C = await match("LootC", null, "classic");
  const hc = setInterval(() => { heal(C.A).catch(() => {}); heal(C.B).catch(() => {}); }, 700);
  d = await C.A.dump();
  check("Classic loot: the original numbers (rate 0.05, cap 20, probmin 30, backpack 1, 8 kinds, no grenades), whatever the player count", d.loot && Math.abs(d.loot.rate - 0.05) < 0.001 && d.loot.max === 20 && d.loot.probmin === 30 && d.loot.bp === 1 && d.loot.kinds === 8 && d.loot.gren === 0, J(d.loot));
  clearInterval(hc); await C.A.context().close(); await C.B.context().close();
}

console.log(`${pass} passed, ${fail} failed`);
await Promise.race([b.close(), new Promise((r) => setTimeout(r, 3000))]);
process.exit(fail ? 1 : 0);
