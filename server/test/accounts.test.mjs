// Player profile tests (docs/ACCOUNTS.md).
//   unit:        node test/accounts.test.mjs --unit             (no server needed)
//   integration: npm run dev:test (wrangler dev on :8799 with ZB_TEST and a fresh local D1), then BASE=localhost:8799 node test/accounts.test.mjs
// Every profile name starts with "zbt-" (hidden test profiles) and every profile made here is deleted again at the end.
import WebSocket from "ws";
import assert from "node:assert/strict";
import { pbkdf2, same, newRecoveryCode, normCode, validPassword, protectNames, PBKDF2_ITER } from "../src/accounts.js";

const BASE = process.env.BASE || "localhost:8787";
const enc = new TextEncoder();
let ok = 0;
const test = async (name, fn) => { try { await fn(); ok++; console.log("ok   " + name); } catch (e) { console.log("FAIL " + name + "\n" + e.stack); process.exitCode = 1; } };
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const rnd = () => Math.random().toString(36).slice(2, 8);

// ---- unit ------------------------------------------------------------------------------------------------------------------------------
await test("password hashing: PBKDF2-SHA256, per-profile salt, deterministic, cost measured", async () => {
  const s1 = "00112233445566778899aabbccddeeff", s2 = "ffeeddccbbaa99887766554433221100";
  const a = await pbkdf2("hunter22", s1, 1000), b = await pbkdf2("hunter22", s1, 1000), c = await pbkdf2("hunter22", s2, 1000), d = await pbkdf2("hunter23", s1, 1000);
  assert.equal(a.length, 64); assert.equal(a, b); assert.notEqual(a, c); assert.notEqual(a, d);
  const t0 = performance.now(); for (let i = 0; i < 5; i++) await pbkdf2("hunter22", s1, PBKDF2_ITER);
  console.log(`     PBKDF2 ${PBKDF2_ITER} iterations: ${((performance.now() - t0) / 5).toFixed(2)} ms (node, this machine)`);
  assert.ok(same(a, b) && !same(a, c) && !same(a, a.slice(1)) && !same(a, null));
});
await test("recovery code: 16 symbols (80 bits), grouped, normalised for typing", () => {
  const c = newRecoveryCode();
  assert.match(c, /^[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}$/);
  assert.notEqual(c, newRecoveryCode());
  assert.equal(normCode(" " + c.toLowerCase().replace(/-/g, " ") + " "), c.replace(/-/g, ""));
  assert.ok(validPassword("abcdef") && !validPassword("abcde") && !validPassword("x".repeat(73)) && !validPassword(123456));
});
await test("protectNames: a registered name counts only for its verified owner", async () => {
  const env = { DB: { prepare: () => ({ bind: () => ({ all: async () => ({ results: [{ id: 7, nkey: "ana" }, { id: 9, nkey: "bo" }] }) }) }) } };
  const r = await protectNames(env, [["Ana", 3], ["Bo", 2], ["Cy", 1]], { ana: 7, bo: 8 });
  assert.deepEqual(r.list, [["Ana", 3], ["Bo (guest)", 2], ["Cy", 1]]);
  assert.deepEqual(r.credit, [[7, 3]]);
  const r2 = await protectNames(env, [["Ana", 3], ["ana", 1]], { ana: 7 }); // the same name twice: only one of them is the owner
  assert.deepEqual(r2.list, [["Ana", 3], ["ana (guest)", 1]]);
});
if (process.argv.includes("--unit")) { console.log(`${ok} passed`); process.exit(process.exitCode || 0); }

// ---- integration (running worker) ------------------------------------------------------------------------------------------------------
const rip = () => `10.7.${(Math.random() * 250) | 0}.${(Math.random() * 250) | 0}`;
// a tiny cookie jar per "browser"
class Browser {
  constructor() { this.cookie = ""; this.ip = rip(); }
  async req(path, { method = "GET", body, ip, headers = {} } = {}) {
    const r = await fetch(`http://${BASE}${path}`, { method, body: body === undefined ? undefined : JSON.stringify(body),
      headers: { "content-type": "application/json", "X-Zb-Test-Ip": ip || this.ip, ...(this.cookie ? { cookie: this.cookie } : {}), ...headers } });
    const sc = r.headers.get("set-cookie");
    if (sc) { const m = /^zbs=([^;]*)/.exec(sc); if (m) this.cookie = m[1] ? "zbs=" + m[1] : ""; }
    let j = null; try { j = await r.json(); } catch {}
    return { status: r.status, j, setCookie: sc };
  }
  post(path, body, opts = {}) { return this.req(path, { ...opts, method: "POST", body }); }
}
const made = []; // [browser, password] to delete at the end
async function signup(name = "zbt-" + rnd(), password = "pw-" + rnd(), data) {
  const b = new Browser();
  const r = await b.post("/api/account/signup", { name, password, data });
  assert.equal(r.status, 200, JSON.stringify(r.j));
  made.push([b, password]);
  return { b, name, password, r };
}

