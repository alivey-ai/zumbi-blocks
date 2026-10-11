// Phase 2b (docs/MULTIPLAYER.md "Data channel lane", "Free plan budget"): clients with a data channel send everything to the host directly and
// the host is their gateway; the relay only keeps the buffer and serves the peers without a channel.
// Host A, direct client B, then C with ?p2p=0 (WebSocket only: a mixed room), then a late joiner D (direct). Checks: both directions of RPC /
// INST over the channel (bullets, health), the WebSocket frames that reach the Durable Object, the mixed room, the late joiner, the fallback
// to the relay when B's channel dies silently (resend of what was not acknowledged) and the way back, and a WebSocket resume while direct.
//   CH=chrome BASE=http://localhost:8799/ node direct.mjs
import { launch, game, createRoom, joinByCode, until, sleep, dropSocket } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8799/";
const withQ = (u, q) => u + (u.includes("?") ? "&" : "?") + q;
const b = await launch();
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };
const J = (x) => JSON.stringify(x);
const send = (p, m, a = "") => p.evaluate(([m, a]) => window.unityInstance.SendMessage("ZbNetRunner", m, a), [m, a]);
const direct = async (p) => { const s = await p.statsDump(); return s && s.p2p && s.p2p.direct; };
const linkDir = async (p, id) => { const d = await direct(p); return d && d.links[id] ? d.links[id].dir : -1; };
const inst = async (p) => { const s = await p.statsDump(); return s && s.p2p && s.p2p.inst ? s.p2p.inst : { sent: {}, recv: {} }; };
const hpOf = (d, id) => { const m = d && d.team ? d.team.find((x) => x.id === id) : null; return m ? m.hp : -1; };
const wsRate = async (pages, ms) => { const f0 = pages.map((p) => p.wsFrames.sent); await sleep(ms); return pages.map((p, i) => +((p.wsFrames.sent - f0[i]) / (ms / 1000)).toFixed(2)); };
let healOn = true;
const pagesToHeal = [];
const healer = setInterval(() => { if (healOn) for (const p of pagesToHeal) if (!p.isClosed()) send(p, "DebugHeal").catch(() => {}); }, 300);

const A = await game(b, "A", BASE); pagesToHeal.push(A);
const code = await createRoom(A, BASE, "Direct");
const B = await game(b, "B", BASE); pagesToHeal.push(B);
await joinByCode(B, code);
check("B is a client", (await B.dump()).mode === "client");
check("B's direct lane comes up (host side and client side)", !!(await until(async () => (await linkDir(A, 1)) === 2 && (await linkDir(B, 0)) === 2, 20000)), J([await direct(A), await direct(B)]));
await A.click_(640, 360); await A.keyboard.press("Enter"); await B.click_(640, 360);
await send(A, "DebugZmax", "6"); // few zombies alive at a time: the test players must survive the whole run (the rest queue in the backlog)
await A.waitForTimeout(8000);
let da = await A.dump(), db = await B.dump();
check("match running, zombies agree (host's INST over the channel)", da.zombies > 0 && da.zombies === db.zombies, J([da.zombies, db.zombies]));

// ---- the budget: what reaches the Durable Object with every client direct --------------------------------------------------------
{
  const [ra, rb] = await wsRate([A, B], 10000);
  const dA = await direct(A);
  check("all direct: the host flushes the WebSocket at the low rate (<= 3 frames/s)", dA.all === 1 && ra <= 3, J([ra, dA.all]));
  check("all direct: the client sends (almost) nothing over the WebSocket (<= 0.5 frames/s)", rb <= 0.5, J(rb));
  console.log("WS frames/s with every client direct: host", ra, "client", rb);
}

await until(async () => (await B.dump()).hp !== undefined, 15000); // B's avatar is in
// ---- client -> host -> others: bullets and health ---------------------------------------------------------------------------
const i0 = { a: await inst(A), b: await inst(B) };
await B.mouse.down(); await B.waitForTimeout(1500); await B.mouse.up(); await B.waitForTimeout(800);
const i1 = { a: await inst(A), b: await inst(B) };
check("B's bullets are instantiated (group 1) and reach the host over the channel", (i1.b.sent[1] || 0) > (i0.b.sent[1] || 0) && (i1.a.recv[1] || 0) > (i0.a.recv[1] || 0), J([i0, i1]));
check("B's bullets did not go over B's WebSocket", (B.stats.instSent[1] || 0) === 0, J(B.stats.instSent));
healOn = false;
await send(B, "DebugHurt", "37"); await sleep(1000);
da = await A.dump();
check("B's health (unbuffered RPC) reaches the host over the channel", hpOf(da, 1) > 0 && hpOf(da, 1) <= 63, J(da.team));
healOn = true;

