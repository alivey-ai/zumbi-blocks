// Phase 5 leaderboard tests (docs/LEADERBOARD.md).
//   unit:        node test/scores.test.mjs --unit             (no server needed)
//   integration: npm run dev:test (wrangler dev on :8799 with ZB_TEST and a fresh local D1), then BASE=localhost:8799 node test/scores.test.mjs
// All names start with "zbt-": test rows are stored hidden and read back with /api/leaderboard?hidden=1.
import WebSocket from "ws";
import assert from "node:assert/strict";
import { cleanName, implausible, spawnBound, makeToken, checkToken, isTestName, utcDay } from "../src/scores.js";

const BASE = process.env.BASE || "localhost:8787";
const enc = new TextEncoder();
let ok = 0;
const test = async (name, fn) => { try { await fn(); ok++; console.log("ok   " + name); } catch (e) { console.log("FAIL " + name + "\n" + e.stack); process.exitCode = 1; } };
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const rnd = () => Math.random().toString(36).slice(2, 8);

// ---- unit ------------------------------------------------------------------------------------------------------------------------------
await test("cleanName: the game's nickname rule", () => {
  assert.equal(cleanName("  Vito  "), "Vito");
  assert.equal(cleanName("a  b"), "a b");
  assert.equal(cleanName("Žan_1.-"), "Žan_1.-");
  assert.equal(cleanName("x"), null);
  assert.equal(cleanName("elevenchars"), null);
  assert.equal(cleanName("Player"), null);
  assert.equal(cleanName("<script>"), null);
  assert.equal(cleanName("a‮b"), null);
  assert.equal(cleanName(42), null);
  assert.ok(isTestName("zbt-abc") && isTestName("ZBT-x") && !isTestName("zbtest"));
});
await test("implausible: wave vs time, kills vs wave / time", () => {
  assert.equal(implausible({ wave: 1, kills: 0, elapsed: 0, players: 1 }), null);
  assert.equal(implausible({ wave: 10, kills: 150, secs: 600, elapsed: 610, players: 1 }), null);
  assert.equal(implausible({ wave: 30, kills: 20, elapsed: 60, players: 1 }), "wave too fast");
  assert.equal(implausible({ wave: 2, kills: 500, elapsed: 600, players: 1 }), "kills vs wave");
  assert.equal(implausible({ wave: 0, kills: 0, elapsed: 10, players: 1 }), "wave");
  assert.equal(implausible({ wave: 1.5, kills: 0, elapsed: 10, players: 1 }), "wave");
  assert.equal(implausible({ wave: 1, kills: -1, elapsed: 10, players: 1 }), "kills");
  assert.equal(implausible({ wave: 3, kills: 10, secs: 900, elapsed: 60, players: 1 }), "match time vs real time");
  assert.equal(implausible({ wave: 20, kills: 200, elapsed: 30, players: 4 }), "wave too fast");
  assert.ok(spawnBound(1, 1) >= 15 && spawnBound(5, 2) > spawnBound(5, 1));
});
await test("solo token: signed, expires, tamper-proof", async () => {
  const env = { ZB_SECRET: "s1" }, now = Date.now();
  const t = await makeToken(env, now);
  const c = await checkToken(env, t, now + 1000);
  assert.ok(c && c.ms === now && c.nonce.length === 18);
  assert.equal(await checkToken({ ZB_SECRET: "other" }, t, now), null);
  assert.equal(await checkToken(env, t.replace(/^\d/, (d) => String((+d + 1) % 10)), now), null);
  assert.equal(await checkToken(env, t, now + 13 * 3600_000), null);
  assert.equal(await checkToken(env, "x", now), null);
});
if (process.argv.includes("--unit")) { console.log(`${ok} passed`); process.exit(process.exitCode || 0); }

// ---- integration (running worker) ------------------------------------------------------------------------------------------------------
const http = (path, opts = {}) => fetch(`http://${BASE}${path}`, { ...opts, headers: { "content-type": "application/json", "X-Zb-Test-Ip": opts.ip || `10.9.${(Math.random() * 250) | 0}.${(Math.random() * 250) | 0}`, ...(opts.headers || {}) } });
const post = (path, body, ip) => http(path, { method: "POST", body: JSON.stringify(body), ip });
const board = async (id, hidden = 1) => { const j = await (await http(`/api/leaderboard${hidden ? "?hidden=1" : ""}`)).json(); return j.boards.find((b) => b.id === id).rows; };

