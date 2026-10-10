// Host leaves, level B+ ("continue"): the claim restart continues the old match instead of starting at wave 1.
//   1. host + 2 clients, played to wave >= 3, a client gets two guns, money, kills and loses some health; the host leaves (Disconnect):
//      the new host continues at the same wave (zombie count, per-zombie life and the 120 s timer of that wave), match time and kills of the team
//      carry over, every player has its own items back, money is banked (level A).
//   2. Classic waves + Classic loot room, host closes the tab, the only client continues alone at the same wave with the OLD room's rules
//      (its own settings are Timed / More).
import { launch, game, createRoom, joinByCode, until, sleep, heal } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8787/";
const b = await launch();
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };
const J = (x) => JSON.stringify(x);
const send = (p, m, v = "") => p.evaluate(([m, v]) => window.unityInstance.SendMessage("ZbNetRunner", m, String(v)), [m, v]);
const leaveViaMenu = async (A) => { await A.keyboard.press("Escape"); await A.waitForTimeout(800); await A.click_(640, 412); await A.waitForTimeout(500); await A.click_(585, 377); };
const healers = [];
const keepAlive = (...ps) => healers.push(setInterval(() => ps.forEach((p) => heal(p).catch(() => {})), 700));
const stopAlive = () => { healers.splice(0).forEach(clearInterval); };
const zcount = (wave, players) => Math.min(Math.round((15 + wave - 1) * (1 + 0.5 * (players - 1))), Math.max(50, Math.min(80, Math.round(50 * (1 + 0.5 * (players - 1))))));

async function room(name, n, setup) {
  const A = await game(b, "A", BASE);
  if (setup) await setup(A);
  const code = await createRoom(A, BASE, name);
  const clients = [];
  for (let i = 0; i < n; i++) { const C = await game(b, "C" + (i + 1), BASE); await joinByCode(C, code); clients.push(C); }
  await A.click_(640, 360); await A.keyboard.press("Enter");
  for (const C of clients) await C.click_(640, 360);
  await A.waitForTimeout(6000);
  keepAlive(A, ...clients);
  return { A, clients, code };
}
// the host's wave clock past 120 s: the next wave starts (works in Classic too: the check itself does not look at the mode)
async function toWave(A, w) {
  for (let k = 0; k < 8; k++) {
    const d = await A.dump(); if (d.wave >= w) return d.wave;
    await send(A, "DebugWaveClock", 121);
    await until(async () => (await A.dump()).wave > d.wave, 5000, 300);
  }
  return (await A.dump()).wave;
}
const give = async (p) => { await send(p, "DebugGive", ""); await p.waitForTimeout(300); };
async function settle(code, players, ms = 40000) {
  return until(async () => {
    const ds = []; for (const p of players) ds.push(await p.dump());
    const hosts = ds.filter((d) => d && d.mode === "host" && d.code === code), cls = ds.filter((d) => d && d.mode === "client" && d.code === code);
    return hosts.length === 1 && cls.length === players.length - 1 ? ds : null;
  }, ms, 700);
}