await test("signup: cookie session (httpOnly), recovery code once, profile + /me", async () => {
  const { b, name, r } = await signup(undefined, undefined, { money: 50, inv: [1, -1] });
  assert.match(r.setCookie, /zbs=[^;]+; Path=\/; Max-Age=7776000; HttpOnly; SameSite=Lax/);
  assert.match(r.j.recovery, /^[A-Z2-9]{4}(-[A-Z2-9]{4}){3}$/);
  assert.equal(r.j.name, name); assert.equal(r.j.rev, 1); assert.deepEqual(r.j.data, { money: 50, inv: [1, -1] });
  const me = await b.req("/api/account/me");
  assert.equal(me.status, 200); assert.equal(me.j.name, name); assert.deepEqual(me.j.stats, { bestWave: 0, kills: 0, games: 0 });
  assert.ok(!("recovery" in me.j));
  assert.equal((await new Browser().req("/api/account/me")).status, 401);
});

await test("signup: names unique case-insensitively, game's name rule, password length", async () => {
  const { name } = await signup();
  const b = new Browser();
  assert.equal((await b.post("/api/account/signup", { name: name.toUpperCase(), password: "abcdef" })).status, 409);
  assert.equal((await b.post("/api/account/signup", { name: "x", password: "abcdef" })).status, 400);
  assert.equal((await b.post("/api/account/signup", { name: "player", password: "abcdef" })).status, 400);
  assert.equal((await b.post("/api/account/signup", { name: "zbt-" + rnd(), password: "abc" })).status, 400);
  assert.equal((await b.post("/api/account/signup", { name: "zbt-" + rnd(), password: "abcdef", data: { x: "y".repeat(5000) } })).status, 413);
  assert.equal((await b.post("/api/account/signup", { name: "zbt-" + rnd(), password: "abcdef" }, { headers: { origin: "https://evil.example" } })).status, 403);
});

await test("login / logout: wrong password refused, right one returns the save; logout revokes the session", async () => {
  const { name, password } = await signup(undefined, undefined, { money: 7 });
  const b = new Browser();
  assert.equal((await b.post("/api/account/login", { name, password: password + "x" })).status, 401);
  assert.equal((await b.post("/api/account/login", { name: "zbt-nobody", password })).status, 401);
  const r = await b.post("/api/account/login", { name: name.toUpperCase(), password });
  assert.equal(r.status, 200); assert.equal(r.j.name, name); assert.deepEqual(r.j.data, { money: 7 });
  const cookie = b.cookie;
  assert.equal((await b.req("/api/account/me")).status, 200);
  assert.equal((await b.post("/api/account/logout", {})).status, 200);
  assert.equal(b.cookie, "");
  const old = new Browser(); old.cookie = cookie; // a copied cookie is dead too (server side revocation)
  assert.equal((await old.req("/api/account/me")).status, 401);
});

await test("cloud save: revision check, a stale write is refused with the server copy", async () => {
  const { b, name, password } = await signup(undefined, undefined, { money: 1 });
  const tab2 = new Browser(); await tab2.post("/api/account/login", { name, password });
  let r = await b.post("/api/account/save", { rev: 1, data: { money: 2 } });
  assert.equal(r.status, 200); assert.equal(r.j.rev, 2);
  r = await tab2.post("/api/account/save", { rev: 1, data: { money: 999 } }); // an old tab
  assert.equal(r.status, 409); assert.equal(r.j.rev, 2); assert.deepEqual(r.j.data, { money: 2 });
  r = await tab2.post("/api/account/save", { rev: 2, data: { money: 3 } });  // after refetching
  assert.equal(r.status, 200); assert.equal(r.j.rev, 3);
  assert.deepEqual((await b.req("/api/account/me")).j.data, { money: 3 });
  assert.equal((await new Browser().post("/api/account/save", { rev: 3, data: { money: 4 } })).status, 401);
  assert.equal((await b.post("/api/account/save", { rev: 3, data: { big: "y".repeat(5000) } })).status, 413);
});

