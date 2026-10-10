// Phase 5 leaderboard, end to end in the real game (docs/LEADERBOARD.md):
//   * a fresh profile is asked for a nickname before Play alone
//   * single player: /api/run token at the start, /api/score at the end, rank shown on the end-of-match screen, row on the (hidden) solo board
//   * multiplayer: host + client in an UNLISTED room (joined by code, so no real player can wander in), the relay records the match when everybody is dead, both see the rank,
//     row on the (hidden) team board with the host's per-player kills
//   * the Leaderboard screen opens from the main menu and loads the boards; the web page panel renders
// Test nicknames start with "zbt-": stored hidden, never on the public boards (read back with ?hidden=1). Safe against the live site.
//   BASE=http://localhost:8799/ CH=chrome node leaderboard.mjs
import { launch, game, joinByCode, until, TEST_FETCH } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8787/";
const b = await launch();
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };
const J = (x) => JSON.stringify(x);
const send = (p, m, a = "") => p.evaluate(([m, a]) => window.unityInstance.SendMessage("ZbNetRunner", m, a), [m, a]);
const rnd = () => Math.random().toString(36).slice(2, 8);
const hiddenBoard = async (id) => { const j = await (await fetch(new URL("/api/leaderboard?hidden=1", BASE))).json(); return j.boards.find((x) => x.id === id).rows; };

console.log("== nickname before Play alone");
{
  const F = await game(b, "F", BASE, { fresh: true });
  await F.click_(750, 281); await F.click_(750, 281); await F.waitForTimeout(800);
  const m = await F.menu();
  check("fresh profile: Play alone opens the nickname prompt (state 30)", m && m.gamestate === 30, J(m && m.gamestate));
  for (let i = 0; i < 3 && (await F.menu()).gamestate === 30; i++) { // the IMGUI text field sometimes misses the first click
    await F.click_(750, 281); await F.waitForTimeout(300); await F.keyboard.type("zbt-" + rnd(), { delay: 50 }); await F.waitForTimeout(300); await F.click_(750, 381); await F.waitForTimeout(3000); // Continue
  }
  await F.waitForTimeout(3000);
  const d = await F.dump(), fm = await F.menu();
  check("after the nickname the single player match starts", d && d.mode === "offline" && fm.gamestate === 3 && fm.nick.startsWith("zbt-"), J([d && d.mode, fm.gamestate, fm.nick]));
  await F.context().close();
}

console.log("== single player");
{
  const nick = "zbt-" + rnd();
  const S = await game(b, "S", BASE);
  await send(S, "DebugNick", nick);
  await S.click_(750, 281); await S.click_(750, 281); await S.waitForTimeout(6000);
  await S.click_(640, 360); await S.keyboard.press("Enter"); await S.waitForTimeout(4000);
  let m = await S.menu();
  check("solo match started and got a start token", m.started === 1 && m.board.token === 1 && m.board.solo === 1, J(m.board));
  await send(S, "DebugKills", "4");
  await S.waitForTimeout(3000);
  await send(S, "DebugHurt", "1000");
  m = await until(async () => { const x = await S.menu(); return x && x.gamestate === 12 && x.board.result.includes("#") ? x : null; }, 20000, 500);
  check("end of match screen shows the rank", !!m && /solo, best wave\): #\d+ today, #\d+ all time/.test(m.board.result), J(m && m.board));
  await S.shot("lb_solo_end.png");
  const rows = await hiddenBoard("solo.all.wave");
  const row = rows.find((r) => r.n === nick);
  check("solo result on the (hidden) solo board: wave 1, 4 kills", row && row.w === 1 && row.k === 4, J(row));
  // Leaderboard screen from the main menu
  await S.click_(750, 391); await S.waitForTimeout(500); // Go back
  await S.click_(750, 431); await S.waitForTimeout(2500); // main menu: Leaderboard (the old grey Credits row)
  m = await S.menu();
  check("main menu Leaderboard button opens the screen and loads the boards", m.gamestate === 35 && m.board.rows >= 0, J([m.gamestate, m.board.rows]));
  await S.shot("lb_screen.png");
  // web page panel
  const panel = await S.evaluate(() => document.querySelector("#lb-rows").textContent);
  check("web page leaderboard panel rendered", panel.length > 0 && !/Loading/.test(panel), panel.slice(0, 80));
  await S.context().close();
}

console.log("== multiplayer (host + client by code)");
{
  const na = "zbt-" + rnd(), nb = "zbt-" + rnd();
  const A = await game(b, "A", BASE);
  await send(A, "DebugNick", na);
  // unlisted room (the original default; createRoom() switches "Show in server list" on): nobody else can find it, B joins by code
  await A.click_(750, 281); await A.click_(750, 331);
  await A.click_(850, 185); await A.keyboard.type("zbt " + rnd().slice(0, 5), { delay: 30 });
  await A.click_(750, 481); await A.waitForTimeout(5000);
  const code = (await A.dump()).code;
  const listed = (await (await fetch(new URL("/api/rooms", BASE), TEST_FETCH)).json()).some((r) => r.code === code);
  check("test room is not in the public server list", !listed);
  if (listed) { console.log("room is listed: stopping before anybody can join it"); await A.context().close(); await b.close(); process.exit(1); }
  check("host created a room", !!code, code);
  const B = await game(b, "B", BASE);
  await send(B, "DebugNick", nb);
  await joinByCode(B, code);
  await A.click_(640, 360); await A.keyboard.press("Enter"); await B.click_(640, 360); await A.waitForTimeout(6000);
  const da = await A.dump(), db = await B.dump();
  check("both in the match", da.mode === "host" && db.mode === "client" && db.code === code, J([da.mode, db.mode, db.code]));
  await send(A, "DebugKills", "6,2");
  await A.waitForTimeout(16000); // the host sends the scores at least every 15 s
  await send(A, "DebugHurt", "1000"); await send(B, "DebugHurt", "1000");
  const rb = await until(async () => { const x = await B.menu(); return x && x.board.result.includes("#") ? x : null; }, 20000, 500);
  const ra = await until(async () => { const x = await A.menu(); return x && x.board.result.includes("#") ? x : null; }, 10000, 500);
  check("client sees the team rank (S_RESULT)", !!rb && /team, best wave\)/.test(rb.board.result), J(rb && rb.board));
  check("host sees the team rank", !!ra && /team, best wave\)/.test(ra.board.result), J(ra && ra.board));
  await until(async () => { const x = await B.menu(); return x && x.gamestate === 12; }, 15000, 500);
  await B.shot("lb_team_end.png");
  const rows = await hiddenBoard("team.all.wave");
  const row = rows.find((r) => r.n === `${na}, ${nb}` || r.n === `${nb}, ${na}`);
  check("team result on the (hidden) team board: 8 kills", row && row.k === 8 && row.w >= 1, J(row || rows.slice(0, 3)));
  const pub = await (await fetch(new URL("/api/leaderboard", BASE))).text();
  check("public boards never show test names", !pub.includes(na) && !pub.includes(nb));
  await A.context().close(); await B.context().close();
}

console.log(`${pass} passed, ${fail} failed`);
await Promise.race([b.close(), new Promise((r) => setTimeout(r, 3000))]);
process.exit(fail ? 1 : 0);