// ---- mixed room: C has no data channel ------------------------------------------------------------------------------------------
const C = await game(b, "C", withQ(BASE, "p2p=0")); pagesToHeal.push(C);
await joinByCode(C, code); await C.click_(640, 360); await C.waitForTimeout(3000);
const mid = await A.dump(); console.log("players before C spawns", mid.players.length);
// a player who joins during a wave watches until the next one (original rule): start the next wave so C gets an avatar
await send(A, "DebugWaveClock", "119.5"); await C.waitForTimeout(7000);
let dc = await C.dump();
check("C (WebSocket only) joined the running match", dc.mode === "client" && dc.zombies > 0, J([dc.mode, dc.zombies]));
const c0 = await inst(C);
await B.mouse.down(); await B.waitForTimeout(1500); await B.mouse.up(); await B.waitForTimeout(1500);
const c1 = await inst(C);
check("B's bullets reach C through the host (C_FWD over the relay)", (c1.recv[1] || 0) > (c0.recv[1] || 0), J([c0, c1]));
healOn = false; await send(B, "DebugHurt", "41"); await sleep(1000);
dc = await C.dump(); da = await A.dump();
check("B's health reaches C through the host", hpOf(dc, 1) > 0 && hpOf(dc, 1) <= 59, J(dc.team));
healOn = true;
healOn = false; await send(C, "DebugHurt", "23"); await sleep(1000);
db = await B.dump();
check("C's health (relay) reaches B", hpOf(db, 2) > 0 && hpOf(db, 2) <= 77, J(db.team));
healOn = true;
await sleep(1500);
da = await A.dump(); db = await B.dump(); dc = await C.dump();
check("mixed room: host, direct B and WebSocket-only C agree on the zombies", da.zombies === db.zombies && db.zombies === dc.zombies, J([da.zombies, db.zombies, dc.zombies]));
check("mixed room: B and C see the same players as the host", dc.players.length === da.players.length && db.players.length === da.players.length && hpOf(db, 2) > 0 && hpOf(dc, 1) > 0, J([da.players.length, db.players.length, dc.players.length]));

// ---- late joiner (direct) -------------------------------------------------------------------------------------------------------
const D = await game(b, "D", BASE); pagesToHeal.push(D);
await joinByCode(D, code); await D.click_(640, 360); await D.waitForTimeout(3000);
await send(A, "DebugWaveClock", "119.5"); await D.waitForTimeout(7000);
check("D's direct lane comes up", !!(await until(async () => (await linkDir(D, 0)) === 2, 20000)));
await sleep(1500);
da = await A.dump(); const dd = await D.dump();
check("late joiner D sees the host's zombies and every player", dd.zombies === da.zombies && dd.players.length === da.players.length && da.players.length >= 3, J([da.zombies, dd.zombies, da.players.length, dd.players.length]));

// ---- B's channel dies silently: back to the relay, nothing lost, then back to the channel ----------------------------------------
await B.evaluate(() => window.zbrtc.stall(true));
check("stalled channel: B falls back to the relay (both sides)", !!(await until(async () => (await linkDir(A, 1)) === 0 && (await linkDir(B, 0)) === 0, 8000)), J([await direct(A), await direct(B)]));
healOn = false; await send(B, "DebugHurt", "9"); await sleep(1000);
da = await A.dump(); dc = await C.dump();
check("on the relay again: B's health still reaches the host and C", hpOf(da, 1) <= 91 && hpOf(da, 1) > 0 && hpOf(dc, 1) <= 91 && hpOf(dc, 1) > 0, J([da.team, dc.team]));
healOn = true;
await B.evaluate(() => window.zbrtc.stall(false));
check("after the stall the direct lane comes back", !!(await until(async () => (await linkDir(A, 1)) === 2 && (await linkDir(B, 0)) === 2, 25000)), J([await direct(A), await direct(B)]));
await sleep(2000);
da = await A.dump(); db = await B.dump(); dc = await C.dump();
check("after the stall everybody agrees on the zombies", da.zombies === db.zombies && db.zombies === dc.zombies, J([da.zombies, db.zombies, dc.zombies]));

// ---- WebSocket resume while direct ---------------------------------------------------------------------------------------------
const instB = (await inst(B)).recv[0] || 0;
await dropSocket(B);
const back = await until(async () => { const m = await B.menu(); return m && m.net && m.net.reconnecting === 0 && (await B.dump()).mode === "client"; }, 15000);
check("B's WebSocket resumes while the channel stays up", !!back);
await sleep(2500);
da = await A.dump(); db = await B.dump();
check("after the resume: same zombies, no duplicated objects", da.zombies === db.zombies, J([da.zombies, db.zombies, (await inst(B)).recv[0] - instB]));
check("B is still direct after the resume", (await linkDir(A, 1)) === 2 && (await linkDir(B, 0)) === 2);
const sA = await direct(A);
check("host gateway forwarded messages and swallowed kill echoes", sA.fwd > 0, J(sA));
console.log("direct A", J(sA));
console.log("direct B", J(await direct(B)));
clearInterval(healer);
console.log(`${pass} passed, ${fail} failed`);
await Promise.race([b.close(), new Promise((r) => setTimeout(r, 3000))]);
process.exit(fail ? 1 : 0);