await test("recovery: wrong code refused; right code sets a new password, issues a new code, logs out everybody else", async () => {
  const { b, name, password, r: su } = await signup();
  const other = new Browser();
  assert.equal((await other.post("/api/account/recover", { name, code: "AAAA-AAAA-AAAA-AAAA", password: "newpass1" })).status, 401);
  const r = await other.post("/api/account/recover", { name, code: su.j.recovery.toLowerCase().replace(/-/g, " "), password: "newpass1" });
  assert.equal(r.status, 200, JSON.stringify(r.j)); assert.notEqual(r.j.recovery, su.j.recovery);
  assert.equal((await other.req("/api/account/me")).status, 200);           // the recovering browser is logged in
  assert.equal((await b.req("/api/account/me")).status, 401);               // the old session is gone
  assert.equal((await new Browser().post("/api/account/login", { name, password })).status, 401);
  assert.equal((await new Browser().post("/api/account/login", { name, password: "newpass1" })).status, 200);
  assert.equal((await new Browser().post("/api/account/recover", { name, code: su.j.recovery, password: "newpass2" })).status, 401); // old code used up
  made.push([other, "newpass1"]);
});

await test("rate limits: login per name (10 / 10 min, any IP) and signup per IP (10 / h)", async () => {
  const { name } = await signup();
  let codes = [];
  for (let i = 0; i < 11; i++) codes.push((await new Browser().post("/api/account/login", { name, password: "wrong-" + i })).status);
  assert.deepEqual(codes.slice(0, 10), Array(10).fill(401)); assert.equal(codes[10], 429);
  const ip = rip(); codes = [];
  for (let i = 0; i < 11; i++) codes.push((await new Browser().post("/api/account/signup", { name: "x", password: "abcdef" }, { ip })).status);
  assert.deepEqual(codes.slice(0, 10), Array(10).fill(400)); assert.equal(codes[10], 429);
});

// ---- leaderboard name protection --------------------------------------------------------------------------------------------------------
const board = async (id) => { const j = await (await fetch(`http://${BASE}/api/leaderboard?hidden=1`)).json(); return j.boards.find((b) => b.id === id).rows; };
async function soloRun(b, nick, kills) {
  const t = (await b.post("/api/run", {})).j.t;
  return b.post("/api/score", { t, nick, wave: 1, kills, secs: 5, rules: "std" });
}
await test("solo: a guest using a registered name is recorded as 'name (guest)'; the owner gets the row and the stats", async () => {
  const { b, name } = await signup();
  const g = await soloRun(new Browser(), name, 2);
  assert.equal(g.status, 200); assert.equal(g.j.name, name + " (guest)");
  const o = await soloRun(b, name, 3);
  assert.equal(o.status, 200); assert.equal(o.j.name, name);
  const rows = await board("solo.all.wave");
  assert.ok(rows.some((r) => r.n === name + " (guest)" && r.k === 2) && rows.some((r) => r.n === name && r.k === 3), JSON.stringify(rows.slice(0, 5)));
  const st = (await b.req("/api/account/me")).j.stats;
  assert.deepEqual(st, { bestWave: 1, kills: 3, games: 1 });
});

