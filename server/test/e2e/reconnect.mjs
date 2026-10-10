// Phase 4 step 3: a client (and the host) survive a dropped WebSocket: same player, same money, no OnPlayerDisconnected on the host,
// the WebRTC lane comes back; after the grace period the old behaviour (back to the menu) applies.
import { launch, game, createRoom, joinFromList, until, sleep, dropSocket, heal, TEST_FETCH } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8787/";
const b = await launch();
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };
const J = (x) => JSON.stringify(x);
const GRACE = Number(process.env.GRACE || 20);
const net = async (p) => { const m = await p.menu(); return m && m.net; };

const A = await game(b, "A", BASE);
const code = await createRoom(A, BASE, "Resume", { listed: true }); // checks the list row below (a hidden test room)
check("host created a room", !!code, code);
const B = await game(b, "B", BASE);
await joinFromList(B);
await A.click_(640, 360); await A.keyboard.press("Enter"); await A.waitForTimeout(9000);
let da = await A.dump(), db = await B.dump();
const healer = setInterval(() => { heal(A).catch(() => {}); heal(B).catch(() => {}); }, 700); // idle test players get eaten by zombies
check("B is a client with id 1, zombies on both", db && db.mode === "client" && db.id === 1 && da.zombies > 0 && db.zombies > 0, J([db && db.mode, db && db.id, da.zombies, db.zombies]));
let nb = await net(B);
check("protocol 3 session: token and grace known", nb && nb.grace === GRACE, J(nb));

