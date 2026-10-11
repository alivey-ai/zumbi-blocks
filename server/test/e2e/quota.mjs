// Free plan quota used up (docs/MULTIPLAYER.md "Free plan budget"): the game explains it instead of "You left the game", single player still works.
// Needs the local Worker with ZB_TEST (npm run dev:test): the cookie zbquota=1 makes every Durable Object path fail like a used-up free plan.
//   CH=chrome BASE=http://localhost:8799/ node quota.mjs
import { launch, game, createRoom, joinByCode, until, dropSocket, heal } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8799/";
const b = await launch();
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };
const J = (x) => JSON.stringify(x);
const quota = (p) => p.context().addCookies([{ name: "zbquota", value: "1", url: new URL(BASE).origin }]);
const MAIN_QUICK = [750, 231], QUOTA_SOLO = [750, 331], QUOTA_MENU = [750, 381];

// 1. Quick Play while the quota is used up -> explanation screen; "Play alone" starts single player
const Q = await game(b, "Q", BASE);
await quota(Q);
await Q.click_(...MAIN_QUICK);
let m = await until(async () => { const x = await Q.menu(); return x && x.gamestate === 37 ? x : null; }, 10000);
check("Quick Play: quota screen (state 37)", !!m, J(m && m.gamestate));
check("quota screen tells the local reset time", Q.logs.some((l) => /QUOTA screen, back at \d{1,2}:\d{2}/.test(l)), Q.logs.filter((l) => /QUOTA/.test(l)).join("|"));
await Q.shot("quota_screen.png");
await Q.click_(...QUOTA_SOLO); await Q.waitForTimeout(6000);
let d = await Q.dump();
check("Play alone from the quota screen: single player runs offline", d && d.mode === "offline" && d.players.length === 1, J(d && [d.mode, d.players && d.players.length]));
await Q.click_(640, 360); await Q.keyboard.press("Enter"); await Q.waitForTimeout(8000);
d = await Q.dump();
check("single player wave 1 starts without any Durable Object", d && d.zombies > 0 && d.wave === 1, J(d && [d.zombies, d.wave]));
await Q.context().close();

// 2. Creating a room while the quota is used up -> the same screen (REJECT 11 on the socket)
const C = await game(b, "C", BASE);
await quota(C);
await C.click_(750, 281); await C.click_(750, 331); await C.click_(850, 185); await C.keyboard.type("Quota", { delay: 30 }); await C.click_(750, 481);
m = await until(async () => { const x = await C.menu(); return x && x.gamestate === 37 ? x : null; }, 10000);
check("Create server: quota screen instead of 'connection failed'", !!m, J(m && m.gamestate));
await C.shot("quota_create.png");
await C.click_(...QUOTA_MENU); await C.waitForTimeout(800);
m = await C.menu();
check("Main menu button goes back to the main menu", m && m.gamestate === 0, J(m && m.gamestate));
await C.context().close();

// 3. In a match: the client's connection drops and the quota runs out meanwhile -> resume refused with 11 -> quota screen, match money kept
const A = await game(b, "A", BASE);
const code = await createRoom(A, BASE, "QuotaMid");
const B = await game(b, "B", BASE);
await joinByCode(B, code);
await A.click_(640, 360); await A.keyboard.press("Enter"); await B.click_(640, 360); await A.waitForTimeout(6000);
await heal(A); await heal(B);
await B.evaluate(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugMoney", "555"));
await B.waitForTimeout(300);
const bank0 = (await B.dump()).bank;
await quota(B);
await dropSocket(B);
m = await until(async () => { const x = await B.menu(); return x && x.gamestate === 37 ? x : null; }, 15000);
check("mid-match: the resume is refused (quota) and the client sees the quota screen", !!m, J(m && m.gamestate));
const db = await B.dump();
check("mid-match: the match money was kept (banked)", db && db.bank === bank0 + 555, J([bank0, db && db.bank]));
check("not the old 'You left the game' screen", !B.logs.some((l) => /gamestate 8\b/.test(l)) && m && m.gamestate !== 8);
await B.shot("quota_midmatch.png");
const da = await A.dump();
check("the host keeps running", da && da.mode === "host", J(da && da.mode));
console.log(`${pass} passed, ${fail} failed`);
await Promise.race([b.close(), new Promise((r) => setTimeout(r, 3000))]);
process.exit(fail ? 1 : 0);
