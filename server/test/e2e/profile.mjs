// Player profiles + cloud save, end to end in the real game (docs/ACCOUNTS.md):
//   * a guest save, then main menu > Profile > Create profile: the guest save moves into the profile, the recovery code is shown once
//   * a change while logged in is uploaded (debounced) and a fresh browser (no cookies, no storage) that logs in gets everything back,
//     after the "Keep profile save" question (it had its own guest save); log out brings that browser's guest save back
//   * an old tab's stale write is refused and that tab takes the newer server copy
//   * reload keeps the session; forgot password with the recovery code; the leaderboard ties the name to the profile ("name (guest)" for
//     anybody else) and the profile screen shows the stats; Delete profile (password) removes it
// Profile names start with "zbt-" (hidden test profiles) and are deleted at the end, also when a check fails. Safe against the live site.
//   BASE=http://localhost:8799/ CH=chrome node profile.mjs
import { launch, game, until } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8787/";
const b = await launch();
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };
const J = (x) => JSON.stringify(x);
const rnd = () => Math.random().toString(36).slice(2, 8);
const send = (p, obj, m, a = "") => p.evaluate(([o, m, a]) => window.unityInstance.SendMessage(o, m, a), [obj, m, a]);
const ask = async (p, obj, m, prefix) => { const n = p.logs.length; await send(p, obj, m); for (let i = 0; i < 60; i++) { await p.waitForTimeout(100); const l = p.logs.slice(n).find((x) => x.startsWith(prefix)); if (l) return JSON.parse(l.slice(prefix.length)); } return null; };
const prof = (p) => ask(p, "ZbProfile", "DebugProfile", "ZBPROF ");
const fields = (p, ...f) => send(p, "ZbProfile", "DebugFields", f.join("\t"));
const api = (p, path, body) => p.evaluate(async ([path, body]) => { const r = await fetch(path, body ? { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(body) } : {}); return { status: r.status, j: await r.json().catch(() => null) }; }, [path, body]);
const hiddenBoard = async (id) => { const j = await (await fetch(new URL("/api/leaderboard?hidden=1", BASE))).json(); return j.boards.find((x) => x.id === id).rows; };
// screen rows: a button at dy sits at y = 360 + dy + 21 on the 1280x720 canvas (right panel centre x 750)
const Y = (dy) => 360 + dy + 21;
const MAIN_PROFILE = [750, Y(150)];

