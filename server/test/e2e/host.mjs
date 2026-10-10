// Phase 4 step 4: the host leaves.
//   Level A (approved): the players keep the money of the match (banked like the ESC Disconnect does).
//   Level B: a remaining player becomes the new host of the SAME room code ("claim restart") and the group continues in a new match without
//   going through the menu (level B+: the new match continues at the old wave with everybody's items, see hostplus.mjs). Covers: clean leave via Disconnect, tab closed, host connection dead for longer than the grace period, the chosen
//   successor opting out, the last player left, and an old host that comes back.
import { launch, game, createRoom, joinByCode, until, sleep, heal, TEST_FETCH } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8787/";
const b = await launch();
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };
const J = (x) => JSON.stringify(x);
const api = async (code) => { const r = await fetch(new URL("/api/room/" + code, BASE)); return r.status === 200 ? r.json() : { exists: false }; };
const menu = async (p) => p.menu();
const money = (p, v) => p.evaluate((x) => window.unityInstance.SendMessage("ZbNetRunner", "DebugMoney", String(x)), v);
const leaveViaMenu = async (A) => { await A.keyboard.press("Escape"); await A.waitForTimeout(800); await A.click_(640, 412); await A.waitForTimeout(500); await A.click_(585, 377); };
const healers = [];
const keepAlive = (...ps) => healers.push(setInterval(() => ps.forEach((p) => heal(p).catch(() => {})), 700));
const stopAlive = () => { healers.splice(0).forEach(clearInterval); };

async function room(name, n) {
  const pages = [];
  const A = await game(b, "A", BASE); pages.push(A);
  const code = await createRoom(A, BASE, name, { listed: true }); // checks that name + listing survive the claim (a hidden test room)
  const clients = [];
  for (let i = 0; i < n; i++) { const C = await game(b, "C" + (i + 1), BASE); await joinByCode(C, code); clients.push(C); }
  await A.click_(640, 360); await A.keyboard.press("Enter");
  for (const C of clients) await C.click_(640, 360);
  await A.waitForTimeout(6000);
  keepAlive(A, ...clients);
  return { A, clients, code };
}
// after a claim restart: who is host, who is client of the same code
async function settle(code, players, ms = 30000) {
  return until(async () => {
    const ds = []; for (const p of players) ds.push(await p.dump());
    const hosts = ds.filter((d) => d && d.mode === "host" && d.code === code), cls = ds.filter((d) => d && d.mode === "client" && d.code === code);
    return hosts.length === 1 && cls.length === players.length - 1 ? ds : null;
  }, ms, 700);
}

// ---------------------------------------------------------------------------------------------------------------------------------
console.log("== 1. host leaves with Disconnect, two clients with money");
{
  const { A, clients: [B, C], code } = await room("HostLeft1", 2);
  await money(B, 500); await money(C, 300);
  const pb0 = (await B.prefs()).money, pc0 = (await C.prefs()).money;
  const i0 = await api(code);
  check("room is running (3 players, epoch 0)", i0.exists && i0.players === 3 && i0.epoch === 0, J(i0));
  await leaveViaMenu(A);
  await sleep(2500);
  const mb = await menu(B), mc = await menu(C);
  check("both clients get the 'host left' screen (state 34), not the plain lost-connection screen", mb.gamestate === 34 || mc.gamestate === 34 || mb.gamestate === 3 || mc.gamestate === 3, J([mb.gamestate, mc.gamestate]));
  const pb1 = (await B.prefs()).money, pc1 = (await C.prefs()).money;
  check("Level A: B keeps its 500 (banked into the saved money)", pb1 === pb0 + 500, J([pb0, pb1]));
  check("Level A: C keeps its 300", pc1 === pc0 + 300, J([pc0, pc1]));
  check("the money screen value is the match money", mb.net.leftMoney === 500 && mc.net.leftMoney === 300, J([mb.net.leftMoney, mc.net.leftMoney]));
  const ds = await settle(code, [B, C]);
  check("claim restart: exactly one of them is the new host of the same room code, the other a client of it", !!ds, J(await Promise.all([B.dump(), C.dump()])));
  const i1 = await api(code);
  check("room is back: hostState ok, epoch 1, same name, listed", i1.exists && i1.hostState === "ok" && i1.epoch === 1 && i1.name === "HostLeft1", J(i1));
  if (ds) {
    const [hp, cp] = (await B.dump()).mode === "host" ? [B, C] : [C, B];
    const dh = await hp.dump();
    check("new host sees one remote player", dh.remote === 1, J(dh.remote));
    await hp.click_(640, 360); await cp.click_(640, 360); await hp.waitForTimeout(2500);
    for (let k = 0; k < 4 && !((await hp.dump()).zombies > 0); k++) { await hp.click_(640, 360); await hp.keyboard.press("Enter"); await hp.waitForTimeout(4000); }
    await hp.waitForTimeout(3000);
    const d1 = await hp.dump(), d2 = await cp.dump();
    // level B+: the match continues at the wave the old one was on (this room never left wave 1; hostplus.mjs covers higher waves)
    check("the match continues for both: wave 1, zombies on host and client, equal counts", d1.zombies > 0 && d2.zombies > 0 && Math.abs(d1.zombies - d2.zombies) <= 2 && d1.wave === 1, J([d1.zombies, d2.zombies, d1.wave]));
    check("the new match starts with fresh match money (0) and the saved money is untouched", (await hp.dump()).money === 0 && (await cp.dump()).money === 0, J([(await hp.dump()).money, (await cp.dump()).money]));
    check("both players are in the new match (2 avatars each)", d1.players.length === 2 && d2.players.length === 2, J([d1.players.length, d2.players.length]));
    const pa = (await hp.prefs()).money, pb = (await cp.prefs()).money;
    check("banked money is still there in the new match", pa >= Math.min(pb0 + 500, pc0 + 300) - 1 && pb >= Math.min(pb0 + 500, pc0 + 300) - 1, J([pa, pb, pb0, pc0]));
  }
  stopAlive();
  for (const p of [A, B, C]) await p.context().close();
}