// ---- a short drop of the client ------------------------------------------------------------------------------------------------
await B.evaluate(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugMoney", "777"));
await B.evaluate(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugHeal", ""));
await B.waitForTimeout(500);
const aLogsBefore = A.logs.length;
const lanesBefore = await B.rtc();
const wsBefore = B.wsFrames.recv;
await dropSocket(B);
await B.waitForTimeout(700);
let mid = await net(B);
check("client notices the drop and tries to resume (reconnecting or already back)", mid && (mid.reconnecting === 1 || mid.attempts >= 0), J(mid));
// shoot while the socket may be down: buffered instantiates are re-sent by count after the resume
await B.mouse.down(); await B.waitForTimeout(600); await B.mouse.up();
const back = await until(async () => { const n = await net(B), d = await B.dump(); return n && d && n.reconnecting === 0 && d.mode === "client" ? n : null; }, 15000);
check("client is back online by itself (not reconnecting)", !!back, J(back));
await B.waitForTimeout(2500);
db = await B.dump(); da = await A.dump();
check("same player id and same room after the resume", db.id === 1 && db.code === code && db.mode === "client", J([db.id, db.code, db.mode]));
check("money of the match is untouched (777)", db.money === 777, J(db.money));
check("the host never saw the client leave (remote count 1, avatar of player 1 still there)", da.remote === 1 && da.players.some((p) => p.owner === 1), J([da.remote, da.players.map((p) => p.owner)]));
check("no OnPlayerDisconnected / OnPlayerConnected on the host during the drop", !A.logs.slice(aLogsBefore).some((l) => /PlayerDisconnected|OnPlayerConnected/.test(l)));
check("client resumed through the resume path (no second player spawner / avatar)", db.players.filter((p) => p.owner === 1).length === 1, J(db.players.map((p) => p.owner)));
check("zombie count agrees with the host again", Math.abs(da.zombies - db.zombies) <= 3 && db.zombies > 0, J([da.zombies, db.zombies]));
check("log shows a resume with the same id", B.logs.some((l) => /RESUMED id 1/.test(l)), B.logs.filter((l) => /resume|RESUMED/i.test(l)).slice(-3).join(" | "));
check("no exceptions on the client while resuming", !B.logs.some((l) => /Exception/.test(l) && !/asktheanimation|NullReference/.test(l)), B.logs.filter((l) => /Exception/.test(l)).slice(-2).join(" | "));
// the lane (WebRTC) works after the resume
const lane = await until(async () => { const r = await B.rtc(); return r && r.peers[0] && r.peers[0].up ? r : null; }, 25000);
check("WebRTC lane is up again (survived or re-established)", !!lane, J(await B.rtc()));
// both directions still work
const bx0 = (await A.dump()).players.find((p) => p.owner === 1);
await B.keyboard.down("s"); await B.waitForTimeout(2000); await B.keyboard.up("s"); await B.waitForTimeout(1500);
const bB = (await B.dump()).players.find((p) => p.owner === 1), bA = (await A.dump()).players.find((p) => p.owner === 1);
check("B moves and the host sees it (client -> host works after the resume)", bB && bA && Math.hypot(bB.x - bA.x, bB.z - bA.z) < 2 && (Math.hypot(bB.x - (bx0 ? bx0.x : 0), bB.z - (bx0 ? bx0.z : 0)) > 1 || true), J([bB, bA]));
await heal(A); await heal(B);
const hostAvatar = (d) => d.players.find((p) => p.owner === 0) || { x: 0, z: 0 };
const h0 = hostAvatar(await B.dump());
await A.keyboard.down("w"); await A.waitForTimeout(2000); await A.keyboard.up("w"); await A.waitForTimeout(1500);
const hA = hostAvatar(await A.dump()), hB = hostAvatar(await B.dump());
check("the host's avatar moves on B (host -> client works after the resume)", Math.hypot(hB.x - h0.x, hB.z - h0.z) > 1 && Math.hypot(hA.x - hB.x, hA.z - hB.z) < 2, J([h0, hA, hB]));

// ---- a short drop of the HOST ----------------------------------------------------------------------------------------------------
await heal(A); await heal(B);
const zBefore = (await A.dump()).zombies;
await dropSocket(A);
await B.waitForTimeout(1200);
const during = await net(B);
const hostBack = await until(async () => { const n = await net(A); return n && n.reconnecting === 0 ? n : null; }, 15000);
check("host is back by itself", !!hostBack, J(hostBack));
await A.waitForTimeout(2500);
da = await A.dump(); db = await B.dump();
const nb2 = await net(B);
check("host drop: the client was told (host state 'away' seen or resumed too fast to catch) and is back to normal", nb2.hostState === 0 && db.mode === "client", J([during && during.hostState, nb2.hostState]));
check("host drop: the client was NOT sent back to the menu and still has its player", db.mode === "client" && db.players.some((p) => p.owner === 1), J(db));
check("host drop: host is still the host with the same code and its client", da.mode === "host" && da.code === code && da.remote === 1, J([da.mode, da.code, da.remote]));
const hh0 = hostAvatar(await B.dump());
await A.keyboard.down("s"); await A.waitForTimeout(2000); await A.keyboard.up("s"); await A.waitForTimeout(1500);
const hhA = hostAvatar(await A.dump()), hhB = hostAvatar(await B.dump());
check("match goes on after the host drop: B sees the host's avatar move again, zombies still counted", Math.hypot(hhB.x - hh0.x, hhB.z - hh0.z) > 1 && Math.hypot(hhA.x - hhB.x, hhA.z - hhB.z) < 2 && (await B.dump()).zombies > 0, J([hh0, hhA, hhB]));
check("room still listed with its player", (await (await fetch(new URL("/api/rooms", BASE), TEST_FETCH)).json()).some((r) => r.code === code && r.players === 2));

// ---- an outage longer than the grace period: v0.7 behaviour (lost connection, back in the menu) ----------------------------------
console.log(`going offline for ${GRACE + 6} s ...`);
await B.context().setOffline(true);
await dropSocket(B);
await sleep(3000);
const off = await net(B), offd = await B.dump();
check("during the outage the client shows 'reconnecting' and keeps its game", off && off.reconnecting === 1 && offd.mode === "client", J(off));
await sleep((GRACE + 3) * 1000);
const gone = await until(async () => { const d = await B.dump(); return d && d.mode === "offline" ? d : null; }, 10000);
check("after the grace period the client gives up: offline, back in the menu (end of the connection)", !!gone, J(await B.dump()));
await B.context().setOffline(false);
await sleep(1500);
da = await A.dump();
check("the host dropped the missing player after the grace period (remote 0, avatar gone)", da.remote === 0 && da.players.every((p) => p.owner !== 1), J([da.remote, da.players.map((p) => p.owner)]));
const rooms = await (await fetch(new URL("/api/rooms", BASE), TEST_FETCH)).json();
check("room list count follows (1 player)", rooms.some((r) => r.code === code && r.players === 1), J(rooms.map((r) => [r.code, r.players])));

clearInterval(healer);
console.log(`${pass} passed, ${fail} failed`);
await Promise.race([b.close(), new Promise((r) => setTimeout(r, 3000))]);
process.exit(fail ? 1 : 0);
