// Phase 3 (getting into a game): nickname prompt, Quick Play (creates / joins), auto start countdown, invite links, server list fields,
// settings persistence. Headless Chrome pages against a running server.
//   CH=chrome BASE=http://localhost:8787/ node phase3.mjs
import { launch, game } from "./lib.mjs";
import WebSocket from "ws";
const BASE = process.env.BASE || "http://localhost:8787/";
const J = (x) => JSON.stringify(x);
const b = await launch();
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };
const rooms = async () => (await fetch(new URL("/api/rooms", BASE))).json();
const info = async (code) => { const r = await fetch(new URL("/api/room/" + code, BASE)); return { status: r.status, body: await r.json() }; };
const until = async (fn, ms = 20000, step = 400) => { const t0 = Date.now(); for (;;) { const v = await fn(); if (v) return v; if (Date.now() - t0 > ms) return v; await new Promise((r) => setTimeout(r, step)); } };
const nickIfAsked = async (P, name = "Guest") => { const x = await P.menu(); if (x && x.gamestate === 30) { await P.click_(750, 281); await P.keyboard.type(name, { delay: 30 }); await P.click_(750, 381); await P.waitForTimeout(500); } };
const MAIN_QUICK = [750, 231], MAIN_START = [750, 281], MAIN_OPTIONS = [750, 381];

// ---------------------------------------------------------------- A: fresh profile, Quick Play with no room open: nickname prompt, then creates a room
const A = await game(b, "A", BASE, { fresh: true });
check("fresh profile: no room open yet", (await rooms()).length === 0, J(await rooms()));
await A.click_(...MAIN_QUICK); await A.waitForTimeout(800);
let m = await A.menu();
check("first multiplayer use asks for a nickname (state 30)", m && m.gamestate === 30, J(m));
await A.click_(750, 381); await A.waitForTimeout(500);              // Continue with an empty field
m = await A.menu();
check("empty / invalid nickname is refused", m && m.gamestate === 30, J(m));
await A.click_(750, 281); await A.keyboard.type("Vito<>x", { delay: 30 });   // brackets are filtered out
await A.click_(750, 381);
const room = await until(async () => (await rooms()).find((r) => r.comment === "Quick Play"), 20000);
check("Quick Play with no open room creates a listed Quick Play room", !!room, J(await rooms()));
check("the nickname is used for the room name", room && room.name === "Vitox's game", room && room.name);
check("room list row has wave / started / ended / region fields", room && room.wave === 0 && room.started === false && room.ended === false && typeof room.age === "number" && "colo" in room && "country" in room, J(room));
check("room list never exposes lat / lon", room && !("lat" in room) && !("lon" in room));
const code = room && room.code;
const bar = await A.evaluate(() => ({ shown: getComputedStyle(document.querySelector("#invite")).display, url: document.querySelector("#invite-url").value, code: document.querySelector("#invite-code").textContent }));
check("invite bar shows the room code and the invite link", bar.shown !== "none" && bar.code === code && bar.url === new URL("/?room=" + code, BASE).href, J(bar));
// ---------------------------------------------------------------- B: sees the room in the server list, then Quick Play joins it
const B = await game(b, "B", BASE);
await B.click_(...MAIN_START); await B.click_(750, 381); await B.waitForTimeout(3500);      // Start Game > Join server
m = await B.menu();
const row = m && m.list.find((r) => r.code === code);
check("server list: row has players / max / wave / region / ping", row && row.players === 1 && row.max === 16 && row.wave === 0 && row.started === 0 && row.country !== undefined && row.ping >= 0, J(m && m.list));
await B.shot("p3_list.png");
await B.click_(750, 661); await B.click_(750, 481);                                          // Go back, Go back: main menu