// ---------------------------------------------------------------------------------------------------------------------------------
console.log("== 2. host closes the tab (close code 1001), one client left: it takes over alone");
{
  const { A, clients: [B], code } = await room("HostLeft2", 1);
  await money(B, 120);
  const pb0 = (await B.prefs()).money;
  await A.close();
  const d = await until(async () => { const x = await B.dump(); return x && x.mode === "host" && x.code === code ? x : null; }, 20000, 700);
  check("the remaining client is the host of the same room", !!d, J(await B.dump()));
  check("its 120 were banked", (await B.prefs()).money === pb0 + 120, J([pb0, (await B.prefs()).money]));
  const i = await api(code);
  check("room listed again with one player", i.exists && i.players === 1 && i.hostState === "ok" && i.epoch === 1, J(i));
  stopAlive();
  await B.context().close();
}

// ---------------------------------------------------------------------------------------------------------------------------------
console.log("== 3. host's connection dies for good (offline > grace): clients see 'away' first, then the restart");
{
  const { A, clients: [B, C], code } = await room("HostLeft3", 2);
  await A.context().setOffline(true);
  await A.evaluate(() => { const s = window.__zbws; if (s && s.ws) s.ws.close(4000, "test drop"); });
  await sleep(3500);
  const away = (await menu(B)).net.hostState;
  check("clients are told the host's connection is lost (host state 1) while the grace runs", away === 1, J(away));
  check("the room reports the host as away", (await api(code)).hostState === "away", J(await api(code)));
  const ds = await settle(code, [B, C], 40000);
  check("after the grace (15 s) one client becomes the host, the other joins the new match", !!ds, J(await Promise.all([B.dump(), C.dump()])));
  await A.context().setOffline(false);
  await sleep(3000);
  const da = await A.dump();
  check("the old host does not come back as host or split the room (it is offline in the menu)", da.mode === "offline", J(da));
  check("the room has exactly the two new players", (await api(code)).players === 2, J(await api(code)));
  stopAlive();
  for (const p of [A, B, C]) await p.context().close();
}

// ---------------------------------------------------------------------------------------------------------------------------------
console.log("== 4. the chosen successor opts out (Main menu): after the head start another player takes over");
{
  const { A, clients: [B, C], code } = await room("HostLeft4", 2);
  await leaveViaMenu(A);
  await sleep(600);
  const i = await api(code);
  const succ = i.restart && i.restart.successor;
  check("the relay names a successor", succ === 1 || succ === 2, J(i));
  const [S, O] = succ === 1 ? [B, C] : [C, B];
  await until(async () => (await menu(S)).gamestate === 34, 5000, 100);
  await S.click_(750, 381); // "Main menu" on the host-left screen (within the 3 s head start)
  await sleep(500);
  check("the successor that opted out is in the main menu", (await menu(S)).gamestate === 0, J(await menu(S)));
  const dO = await until(async () => { const x = await O.dump(); return x && x.mode === "host" && x.code === code ? x : null; }, 25000, 700);
  check("the other player takes over after the head start", !!dO, J(await O.dump()));
  check("the one who left is NOT dragged back in", (await S.dump()).mode === "offline", J(await S.dump()));
  stopAlive();
  for (const p of [A, B, C]) await p.context().close();
}

console.log("== 5. nobody left to take over: the room closes");
{
  const { A, code } = await room("HostLeft5", 0);
  await leaveViaMenu(A); await sleep(2500);
  const i = await api(code);
  check("room is gone and no longer listed", !i.exists && !(await (await fetch(new URL("/api/rooms", BASE), TEST_FETCH)).json()).some((r) => r.code === code), J(i));
  stopAlive();
  await A.context().close();
}

console.log(`${pass} passed, ${fail} failed`);
await Promise.race([b.close(), new Promise((r) => setTimeout(r, 3000))]);
process.exit(fail ? 1 : 0);