// ---------------------------------------------------------------------------------------------------------------------------------
console.log("== 1. host leaves (Disconnect) at wave >= 3 with two clients: the new match continues at that wave");
{
  const { A, clients: [B, C], code } = await room("Plus1", 2);
  const W = await toWave(A, 3);
  check("the match reached wave 3", W >= 3, J(W));
  await sleep(1500);
  await give(B); await give(B); await give(C);
  await send(B, "DebugMoney", 400); await send(C, "DebugMoney", 150); await send(B, "DebugKills", 9);
  await sleep(800);
  const a0 = await A.dump();
  const b0 = await B.dump(), c0 = await C.dump();
  const bank0 = (await B.prefs()).money;
  check("B carries two extra guns", b0.items.filter((x) => x >= 0).length >= 4, J(b0.items));
  stopAlive(); await heal(B); await send(B, "DebugHurt", 35);
  const hpB = 65;
  console.log("before:", J({ wave: a0.wave, zombies: a0.zombies, zlifeMax: a0.zlifeMax, min: a0.min, killed: a0.killed, Bitems: b0.items, Bquant: b0.quant, Bammo: b0.ammo, Bsel: b0.sel, hpB, Citems: c0.items }));
  await leaveViaMenu(A); // (no healing from here on: B's life must not be topped up until its new avatar was checked)
  await sleep(2000);
  const mb = await B.menu();
  check("B is on the host-left screen", mb.gamestate === 34 || mb.gamestate === 3 || mb.gamestate === 6, J(mb.gamestate));
  const ds = await settle(code, [B, C]);
  check("claim restart: one new host, one client, same room code", !!ds, J(await Promise.all([B.dump(), C.dump()])));
  // health: B's new avatar must come back with the life its old one had at the host-left moment (the restore logs it), not full
  const hpNew = await until(async () => { const d = await B.dump(); return d && d.hp != null ? d.hp : null; }, 20000, 300);
  const rl = B.logs.find((l) => /zbcarry\] restored own player/.test(l)) || "";
  const lifeLogged = Number((/life (-?[0-9.]+)/.exec(rl) || [])[1]);
  check("B's health carried over (not reset to full)", lifeLogged > 0 && lifeLogged < 100 && Math.abs(hpNew - lifeLogged) <= 1, J([hpB, lifeLogged, hpNew]));
  keepAlive(B, C);
  const msgs = await until(async () => { const x = [(await B.dump()).carry.line, (await C.dump()).carry.line]; return x.every((l) => l.includes("continuing at wave " + a0.wave)) ? x : null; }, 15000, 300);
  check("both players see 'Host left - continuing at wave " + a0.wave + "'", !!msgs, J([(await B.dump()).carry, (await C.dump()).carry]));
  if (ds) {
    const [hp, cp] = (await B.dump()).mode === "host" ? [B, C] : [C, B];
    const started = await until(async () => { const d = await hp.dump(); return d && d.started === 1 && d.zombies > 0 ? d : null; }, 45000, 500);
    check("the new host starts the match by itself (players back, then 3 s)", !!started, J(await hp.dump()));
    await sleep(1500);
    const h1 = await hp.dump(), c1 = await cp.dump();
    console.log("after:", J({ wave: h1.wave, zombies: h1.zombies, zlifeMax: h1.zlifeMax, min: h1.min, killed: h1.killed, carry: h1.carry, cwave: c1.mdwave, waveleft: c1.waveleft }));
    check("continues at the same wave (new host and client)", h1.wave === a0.wave && c1.mdwave === a0.wave, J([a0.wave, h1.wave, c1.mdwave]));
    check("zombie count of that wave for 2 players (Timed scaling)", Math.abs(h1.zombies - zcount(a0.wave, 2)) <= 2 && Math.abs(c1.zombies - h1.zombies) <= 3, J([h1.zombies, c1.zombies, zcount(a0.wave, 2)]));
    const zl = await until(async () => { const d = await hp.dump(); return d.zlifeMax != null ? d.zlifeMax : null; }, 10000, 500);
    check("per-zombie life of that wave (same as the old host's newest zombies)", zl === a0.zlifeMax, J([a0.zlifeMax, zl]));
    check("the 120 s wave timer runs from the start of the wave", c1.waveleft > 100 && c1.waveleft <= 120 && h1.timed === 1, J([c1.waveleft, h1.timed]));
    check("match time continues", h1.min * 60 + h1.sec >= a0.min * 60 + a0.sec, J([a0.min, a0.sec, h1.min, h1.sec]));
    check("zombies killed by the team carry over", h1.killed >= a0.killed, J([a0.killed, h1.killed]));
    const b1 = await B.dump(), cc1 = await C.dump();
    check("B has its items back (slots, magazines, spare ammo, selected slot)", J(b1.items) === J(b0.items) && J(b1.quant) === J(b0.quant) && J(b1.ammo) === J(b0.ammo) && b1.sel === b0.sel, J([b0.items, b1.items, b0.quant, b1.quant, b0.ammo, b1.ammo]));
    check("C has its items back", J(cc1.items) === J(c0.items) && J(cc1.ammo) === J(c0.ammo), J([c0.items, cc1.items]));
    check("B's kills carried over", b1.kills === 9, J(b1.kills));
    const bank1 = (await B.prefs()).money;
    check("B's match money was banked (level A), the new match money starts at 0", bank1 === bank0 + 400 && b1.money === 0, J([bank0, bank1, b1.money]));
    check("both see 2 players", h1.players.length === 2 && c1.players.length === 2, J([h1.players.length, c1.players.length]));
    // keep playing: the next wave after the continued one
    await send(hp, "DebugWaveClock", 121);
    const nx = await until(async () => { const d = await hp.dump(); return d.wave === a0.wave + 1 ? d : null; }, 6000, 300);
    check("the next wave follows (wave + 1)", !!nx, J((await hp.dump()).wave));
  }
  for (const p of [B, C]) console.log(p.name, J(p.logs.filter((l) => /zbcarry/.test(l))));
  stopAlive();
  for (const p of [A, B, C]) await p.context().close();
}