await A.click_(640, 360); // spawn (the first click of the original): the auto start countdown begins
m = await until(async () => { const x = await A.menu(); return x && x.countdown > 0 && x; }, 8000, 300);
check("host sees the visible auto start countdown (<= 10 s)", m && m.countdown > 0 && m.countdown <= 10, J(m));
check("not started yet during the countdown", m && m.started === 0, J(m));
await B.click_(...MAIN_QUICK);
const samplesA = [], samplesB = [];
for (let i = 0; i < 14; i++) { const [x, y] = [await A.menu(), await B.menu()]; samplesA.push(x); samplesB.push(y); }
let db = await B.dump();
check("Quick Play joins the existing room (client of that room)", db && db.mode === "client" && db.code === code, J(db));
check("the joining client sees the countdown too", samplesB.some((x) => x && x.countdown > 0), J(samplesB.map((x) => x && x.countdown)));
check("a second player cuts the countdown to 5 s or less", samplesA.some((x) => x && x.countdown > 0 && x.countdown <= 5), J(samplesA.map((x) => x && x.countdown)));
const r1 = await rooms();
check("room has 2 players and still exactly one room exists", r1.length === 1 && r1[0].players === 2, J(r1));
// auto start: no ENTER pressed anywhere
const started = await until(async () => { const d = await A.dump(); return d && d.zombies > 0 && d; }, 25000, 500);
check("match auto starts without ENTER (zombies spawn on the host)", !!started, J(await A.dump()));
const ra = await until(async () => { const r = (await rooms())[0]; return r && r.started && r.wave >= 1 && r; }, 10000, 500);
check("room list shows the match started with a wave", !!ra, J(await rooms()));
const dbb = await B.dump();
check("client has zombies too", dbb && dbb.zombies > 0, J(dbb && dbb.zombies));

// ---------------------------------------------------------------- invite links
const inv = await info(code);
check("/api/room/CODE reports a started match, never the password", inv.status === 200 && inv.body.started === true && !("pw" in inv.body && typeof inv.body.pw === "string"), J(inv));
const D = await game(b, "D", new URL("/?room=" + code, BASE).href);
await D.waitForTimeout(1500); await nickIfAsked(D, "Dee"); await D.waitForTimeout(5000);
const dd = await D.dump();
check("invite link ?room=CODE joins that room directly", dd && dd.mode === "client" && dd.code === code, J(dd));
const q = await D.evaluate(() => location.search);
check("the invite parameter is removed from the address once used", !/room=/.test(q), q);
check("room has 3 players", (await rooms())[0].players === 3, J(await rooms()));

const E = await game(b, "E", new URL("/?room=ZZZZZ", BASE).href);
await E.waitForTimeout(1500); await nickIfAsked(E); await E.waitForTimeout(1500);
m = await E.menu();
check("invite to a room that does not exist: clean error, stays in the menu (state 32)", m && m.gamestate === 32 && /not found|closed/i.test(m.inviteErr), J(m));
await E.shot("p3_invite_error.png");

// wrong build: a room of another version (raw relay socket)
const wsHost = (type) => new Promise((res, rej) => {
  const u = new URL("/ws/host?v=2&max=4&name=Old&type=" + encodeURIComponent(type), BASE); u.protocol = u.protocol.replace("http", "ws");
  const ws = new WebSocket(u); ws.binaryType = "arraybuffer";
  ws.on("message", (d) => { const f = new Uint8Array(d); if (f[2] === 0x81) { const n = f[2 + 1 + 4 + 1 + 1]; res({ ws, code: new TextDecoder().decode(f.slice(2 + 1 + 4 + 1 + 1 + 1, 2 + 1 + 4 + 1 + 1 + 1 + n)) }); } });
  ws.on("error", rej);
});
const other = await wsHost("Zumbiblocks some other build");
const F = await game(b, "F", new URL("/?room=" + other.code, BASE).href);
await F.waitForTimeout(1500); await nickIfAsked(F); await F.waitForTimeout(1500);
m = await F.menu();
check("invite to a room of another build is refused with a clear message", m && m.gamestate === 32 && /another version/i.test(m.inviteErr), J(m));
other.ws.close();

