import { launch, game, TEST_FETCH } from "./lib.mjs";
import assert from "node:assert/strict";
const BASE0 = process.env.BASE || "http://localhost:8787/";
// NETSIM="60,20": every page adds 60 ms round trip +-20 ms jitter to its own socket (client <-> host path is crossed twice: ~2x).
const NETSIM = process.env.NETSIM || "0";
const BASE = NETSIM !== "0" ? BASE0 + (BASE0.includes("?") ? "&" : "?") + "netsim=" + NETSIM : BASE0;
const LEGACY = /legacy=1/.test(BASE);
// p2p (WebRTC fast lane) is on unless the base URL says ?p2p=0. With it on, D (the late joiner) joins with ?p2p=0 on purpose: a mixed room
// (host + two data channel clients + one WebSocket-only client) must keep every peer's zombies in agreement.
const P2P = !/[?&]p2p=0/.test(BASE);
const withQ = (u, q) => u + (u.includes("?") ? "&" : "?") + q;
console.log("e2e BASE", BASE, "NETSIM", NETSIM);
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
const rooms = await (await fetch(new URL("/api/rooms", BASE), TEST_FETCH)).json();
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
check("room list shows 3/16", (await (await fetch(new URL("/api/rooms", BASE), TEST_FETCH)).json())[0].players === 3);

// --- start the match ---
await A.click_(640, 360); await A.keyboard.press("Enter");
// idle test players get eaten (Timed waves scale the zombie count with the players since the playtest changes): keep everybody alive
const healPages = [A, B, C];
const healer = setInterval(() => { for (const p of healPages) if (p && !p.isClosed()) p.evaluate(() => window.unityInstance && window.unityInstance.SendMessage("ZbNetRunner", "DebugHeal", "")).catch(() => {}); }, 700);
await A.waitForTimeout(12000);
da = await A.dump(); db = await B.dump(); dc = await C.dump();
check("wave started everywhere (zombies > 0)", da.zombies > 0 && db.zombies > 0 && dc.zombies > 0, J([da.zombies, db.zombies, dc.zombies]));
check("zombie counts match", da.zombies === db.zombies && db.zombies === dc.zombies, J([da.zombies, db.zombies, dc.zombies]));
check("wave number synced", da.wave === 1, J(da.wave));

// --- shooting sync ---
// bullets: counted by the game on every path (phase 2b: a direct client's instantiates go over the data channel, not its WebSocket)
const gInst = async (p) => { const s = await p.statsDump(); return s && s.p2p && s.p2p.inst ? s.p2p.inst : { sent: {}, recv: {} }; };
const sentBefore = (await gInst(B)).sent[1] || 0; const recvBefore = (await gInst(A)).recv[1] || 0;
await B.click_(640, 360);
await B.mouse.down(); await B.waitForTimeout(1500); await B.mouse.up(); await B.waitForTimeout(800);
const gB = await gInst(B), gA = await gInst(A);
check("B's shots are sent as bullet instantiates (group 1)", (gB.sent[1] || 0) > sentBefore, J([gB, B.stats.instSent]));
check("host receives B's bullets", (gA.recv[1] || 0) > recvBefore, J([gA, A.stats.instRecv]));


{
  // instant local feedback: the shooter has its own bullet / muzzle effect without waiting for any network round trip
  const t0 = Date.now(); await B.mouse.down(); await B.waitForTimeout(80);
  const local = await B.dump(); await B.mouse.up();
  check("shooter sees own bullets immediately (local instantiate, before any echo)", local && local.shots > 0, J([local && local.shots, Date.now() - t0]));
}

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