const name = "zbt-" + rnd(), pw1 = "pw-" + rnd() + rnd(), pw2 = "np-" + rnd() + rnd();
let password = pw1;
const pages = [];
try {
  console.log("== guest save -> create profile (migrated)");
  const P = await game(b, "P", BASE); pages.push(P);
  await send(P, "ZbNetRunner", "DebugSetSave", "4321,2,3,1,4,Savey"); await P.waitForTimeout(300);
  await P.click_(...MAIN_PROFILE);
  let s = await prof(P);
  check("main menu > Profile opens the profile screen (guest)", s && s.gamestate === 50 && s.loggedIn === 0, J(s));
  await P.shot("acct_guest.png");
  await P.click_(750, Y(-80));                                   // Create profile
  await fields(P, name, password, password);
  await P.click_(750, Y(20));                                    // Create profile (submit)
  s = await until(async () => { const x = await prof(P); return x && x.gamestate === 54 ? x : null; }, 15000, 500);
  check("signup shows the recovery code once (state 54)", s && /^[A-Z2-9]{4}(-[A-Z2-9]{4}){3}$/.test(s.recovery) && s.loggedIn === 1 && s.name === name, J(s));
  const recovery = s && s.recovery;
  await P.shot("acct_code.png");
  await P.click_(750, Y(-20));                                   // I wrote it down
  s = await prof(P);
  check("back on the profile screen, logged in", s && s.gamestate === 50 && s.loggedIn === 1, J(s));
  await P.shot("acct_profile.png");
  let me = await api(P, "/api/account/me");
  check("the session cookie works from the game page; the guest save moved into the profile", me.status === 200 && me.j.data && me.j.data.money === 4321 && me.j.data.skin === 2 && me.j.data.inv[3] === 5 && me.j.data.nick === name, J(me));
  let pr = await P.prefs();
  check("the nickname is the profile name", pr && pr.nick === name, J(pr));

  console.log("== change while logged in -> uploaded");
  await send(P, "ZbNetRunner", "DebugSetSave", "5555,1,2,0,3,whatever");
  s = await prof(P);
  check("a save marks the profile dirty (upload is debounced)", s && s.dirty === 1, J(s));
  s = await until(async () => { const x = await prof(P); return x && x.dirty === 0 ? x : null; }, 20000, 1000);
  check("uploaded within the debounce window (<= 10 s + 1 s)", !!s && s.rev === 2, J(s || await prof(P)));
  me = await api(P, "/api/account/me");
  check("server copy has the new money, the nickname stays the profile name", me.j.data.money === 5555 && me.j.data.shirt === 3 && me.j.data.nick === name, J(me.j.data));

  console.log("== fresh browser (no cookies, no storage) with its own guest save: log in");
  const Q = await game(b, "Q", BASE); pages.push(Q);
  await send(Q, "ZbNetRunner", "DebugSetSave", "77,0,0,0,0,Guesty"); await Q.waitForTimeout(300);
  await Q.click_(...MAIN_PROFILE); await Q.click_(750, Y(-130)); // Profile > Log in
  await fields(Q, name.toUpperCase(), password);                  // names are case-insensitive
  await Q.click_(750, Y(-30));                                    // Log in
  s = await prof(Q);
  check("a browser with a guest save asks first (state 55)", s && s.gamestate === 55, J(s));
  await Q.shot("acct_keep.png");
  await Q.click_(750, Y(-100));                                   // Keep profile save
  s = await until(async () => { const x = await prof(Q); return x && x.loggedIn === 1 ? x : null; }, 15000, 500);
  pr = await Q.prefs();
  check("logged in: everything is back (money, look, items, volume, nickname)", s && pr && pr.money === 5555 && pr.skin === 1 && pr.beard === 2 && pr.shirt === 3 && pr.inv3 === 5 && pr.vol === 0.5 && pr.nick === name, J([s, pr]));
  check("the guest save is kept aside", s && s.guestSave === 1, J(s));

  console.log("== stale write from an old tab");
  await send(P, "ZbNetRunner", "DebugMoney", "6000"); await send(P, "ZbNetRunner", "DebugSetSave", "6000,1,2,0,3,x");
  await send(P, "ZbProfile", "DebugFlush");
  await until(async () => (await prof(P)).dirty === 0, 15000, 500);
  await send(Q, "ZbNetRunner", "DebugSetSave", "9999,1,2,0,3,x");   // Q is still on the old revision
  await send(Q, "ZbProfile", "DebugFlush");
  s = await until(async () => { const x = await prof(Q); return x && x.dirty === 0 && x.msg.includes("another tab") ? x : null; }, 15000, 500);
  pr = await Q.prefs();
  check("the old tab's write is refused and it loads the newer save", !!s && pr.money === 6000, J([s, pr && pr.money]));
  me = await api(Q, "/api/account/me");
  check("server keeps the newer save", me.j.data.money === 6000, J(me.j.data.money));

  console.log("== reload keeps the session");
  await Q.reloadGame();
  s = await until(async () => { const x = await prof(Q); return x && x.loggedIn === 1 && x.rev >= 3 ? x : null; }, 20000, 1000);
  pr = await Q.prefs();
  check("after F5 still logged in with the profile's save", !!s && pr.money === 6000 && pr.nick === name, J([s, pr && pr.money]));

  console.log("== log out -> the guest save comes back");
  await Q.click_(...MAIN_PROFILE); await Q.click_(750, Y(-100));  // Profile > Log out
  s = await until(async () => { const x = await prof(Q); return x && x.loggedIn === 0 && x.busy === 0 ? x : null; }, 15000, 500);
  pr = await Q.prefs();
  check("logged out: this browser's guest save is back", !!s && pr.money === 77 && pr.nick === "Guesty", J([s, pr]));
  check("the session is gone server side", (await api(Q, "/api/account/me")).status === 401);

  console.log("== leaderboard: a guest using the profile name vs the owner");
  {
    const G = Q; // guest now, nickname = the profile name
    await send(G, "ZbNetRunner", "DebugNick", name);
    await G.click_(750, Y(20)); await G.waitForTimeout(300);     // Go back to the main menu (guest profile screen)
    await G.click_(750, 281); await G.click_(750, 281); await G.waitForTimeout(6000);
    await G.click_(640, 360); await G.keyboard.press("Enter"); await G.waitForTimeout(4000);
    await send(G, "ZbNetRunner", "DebugKills", "2"); await G.waitForTimeout(2000);
    await send(G, "ZbNetRunner", "DebugHurt", "1000");
    await until(async () => { const x = await G.menu(); return x && x.board.result.includes("#") ? x : null; }, 20000, 500);
    await P.click_(750, Y(0)); await P.waitForTimeout(300);      // P (logged in): back to the main menu, Play alone
    await P.click_(750, 281); await P.click_(750, 281); await P.waitForTimeout(6000);
    await P.click_(640, 360); await P.keyboard.press("Enter"); await P.waitForTimeout(4000);
    await send(P, "ZbNetRunner", "DebugKills", "3"); await P.waitForTimeout(2000);
    await send(P, "ZbNetRunner", "DebugHurt", "1000");
    await until(async () => { const x = await P.menu(); return x && x.board.result.includes("#") ? x : null; }, 20000, 500);
    const rows = await hiddenBoard("solo.all.wave");
    check("guest with a registered name is listed as 'name (guest)'", rows.some((r) => r.n === name + " (guest)" && r.k === 2), J(rows.slice(0, 5)));
    check("the logged-in owner's run is listed under the name", rows.some((r) => r.n === name && r.k === 3), J(rows.slice(0, 5)));
    await until(async () => (await P.menu()).gamestate === 12, 15000, 500);
    await P.click_(750, 391); await P.waitForTimeout(500);     // Go back
    await P.click_(...MAIN_PROFILE); await P.waitForTimeout(2500);
    s = await prof(P);
    check("profile screen shows the stats (best wave 1, 3 kills, 1 game)", s && s.stats.bestWave === 1 && s.stats.kills === 3 && s.stats.games === 1, J(s));
    await P.shot("acct_stats.png");
  }

  console.log("== forgot password");
  const R = await game(b, "R", BASE); pages.push(R);
  await R.click_(...MAIN_PROFILE); await R.click_(750, Y(-30));  // Forgot password
  await fields(R, name, pw2, pw2, recovery.toLowerCase());
  await R.click_(750, Y(20));                                     // Reset password (fresh browser: no guest data, no question)
  s = await until(async () => { const x = await prof(R); return x && x.gamestate === 54 ? x : null; }, 15000, 500);
  check("recovery code + new password: logged in, a new code is shown", s && s.loggedIn === 1 && s.recovery && s.recovery !== recovery, J(s));
  if (s && s.loggedIn === 1) password = pw2;
  check("the old sessions are logged out", (await api(P, "/api/account/me")).status === 401);
  pr = await R.prefs();
  check("the recovering browser has the profile's save", pr && pr.money >= 6000, J(pr && pr.money));
  await R.click_(750, Y(-20));                                    // I wrote it down

  console.log("== delete profile");
  await R.click_(750, Y(-50));                                    // Delete profile
  await fields(R, "", password);
  await R.click_(750, Y(-60));                                    // Delete profile (submit)
  s = await until(async () => { const x = await prof(R); return x && x.loggedIn === 0 && x.busy === 0 ? x : null; }, 15000, 500);
  check("profile deleted, the browser is a guest again", !!s && /deleted/.test(s.msg), J(s));
  const login = await api(R, "/api/account/login", { name, password });
  check("the profile is gone", !!s && login.status === 401, J(login));
  if (s && login.status === 401) password = null;
} catch (e) { fail++; console.log("FAIL exception", e.stack); }
finally {
  // never leave a test profile behind (live runs)
  if (password) {
    const p = pages[0] || (await game(b, "X", BASE));
    const r = await p.evaluate(async ([name, password]) => {
      const post = (u, b) => fetch(u, { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(b) });
      const l = await post("/api/account/login", { name, password }); if (l.status !== 200) return "login " + l.status;
      return "delete " + (await post("/api/account/delete", { password })).status;
    }, [name, password]);
    console.log("cleanup:", r);
  }
}
console.log(`${pass} passed, ${fail} failed`);
await Promise.race([b.close(), new Promise((r) => setTimeout(r, 3000))]);
process.exit(fail ? 1 : 0);
