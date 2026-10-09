import { launch, game } from "./lib.mjs";
import assert from "node:assert/strict";
const BASE = process.env.BASE || "http://localhost:8787/";
const b = await launch();
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };
const J = (x) => JSON.stringify(x);

// --- A hosts (listed) ---
const A = await game(b, "A", BASE);
await A.click_(750, 281); await A.click_(750, 331);
await A.click_(850, 185); await A.keyboard.type("E2E Room", { delay: 30 });
await A.click_(750, 331); await A.click_(750, 481);
await A.waitForTimeout(5000);
const rooms = await (await fetch(new URL("/api/rooms", BASE))).json();
check("room appears in /api/rooms", rooms.length === 1 && rooms[0].name === "E2E Room", J(rooms));
const code = rooms[0].code;

// --- B joins from the server list ---
const B = await game(b, "B", BASE);
await B.click_(750, 281); await B.click_(750, 381); await B.waitForTimeout(1500);
await B.click_(400, 45); await B.click_(750, 561); await B.waitForTimeout(5000);
let da = await A.dump(), db = await B.dump();
check("B joined via list (client, id 1)", db && db.mode === "client" && db.id === 1, J(db));
check("host sees 2 players", da && da.players.length === 2 && da.remote === 1, J(da));

// --- C joins by room code (Direct IP) ---
const C = await game(b, "C", BASE);
await C.click_(750, 281); await C.click_(750, 431); await C.waitForTimeout(800);
await C.shot("c_direct.png");
await C.click_(850, 265); await C.keyboard.press("Control+a"); await C.keyboard.type(code, { delay: 30 }); await C.waitForTimeout(300);
await C.shot("c_direct2.png");
await C.click_(750, 431); await C.waitForTimeout(5000);
let dc = await C.dump(); da = await A.dump(); db = await B.dump();
check("C joined by code (client, id 2)", dc && dc.mode === "client" && dc.id === 2, J(dc));
check("everyone sees 3 players", da.players.length === 3 && db.players.length === 3 && dc.players.length === 3, J([da.players.length, db.players.length, dc.players.length]));
check("room list shows 3/16", (await (await fetch(new URL("/api/rooms", BASE))).json())[0].players === 3);

// --- start the match ---
await A.click_(640, 360); await A.keyboard.press("Enter"); await A.waitForTimeout(12000);
da = await A.dump(); db = await B.dump(); dc = await C.dump();
check("wave started everywhere (zombies > 0)", da.zombies > 0 && db.zombies > 0 && dc.zombies > 0, J([da.zombies, db.zombies, dc.zombies]));
check("zombie counts match", da.zombies === db.zombies && db.zombies === dc.zombies, J([da.zombies, db.zombies, dc.zombies]));
check("wave number synced", da.wave === 1, J(da.wave));

// --- movement sync ---
const posOf = (d, name) => d.players.find((p) => p.name === name);
await A.keyboard.down("w"); await A.waitForTimeout(3000); await A.keyboard.up("w"); await A.waitForTimeout(1200);
da = await A.dump(); db = await B.dump(); dc = await C.dump();
const mine = da.players.find((p) => p.owner === 0) || { x: 0, z: 0 };
const onB = db.players.find((p) => p.owner === 0) || mine, onC = dc.players.find((p) => p.owner === 0) || mine;
console.log("A pos", J(mine), "seen on B", J(onB), "on C", J(onC));
check("A's avatar position matches on B and C", Math.hypot(mine.x - onB.x, mine.z - onB.z) < 2 && Math.hypot(mine.x - onC.x, mine.z - onC.z) < 2);
await B.keyboard.down("s"); await B.waitForTimeout(3000); await B.keyboard.up("s"); await B.waitForTimeout(1200);
da = await A.dump(); db = await B.dump();
const bOnB = db.players.find((p) => p.owner === 1), bOnA = da.players.find((p) => p.owner === 1);
console.log("B pos", J(bOnB), "seen on A", J(bOnA));
check("B's avatar position matches on host (or B died: skipped)", !bOnB || !bOnA || Math.hypot(bOnB.x - bOnA.x, bOnB.z - bOnA.z) < 2);