// --- feel layer: stats overlay, host round trip, compact state, interpolation, instant local shooting feedback ---
{
  const sA = await A.statsDump(), sB = await B.statsDump(), sC0 = await C.statsDump();
  console.log("stats A", J(sA)); console.log("stats B", J(sB));
  check("stats dump works on host and client", !!sA && !!sB);
  const rttB = sB && sB.rtt && sB.rtt["0"];
  check("client measures RTT to the host (echo)", rttB && rttB.n >= 3 && rttB.last >= 0 && rttB.last < 2000, J(rttB));
  check("host measures RTT to both clients", sA && sA.rtt && sA.rtt["1"] && sA.rtt["2"] && sA.rtt["1"].n >= 3, J(sA && sA.rtt));
  if (NETSIM !== "0") {
    const d = Number(NETSIM.split(",")[0]);
    check("netsim shows up in the RTT (host <-> client ~ 2x netsim)", rttB.avg > 1.5 * d, J([rttB.avg, d]));
  }
  check("sender clock / jitter tracked for the host", sB && sB.clockJitter && sB.clockJitter["0"] !== undefined, J(sB && sB.clockJitter));
  if (!LEGACY) {
    if (!P2P) check("state travels as compact messages (S_STATE 0x8d)", (B.stats.recv[0x8d] || 0) > 100 && (A.stats.sent[11] || 0) > 100, J([B.stats.recv[0x8d], A.stats.sent[11]]));
    else {
      const rA = await A.rtc(), rB = await B.rtc(), rC = await C.rtc();
      check("data channel up host<->B and host<->C (direct, local candidates)", rA && rA.peers[1] && rA.peers[1].up && rA.peers[2] && rA.peers[2].up && rB.peers[0] && rB.peers[0].up && rC.peers[0] && rC.peers[0].up, J([rA, rB, rC]));
      check("state travels over the data channel (client got compact state messages there)", sB.p2p && sB.p2p.rxMsgs > 300 && sC0.p2p.rxMsgs > 300, J([sB.p2p, sC0.p2p]));
      check("WebSocket carries (almost) no host state to a DC client (relay skips it)", (B.stats.recv[0x8d] || 0) < 0.1 * sB.p2p.rxMsgs, J([B.stats.recv[0x8d], sB.p2p.rxMsgs]));
      check("host WebSocket carries no state while every client is on the channel", (A.stats.sent[11] || 0) < 0.1 * sA.p2p.txMsgs, J([A.stats.sent[11], sA.p2p.txMsgs]));
      check("stats overlay link status: DC with an RTT", sB.p2p.links["0"] && sB.p2p.links["0"].up === 1 && sB.p2p.links["0"].rtt >= 0, J(sB.p2p.links));
      check("RTT over the channel is measured by the game echo too", sB.rtt["0"] && sB.rtt["0"].n >= 3, J(sB.rtt));
    }
    check("interpolation active on the client", sB.interp.on === 1 && sB.interp.entities > 0 && sB.interp.snaps > 100, J(sB.interp));
    check("interpolation is not starved or teleporting", sB.interp.frames > 0 && sB.interp.starved / sB.interp.frames < 0.05 && sB.interp.teleports < 10, J(sB.interp));
    check("interpolation delay adapted to a sane value (100..350 ms)", sB.interp.avgDelay >= 99 && sB.interp.avgDelay <= 350, J(sB.interp.avgDelay));
  }
  if (!P2P) check("frames carry several messages (batching)", (sB.rx.msgs / Math.max(1, sB.rx.frames)) > 1.5 && (sA.tx.msgs / Math.max(1, sA.tx.frames)) > 1.5, J([sB.rx, sA.tx]));
  else check("data channel messages carry several state messages (batching / chunks)", sB.p2p.rxMsgs / Math.max(1, sB.p2p.rxFrames) > 1.5, J(sB.p2p));
  // F3 overlay toggle
  await B.keyboard.press("F3"); await B.waitForTimeout(600);
  const on = await B.statsDump(); await B.shot("b_overlay_on.png");
  await B.keyboard.press("F3"); await B.waitForTimeout(600);
  const off = await B.statsDump();
  check("F3 toggles the stats overlay", on.overlay === 1 && off.overlay === 0, J([on.overlay, off.overlay]));
}

// --- late join (D) ---
const D = await game(b, "D", P2P ? withQ(BASE, "p2p=0") : BASE); healPages.push(D);
await D.click_(750, 281); await D.click_(750, 431); await D.waitForTimeout(800);
await D.click_(850, 265); await D.keyboard.press("Control+a"); await D.keyboard.type(code.toLowerCase(), { delay: 30 });
await D.click_(750, 431); await D.waitForTimeout(6000);
let dd = await D.dump(); da = await A.dump();
check("late joiner D connected (lowercase code works)", dd && dd.mode === "client", J(dd));
check("late joiner sees zombies like the host", dd.zombies > 0 && Math.abs(dd.zombies - da.zombies) <= 3, J([dd.zombies, da.zombies]));
check("late joiner sees the same players as the host (zombies may have killed some)", da.remote === 3 && dd.players.length === da.players.length, J([da.remote, dd.players.length, da.players.length]));
await D.shot("d_late.png");
if (P2P) {
  await D.waitForTimeout(2000);
  const a2 = (await A.dump()).zpos, d2 = (await D.dump()).zpos, b2 = (await B.dump()).zpos;
  const sameD = a2.filter((p) => d2.includes(p)).length, sameB = a2.filter((p) => b2.includes(p)).length;
  check("mixed room: WebSocket-only D and DC client B both agree with the host on zombie positions", a2.length > 0 && sameD >= a2.length * 0.6 && sameB >= a2.length * 0.6, J([a2.length, sameD, sameB]));
  const rD = await D.rtc();
  check("D (?p2p=0) never opened a data channel", rD && rD.enabled === false && Object.keys(rD.peers).length === 0, J(rD));
}

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

// --- host leaves via in-game Disconnect (phase 4: the remaining player takes over the room, "claim restart") ---
await A.keyboard.press("Escape"); await A.waitForTimeout(800); await A.click_(640, 412); await A.waitForTimeout(500); await A.click_(585, 377); await D.waitForTimeout(7000);
dd = await D.dump(); da = await A.dump();
check("host is offline after Disconnect", da.mode === "offline", J(da));
check("remaining client takes over: host of the same room code, in a fresh match (claim restart; see host.mjs for the full flow)", dd.mode === "host" && dd.code === code, J(dd));
await D.shot("d_after_host_left.png");
const after = await (await fetch(new URL("/api/rooms", BASE), TEST_FETCH)).json();
check("room stays in the list under the same code, hosted by D", after.length === 1 && after[0].code === code && after[0].players === 1, J(after));

clearInterval(healer);
console.log(`${pass} passed, ${fail} failed`);
await Promise.race([b.close(), new Promise((r) => setTimeout(r, 3000))]);
process.exit(fail ? 1 : 0);