function frame(...bodies) {
  let n = 0; for (const b of bodies) n += 2 + b.length;
  const f = new Uint8Array(n); let o = 0;
  for (const b of bodies) { f[o++] = b.length & 255; f[o++] = b.length >> 8; f.set(b, o); o += b.length; }
  return f;
}
const meta = (wave, started, ended) => new Uint8Array([16, wave & 255, wave >> 8, (started ? 1 : 0) | (ended ? 2 : 0), 1, 0]);
const score = (rules, wave, list) => {
  const a = [20, rules, wave & 255, wave >> 8, list.length];
  for (const [n, k] of list) { const b = enc.encode(n); a.push(k & 255, k >> 8, b.length, ...b); }
  return new Uint8Array(a);
};
class Sock {
  constructor(path) {
    this.msgs = [];
    this.ws = new WebSocket(`ws://${BASE}${path}`, { headers: { "X-Zb-Test-Ip": `10.8.${(Math.random() * 250) | 0}.${(Math.random() * 250) | 0}` } });
    this.ws.binaryType = "arraybuffer";
    this.ws.on("message", (d, bin) => { if (!bin) return; const f = new Uint8Array(d); let o = 0; while (o + 2 <= f.length) { const l = f[o] | (f[o + 1] << 8); o += 2; this.msgs.push(f.slice(o, o + l)); o += l; } });
    this.opened = new Promise((res, rej) => { this.ws.on("open", res); this.ws.on("error", rej); });
  }
  send(...b) { this.ws.send(frame(...b)); }
  async take(op, ms = 5000) { const t0 = Date.now(); for (;;) { const i = this.msgs.findIndex((m) => m[0] === op); if (i >= 0) return this.msgs.splice(i, 1)[0]; if (Date.now() - t0 > ms) throw new Error("timeout op 0x" + op.toString(16)); await sleep(50); } }
}
const result = (m) => ({ board: m[1], today: m[2] | (m[3] << 8), all: m[4] | (m[5] << 8) });
async function host() {
  const h = new Sock(`/ws/host?v=3&name=zbt+room&max=4`); await h.opened;
  const w = await h.take(0x81);
  return { h, code: new TextDecoder().decode(w.subarray(8, 8 + w[7])) };
}

await test("team match: relay records at the end, S_RESULT to everybody, row on the hidden team boards", async () => {
  const a = "zbt-" + rnd(), b = "zbt-" + rnd();
  const { h, code } = await host();
  const c = new Sock(`/ws/room/${code}?v=3`); await c.opened; await c.take(0x81);
  h.send(meta(1, true, false));
  await sleep(300);
  h.send(score(3, 1, [[a, 7], [b, 4]]), meta(1, true, true));
  const rh = result(await h.take(0x96)), rc = result(await c.take(0x96));
  assert.equal(rh.board, 1); assert.deepEqual(rc, rh); assert.ok(rh.today >= 1 && rh.all >= 1);
  const rows = await board("team.all.wave");
  const row = rows.find((r) => r.n === `${a}, ${b}`);
  assert.ok(row, JSON.stringify(rows.slice(0, 3)));
  assert.equal(row.w, 1); assert.equal(row.k, 11); assert.equal(row.d, utcDay(Date.now()));
  assert.ok((await board("team.today.kills")).some((r) => r.n === `${a}, ${b}` && r.k === 11));
  // public boards never show test rows
  const pub = await (await http("/api/leaderboard")).json();
  assert.ok(!JSON.stringify(pub).includes("zbt-"));
  h.ws.close(); c.ws.close();
});

await test("host leaves mid-match: the match is recorded once (solo board for a lone host)", async () => {
  const a = "zbt-" + rnd();
  const { h } = await host();
  h.send(meta(1, true, false));
  await sleep(200);
  h.send(score(3, 1, [[a, 5]]), meta(1, true, false));
  await sleep(200);
  h.ws.close(1000);
  let row; for (let i = 0; i < 30 && !row; i++) { await sleep(200); row = (await board("solo.all.wave")).find((r) => r.n === a); }
  assert.ok(row && row.k === 5 && row.w === 1);
});

await test("Classic rules / impossible wave: not ranked (S_RESULT 255)", async () => {
  for (const [rules, wave] of [[1, 1], [3, 40]]) {
    const a = "zbt-" + rnd();
    const { h } = await host();
    h.send(meta(1, true, false));
    await sleep(200);
    h.send(score(rules, wave, [[a, 5]]), meta(wave, true, true));
    assert.equal(result(await h.take(0x96)).board, 255);
    assert.ok(!(await board("solo.all.wave")).some((r) => r.n === a));
    h.ws.close();
  }
});

await test("a better run replaces the best; a worse one does not", async () => {
  const a = "zbt-" + rnd();
  for (const k of [3, 9, 2]) {
    const { h } = await host();
    h.send(meta(1, true, false)); await sleep(150);
    h.send(score(3, 1, [[a, k]]), meta(1, true, true));
    await h.take(0x96); h.ws.close();
  }
  const rows = (await board("solo.all.wave")).filter((r) => r.n === a);
  assert.equal(rows.length, 1); assert.equal(rows[0].k, 9);
});