// ---------------------------------------------------------------------------------------------------------------------------------
console.log("== 2. Classic waves + Classic loot room, host closes the tab, the only client continues alone with the old room's rules");
{
  const { A, clients: [B], code } = await room("Plus2", 1, async (A) => { await send(A, "DebugWaves", "classic"); await send(A, "DebugLoot", "classic"); });
  const W = await toWave(A, 3);
  check("Classic room reached wave 3", W >= 3, J(W));
  const a0 = await A.dump();
  check("room is Classic / Classic (waveleft -1, original loot)", a0.timed === 0 && a0.loot.probmin !== 50, J([a0.timed, a0.loot]));
  await sleep(2500); // the room rules reach the client (every 2 s)
  await give(B);
  await sleep(500);
  const b0 = await B.dump();
  check("B carries an extra gun", b0.items.filter((x) => x >= 0).length >= 3, J(b0.items));
  await A.close();
  const d = await until(async () => { const x = await B.dump(); return x && x.mode === "host" && x.code === code && x.started === 1 && x.zombies > 0 ? x : null; }, 45000, 700);
  check("B is the new host and the match runs", !!d, J(await B.dump()));
  if (d) {
    await sleep(1000);
    const b1 = await B.dump();
    console.log("after:", J({ wave: b1.wave, zombies: b1.zombies, timed: b1.timed, loot: b1.loot, carry: b1.carry }));
    check("continues at the same wave", b1.wave === a0.wave, J([a0.wave, b1.wave]));
    check("Classic waves kept (B's own setting is Timed)", b1.timed === 0 && b1.waveleft === -1, J([b1.timed, b1.waveleft]));
    check("Classic loot kept (B's own setting is More)", b1.loot && b1.loot.probmin !== 50 && b1.loot.gren === 0, J(b1.loot));
    check("Classic zombie count of that wave (no player scaling)", Math.abs(b1.zombies - (15 + a0.wave - 1)) <= 1, J([b1.zombies, 15 + a0.wave - 1]));
    check("B has its items back", J(b1.items) === J(b0.items) && J(b1.ammo) === J(b0.ammo), J([b0.items, b1.items]));
  }
  console.log(B.name, J(B.logs.filter((l) => /zbcarry/.test(l))));
  stopAlive();
  for (const p of [A, B]) await p.context().close().catch(() => {});
}

console.log(`${pass} passed, ${fail} failed`);
await Promise.race([b.close(), new Promise((r) => setTimeout(r, 3000))]);
process.exit(fail ? 1 : 0);