{
  const a1 = (await A.dump()).zpos, b1 = (await B.dump()).zpos;
  const same = a1.filter((p) => b1.includes(p)).length;
  check("host and client agree on zombie positions", a1.length > 0 && same >= a1.length * 0.6, J([a1.length, same]));
}

// --- shooting sync ---
const sentBefore = B.stats.instSent[1] || 0; const recvBefore = A.stats.instRecv[1] || 0;
await B.click_(640, 360);
await B.mouse.down(); await B.waitForTimeout(1500); await B.mouse.up(); await B.waitForTimeout(500);
check("B's shots are sent as bullet instantiates (group 1)", (B.stats.instSent[1] || 0) > sentBefore, J(B.stats.instSent));
check("host receives B's bullets", (A.stats.instRecv[1] || 0) > recvBefore, J(A.stats.instRecv));

// --- late join (D) ---
const D = await game(b, "D", BASE);
await D.click_(750, 281); await D.click_(750, 431); await D.waitForTimeout(800);
await D.click_(850, 265); await D.keyboard.press("Control+a"); await D.keyboard.type(code.toLowerCase(), { delay: 30 });
await D.click_(750, 431); await D.waitForTimeout(6000);
let dd = await D.dump(); da = await A.dump();
check("late joiner D connected (lowercase code works)", dd && dd.mode === "client", J(dd));
check("late joiner sees zombies like the host", dd.zombies > 0 && Math.abs(dd.zombies - da.zombies) <= 3, J([dd.zombies, da.zombies]));
check("late joiner sees the same players as the host (zombies may have killed some)", da.remote === 3 && dd.players.length === da.players.length, J([da.remote, dd.players.length, da.players.length]));
await D.shot("d_late.png");

// --- client leaves (C closes the tab) ---
const beforeC = (await A.dump()).players.filter((p) => p.owner === 2).length;
await C.close(); await A.waitForTimeout(3000);
da = await A.dump(); db = await B.dump();
check("host remote count drops after C leaves", da.remote === 2, J(da.remote));
check("C's avatar is gone on host and on B", da.players.every((p) => p.owner !== 2) && db.players.every((p) => p.owner !== 2), J([beforeC, da.players.map((p) => p.owner), db.players.map((p) => p.owner)]));

// --- B uses the in-game menu: Esc, Disconnect ---
await B.keyboard.press("Escape"); await B.waitForTimeout(800); await B.click_(640, 412); await B.waitForTimeout(500); await B.click_(585, 377); await B.waitForTimeout(3000);
db = await B.dump(); da = await A.dump();
check("B left via in-game Disconnect (offline, back in menu)", db.mode === "offline", J(db));
check("host dropped B (remote count and avatar)", da.remote === 1 && da.players.every((p) => p.owner !== 1), J([da.remote, da.players.map((p) => p.owner)]));
await B.shot("b_after_disconnect.png");

// --- host leaves via in-game Disconnect ---
await A.keyboard.press("Escape"); await A.waitForTimeout(800); await A.click_(640, 412); await A.waitForTimeout(500); await A.click_(585, 377); await D.waitForTimeout(4000);
dd = await D.dump(); da = await A.dump();
check("host is offline after Disconnect", da.mode === "offline", J(da));
check("remaining client is disconnected when host leaves", dd.mode === "offline", J(dd));
await D.shot("d_after_host_left.png");
await D.waitForTimeout(500);
const after = await (await fetch(new URL("/api/rooms", BASE))).json();
check("room removed from the list", after.length === 0, J(after));

console.log(`${pass} passed, ${fail} failed`);
await b.close();
process.exit(fail ? 1 : 0);