function frame(...bodies) {
  let n = 0; for (const x of bodies) n += 2 + x.length;
  const f = new Uint8Array(n); let o = 0;
  for (const x of bodies) { f[o++] = x.length & 255; f[o++] = x.length >> 8; f.set(x, o); o += x.length; }
  return f;
}
const meta = (wave, started, ended) => new Uint8Array([16, wave & 255, wave >> 8, (started ? 1 : 0) | (ended ? 2 : 0), 1, 0]);
const score = (rules, wave, list) => { const a = [20, rules, wave & 255, wave >> 8, list.length]; for (const [n, k] of list) { const x = enc.encode(n); a.push(k & 255, k >> 8, x.length, ...x); } return new Uint8Array(a); };
class Sock {
  constructor(path, cookie = "") {
    this.msgs = [];
    this.ws = new WebSocket(`ws://${BASE}${path}`, { headers: { "X-Zb-Test-Ip": rip(), ...(cookie ? { cookie } : {}) } });
    this.ws.binaryType = "arraybuffer";
    this.ws.on("message", (d, bin) => { if (!bin) return; const f = new Uint8Array(d); let o = 0; while (o + 2 <= f.length) { const l = f[o] | (f[o + 1] << 8); o += 2; this.msgs.push(f.slice(o, o + l)); o += l; } });
    this.opened = new Promise((res, rej) => { this.ws.on("open", res); this.ws.on("error", rej); });
  }
  send(...x) { this.ws.send(frame(...x)); }
  async take(op, ms = 5000) { const t0 = Date.now(); for (;;) { const i = this.msgs.findIndex((m) => m[0] === op); if (i >= 0) return this.msgs.splice(i, 1)[0]; if (Date.now() - t0 > ms) throw new Error("timeout op 0x" + op.toString(16)); await sleep(50); } }
}
await test("relay: names tied to the sockets' sessions; an impostor shows as '(guest)', the owner gets the stats", async () => {
  const own = await signup(), imp = await signup();
  // host = the owner of imp's name? no: host is a guest who claims own.name, client is logged in as imp
  const h = new Sock(`/ws/host?v=3&name=zbt+acc&max=4`, "zbtest=1"); await h.opened;
  const w = await h.take(0x81); const code = new TextDecoder().decode(w.subarray(8, 8 + w[7]));
  const c = new Sock(`/ws/room/${code}?v=3`, imp.b.cookie); await c.opened; await c.take(0x81);
  h.send(meta(1, true, false)); await sleep(300);
  h.send(score(3, 1, [[own.name, 5], [imp.name, 4]]), meta(1, true, true));
  await h.take(0x96);
  const rows = await board("team.all.wave");
  const want = `${own.name} (guest), ${imp.name}`;
  assert.ok(rows.some((r) => r.n === want && r.k === 9), JSON.stringify(rows.slice(0, 5)));
  assert.deepEqual((await imp.b.req("/api/account/me")).j.stats, { bestWave: 1, kills: 4, games: 1 });
  assert.deepEqual((await own.b.req("/api/account/me")).j.stats, { bestWave: 0, kills: 0, games: 0 });
  h.ws.close(); c.ws.close();
});

await test("test rooms (cookie zbtest=1) are never listed for real players; tests see only test rooms", async () => {
  const t = new Sock(`/ws/host?v=3&name=zbt+hidden&max=4`, "zbtest=1"); await t.opened;
  const w = await t.take(0x81); const code = new TextDecoder().decode(w.subarray(8, 8 + w[7]));
  t.send(new Uint8Array([8, 1, 10, ...enc.encode("zbt hidden"), 0, 0])); // C_LISTING: listed
  await sleep(500);
  const pub = await (await fetch(`http://${BASE}/api/rooms`)).json();
  const mine = await (await fetch(`http://${BASE}/api/rooms`, { headers: { cookie: "zbtest=1" } })).json();
  assert.ok(!pub.some((r) => r.code === code), JSON.stringify(pub));
  assert.ok(mine.some((r) => r.code === code), JSON.stringify(mine));
  assert.ok(mine.every((r) => !("test" in r)));
  t.ws.close();
});

await test("delete profile: password required; the name is free again", async () => {
  const { b, name, password } = await signup();
  assert.equal((await b.post("/api/account/delete", { password: "nope-nope" })).status, 401);
  assert.equal((await b.post("/api/account/delete", { password })).status, 200);
  assert.equal((await b.req("/api/account/me")).status, 401);
  assert.equal((await new Browser().post("/api/account/login", { name, password })).status, 401);
  const again = await new Browser().post("/api/account/signup", { name, password });
  assert.equal(again.status, 200);
  made.push([new Browser(), password]); made[made.length - 1][0].cookie = "zbs=" + /zbs=([^;]*)/.exec(again.setCookie)[1];
});

// clean up: every profile this run made
let left = 0;
for (const [b, pw] of made) { const r = await b.post("/api/account/delete", { password: pw }); if (r.status !== 200 && r.status !== 401) left++; }
console.log(`cleanup: deleted the test profiles${left ? ` (${left} failed)` : ""}`);
console.log(`${ok} passed`);
process.exit(process.exitCode || 0);