await test("solo: /api/run token + /api/score, ranks, single use, checks", async () => {
  const nick = "zbt-" + rnd(), ip = `10.7.${(Math.random() * 250) | 0}.1`;
  const r1 = await post("/api/run", {}, ip);
  assert.equal(r1.status, 200);
  const { t } = await r1.json();
  await sleep(1200);
  const s = await post("/api/score", { t, nick, wave: 1, kills: 6, secs: 1, rules: "std" }, ip);
  const sj = await s.json();
  assert.equal(s.status, 200, JSON.stringify(sj)); assert.ok(sj.ok && sj.board && sj.mode === "solo" && sj.today >= 1 && sj.all >= 1);
  assert.ok((await board("solo.today.wave")).some((r) => r.n === nick && r.k === 6));
  assert.equal((await post("/api/score", { t, nick, wave: 1, kills: 6, secs: 1, rules: "std" }, ip)).status, 409, "token reuse");
  const { t: t2 } = await (await post("/api/run", {}, ip)).json();
  assert.equal((await post("/api/score", { t: t2, nick, wave: 25, kills: 6, secs: 1, rules: "std" }, ip)).status, 422, "wave 25 after 0 s");
  assert.equal((await post("/api/score", { t: t2, nick, wave: 1, kills: 6, secs: 999, rules: "std" }, ip)).status, 422, "match time > real time");
  assert.equal((await post("/api/score", { t: t2 + "0", nick, wave: 1, kills: 1, secs: 1, rules: "std" }, ip)).status, 403, "bad token");
  assert.equal((await post("/api/score", { t: t2, nick: "<b>", wave: 1, kills: 1, secs: 1, rules: "std" }, ip)).status, 400, "bad nick");
  const cl = await (await post("/api/score", { t: t2, nick, wave: 1, kills: 1, secs: 1, rules: "classic" }, ip)).json();
  assert.ok(cl.ok && !cl.board);
  assert.equal((await http("/api/score")).status, 405);
});

await test("solo score rate limit (10 / min per IP)", async () => {
  const ip = `10.6.${(Math.random() * 250) | 0}.2`;
  let last = 0;
  for (let i = 0; i < 11; i++) last = (await post("/api/score", { t: "x" }, ip)).status;
  assert.equal(last, 429);
});

await test("host leaves, match continues (B+): first part recorded for the old team, the continuation separately, timed from the original start", async () => {
  const a = "zbt-" + rnd(), b = "zbt-" + rnd(), td = new TextDecoder();
  const h = new Sock(`/ws/host?v=3&name=zbt+bplus&max=4&tclaim=500`); await h.opened;
  const w = await h.take(0x81); const code = td.decode(w.subarray(8, 8 + w[7]));
  const c = new Sock(`/ws/room/${code}?v=3`); await c.opened;
  const cw = await c.take(0x81); const cid = new DataView(cw.buffer, cw.byteOffset).getInt32(1, true);
  const o = 8 + cw[7], ctok = td.decode(cw.subarray(o + 1, o + 1 + cw[o]));
  h.send(meta(1, true, false));
  await sleep(11000); // wave 3 is plausible after 10 s
  h.send(score(3, 3, [[a, 4], [b, 3]]), meta(3, true, false));
  await sleep(300);
  h.ws.close(1000); // host leaves for good -> part 1 recorded, claim restart
  let row; for (let i = 0; i < 30 && !row; i++) { await sleep(200); row = (await board("team.all.wave")).find((r) => r.n === `${a}, ${b}`); }
  assert.ok(row && row.w === 3 && row.k === 7, "part 1: " + JSON.stringify(row));
  await sleep(300);
  const nh = new Sock(`/ws/host?v=3&claim=${code}.${cid}.${ctok}`); await nh.opened; await nh.take(0x81);
  nh.send(meta(2, false, false)); await sleep(200);
  nh.send(meta(3, true, false)); await sleep(300); // the continuation starts at the old wave right away
  nh.send(score(3, 3, [[b, 6]]), meta(3, true, true));
  const r = result(await nh.take(0x96));
  assert.equal(r.board, 0, "continuation is ranked (would be 'wave too fast' if timed from the claim)");
  assert.ok((await board("solo.all.wave")).some((x) => x.n === b && x.w === 3 && x.k === 6));
  nh.ws.close(); c.ws.close();
});

await test("/api/stats counts games per day", async () => {
  const j = await (await http("/api/stats")).json();
  const d = j.days[utcDay(Date.now())];
  assert.ok(d && d.online.started >= 4 && d.online.finished >= 4 && d.single.started >= 2, JSON.stringify(d));
});

await test("/api/leaderboard is cacheable and well formed", async () => {
  const r = await http("/api/leaderboard");
  assert.match(r.headers.get("cache-control"), /max-age/);
  const j = await r.json();
  assert.equal(j.boards.length, 8);
  assert.deepEqual(j.boards.map((b) => b.id), ["solo.today.wave", "solo.today.kills", "solo.all.wave", "solo.all.kills", "team.today.wave", "team.today.kills", "team.all.wave", "team.all.kills"]);
});

console.log(`${ok} passed`);
process.exit(process.exitCode || 0);