// a closed room: the host leaves, then the link is used
await A.close();
await until(async () => (await rooms()).length === 0, 15000);
const G = await game(b, "G", new URL("/?room=" + code, BASE).href);
await G.waitForTimeout(1500); await nickIfAsked(G); await G.waitForTimeout(1500);
m = await G.menu();
check("invite to a room whose host left: not found / closed", m && m.gamestate === 32 && /not found|closed/i.test(m.inviteErr), J(m));

// ---------------------------------------------------------------- settings: FOV, graphics, invert Y; persist across a reload
const S = await game(b, "S", BASE);
let pr = await S.prefs();
check("untouched profile: v0.7 defaults (fov 70, invert off, graphics Medium = the WebGL quality level 3)", pr && pr.fov === 70 && pr.invy === 0 && pr.quality === 1 && pr.qlevel === 3, J(pr));
await S.click_(...MAIN_OPTIONS); await S.click_(750, 431);         // Options > All settings
m = await S.menu();
check("Options has an All settings screen (state 33)", m && m.gamestate === 33, J(m));
await S.mouse.move(0, 0);
const box = await S.evaluate(() => { const r = document.querySelector("#unity-canvas").getBoundingClientRect(); return { x: r.x, y: r.y }; });
const drag = async (x0, x1, y) => { await S.mouse.move(box.x + x0, box.y + y); await S.mouse.down(); await S.mouse.move(box.x + x1, box.y + y, { steps: 6 }); await S.mouse.up(); await S.waitForTimeout(300); };
await drag(845, 925, 231);                                          // FOV slider (60..110) to about 95
await S.click_(750, 381);                                           // Graphics: Medium -> High
await S.click_(720, 431);                                           // Invert vertical look
await drag(845, 880, 281);                                          // sensitivity slider
pr = await S.prefs();
const sensSet = pr && pr.sens;
check("settings changed in the UI: fov up, sensitivity moved, graphics High (level 4), invert Y on", pr && pr.sens > 1.2 && pr.fov > 85 && pr.quality === 2 && pr.qlevel === 4 && pr.invy === 1, J(pr));
console.log("sens set to", pr && pr.sens);
await S.shot("p3_settings.png");
await S.waitForTimeout(1500);                                       // IndexedDB flush
await S.reloadGame();
pr = await until(async () => { const x = await S.prefs(); return x && x.fov > 85 && Math.abs(x.sens - sensSet) < 0.01 && x; }, 20000, 1000) || await S.prefs(); // the game reads its prefs a moment after the overlay is gone
check("settings persist across a reload (fov, graphics, invert Y, sensitivity)", pr && pr.fov > 85 && pr.quality === 2 && pr.qlevel === 4 && pr.invy === 1 && Math.abs(pr.sens - sensSet) < 0.01, J(pr));
// the saved FOV reaches the player in a match
await S.click_(...MAIN_START); await S.click_(...MAIN_START); await S.waitForTimeout(6000); await S.click_(640, 360); await S.waitForTimeout(2500);
m = await S.menu();
check("the saved FOV is applied to the local player", m && m.deffov === pr.fov, J([m && m.deffov, pr.fov]));

// in game: the pause panel's Options button opens the same settings
await S.keyboard.press("Escape"); await S.waitForTimeout(800);
await S.click_(640, 376); await S.waitForTimeout(500);
m = await S.menu();
check("in-game: pause panel > Options opens the settings overlay", m && m.overlay === 1, J(m));
await S.shot("p3_overlay.png");
await drag(700, 845, 231);                                          // FOV slider of the overlay (x 660..830) to its end
await S.waitForTimeout(1200);
pr = await S.prefs(); m = await S.menu();
check("in-game FOV change applies live to the player and is saved", pr && pr.fov >= 108 && m && m.deffov === pr.fov, J([pr && pr.fov, m && m.deffov]));
await S.click_(640, 481); await S.waitForTimeout(500);
m = await S.menu();
check("Back to the game closes the overlay", m && m.overlay === 0, J(m));

await b.close();
console.log(`${pass} passed, ${fail} failed`);
process.exit(fail ? 1 : 0);
