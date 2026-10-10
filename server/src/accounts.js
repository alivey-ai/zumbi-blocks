// Player profiles (docs/ACCOUNTS.md): profile name + password, a one-time recovery code instead of e-mail, revocable sessions in an
// httpOnly cookie, one cloud save row per profile with a revision number (stale writes are refused), small stats, and the leaderboard
// name protection (a registered name can only be used by its owner; anybody else shows as "name (guest)").
// Free plan budget: a save is 1 session read + 1 UPDATE; login / signup 1 PBKDF2 (PBKDF2_ITER, measured to stay well inside the CPU limit).
import { cleanName, isTestName } from "./scores.js";

export const PBKDF2_ITER = 50_000;        // ~4 ms on an M-series core, see docs/ACCOUNTS.md "Password hashing"
export const SESSION_DAYS = 90;
export const SAVE_MAX = 4096;             // bytes of save JSON
export const PW_MIN = 6, PW_MAX = 72;
export const COOKIE = "zbs";
const enc = new TextEncoder();
const CODE_ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // 32 symbols = 5 bits each

const hex = (buf) => [...new Uint8Array(buf)].map((x) => x.toString(16).padStart(2, "0")).join("");
const rand = (n) => crypto.getRandomValues(new Uint8Array(n));
export async function sha256(s) { return hex(await crypto.subtle.digest("SHA-256", enc.encode(s))); }
export async function pbkdf2(password, saltHex, iter) {
  const key = await crypto.subtle.importKey("raw", enc.encode(password), "PBKDF2", false, ["deriveBits"]);
  const salt = new Uint8Array(saltHex.match(/../g).map((h) => parseInt(h, 16)));
  return hex(await crypto.subtle.deriveBits({ name: "PBKDF2", hash: "SHA-256", salt, iterations: iter }, key, 256));
}
// constant time for equal length strings
export function same(a, b) {
  if (typeof a !== "string" || typeof b !== "string" || a.length !== b.length) return false;
  let d = 0;
  for (let i = 0; i < a.length; i++) d |= a.charCodeAt(i) ^ b.charCodeAt(i);
  return d === 0;
}
export function newRecoveryCode() {
  const b = rand(16); let s = "";
  for (let i = 0; i < 16; i++) s += CODE_ALPHABET[b[i] & 31] + (i % 4 === 3 && i < 15 ? "-" : "");
  return s; // XXXX-XXXX-XXXX-XXXX, 80 bits
}
export const normCode = (c) => (typeof c === "string" ? c.toUpperCase().replace(/[^A-Z0-9]/g, "") : "");
export function validPassword(p) { return typeof p === "string" && p.length >= PW_MIN && p.length <= PW_MAX; }

// ---- sessions ------------------------------------------------------------------------------------------------------------------------
function cookieValue(request, name) {
  const c = request.headers.get("Cookie") || "";
  for (const part of c.split(";")) { const i = part.indexOf("="); if (i > 0 && part.slice(0, i).trim() === name) return part.slice(i + 1).trim(); }
  return null;
}
export const isTestRequest = (request) => cookieValue(request, "zbtest") === "1";
function setCookie(request, token, maxAge) {
  const secure = new URL(request.url).protocol === "https:" ? "; Secure" : "";
  return `${COOKIE}=${token}; Path=/; Max-Age=${maxAge}; HttpOnly; SameSite=Lax${secure}`;
}
async function newSession(env, aid, now) {
  const token = btoa(String.fromCharCode(...rand(32))).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
  const at = Math.floor(now / 1000);
  await env.DB.prepare("INSERT INTO sessions(th, aid, created, expires) VALUES(?1, ?2, ?3, ?4)").bind(await sha256(token), aid, at, at + SESSION_DAYS * 86400).run();
  return token;
}
// -> {aid, name, nkey, th} or null. Sliding expiry: renewed to 90 days at most once a day (one small write per active day).
export async function sessionAccount(env, request, now = Date.now()) {
  const tok = cookieValue(request, COOKIE);
  if (!tok || tok.length > 64 || !env.DB) return null;
  const th = await sha256(tok), at = Math.floor(now / 1000);
  const r = await env.DB.prepare("SELECT s.aid, s.expires, a.name, a.nkey FROM sessions s JOIN accounts a ON a.id = s.aid WHERE s.th = ?1").bind(th).first();
  if (!r || r.expires < at) return null;
  if (r.expires - at < (SESSION_DAYS - 1) * 86400) await env.DB.prepare("UPDATE sessions SET expires = ?2 WHERE th = ?1").bind(th, at + SESSION_DAYS * 86400).run();
  return { aid: r.aid, name: r.name, nkey: r.nkey, th };
}

// ---- leaderboard name protection + profile stats (used by /api/score and the Room Durable Object) ------------------------------------
// list: [[cleanName, kills], ...]; verified: {nkey: accountId} of the players whose session the server checked (solo: the request's cookie,
// multiplayer: every socket that joined the room with a session). A registered name used by anybody else becomes "name (guest)".
// Returns {list, credit: [[aid, kills], ...]} (credit = verified profiles in this match).
export async function protectNames(env, list, verified = {}) {
  const keys = [...new Set(list.map(([n]) => n.toLowerCase()))];
  if (!keys.length || !env.DB) return { list, credit: [] };
  const { results } = await env.DB.prepare(`SELECT id, nkey FROM accounts WHERE nkey IN (${keys.map((_, i) => "?" + (i + 1)).join(",")})`).bind(...keys).all();
  const owner = new Map(results.map((r) => [r.nkey, r.id]));
  const credit = [], seen = new Set();
  const out = list.map(([n, k]) => {
    const key = n.toLowerCase(), id = owner.get(key);
    if (id == null) return [n, k];
    if (verified[key] === id && !seen.has(id)) { seen.add(id); credit.push([id, k]); return [n, k]; }
    return [n + " (guest)", k];
  });
  return { list: out, credit };
}
export async function creditStats(env, credit, wave) {
  if (!credit.length || !env.DB) return;
  const st = env.DB.prepare("UPDATE accounts SET games = games + 1, kills = kills + ?2, best_wave = MAX(best_wave, ?3) WHERE id = ?1");
  await env.DB.batch(credit.map(([aid, k]) => st.bind(aid, Math.max(0, k | 0), Math.max(0, wave | 0))));
}

// ---- HTTP: /api/account/* ------------------------------------------------------------------------------------------------------------
const noStore = { "cache-control": "no-store" };
const json = (body, status = 200, extra = {}) => Response.json(body, { status, headers: { ...noStore, ...extra } });
async function body(request) {
  const t = await request.text();
  if (t.length > SAVE_MAX + 1024) return null;
  try { const o = JSON.parse(t || "{}"); return o && typeof o === "object" ? o : null; } catch { return null; }
}
function saveJson(data) {
  if (data == null) return null;
  if (typeof data !== "object" || Array.isArray(data)) return undefined;
  const s = JSON.stringify(data);
  return s.length <= SAVE_MAX ? s : undefined;
}
function profile(row, extra = {}) {
  let data = null;
  try { data = row.data ? JSON.parse(row.data) : null; } catch { data = null; }
  return { ok: true, name: row.name, rev: row.rev, data, stats: { bestWave: row.best_wave, kills: row.kills, games: row.games }, ...extra };
}
const SELECT = "SELECT id, name, nkey, pw_hash, pw_salt, pw_iter, rec_hash, rev, data, best_wave, kills, games FROM accounts";

// limits (per IP via the Lobby DO counter, shared by all isolates; per name the same way). Sized for NATs (a classroom behind one IP).
export const ACCOUNT_LIMITS = {
  signup: [10, 3_600_000], login: [30, 600_000], loginName: [10, 600_000], recover: [10, 3_600_000], recoverName: [5, 3_600_000],
  del: [10, 600_000], save: [30, 60_000],
};

// opts: {ip, allow(key, [limit, windowMs]) -> Promise<bool>}
export async function handleAccount(request, env, ctx, opts) {
  const url = new URL(request.url);
  const op = url.pathname.slice("/api/account/".length);
  if (!env.DB) return json({ ok: false, error: "no database" }, 503);
  const now = Date.now(), at = Math.floor(now / 1000);
  const limited = () => json({ ok: false, error: "rate limited" }, 429, { "retry-after": "60" });
  const { ip, allow } = opts;

  if (op === "me" && request.method === "GET") {
    const s = await sessionAccount(env, request, now);
    if (!s) return json({ ok: false, error: "not logged in" }, 401);
    return json(profile(await env.DB.prepare(SELECT + " WHERE id = ?1").bind(s.aid).first()));
  }
  if (request.method !== "POST") return json({ ok: false, error: "POST only" }, 405);
  // CSRF: the session cookie is SameSite=Lax; also refuse a cross-site Origin outright
  const origin = request.headers.get("Origin");
  if (origin && origin !== url.origin) return json({ ok: false, error: "origin" }, 403);
  const b = await body(request);
  if (!b) return json({ ok: false, error: "bad request" }, 400);

  if (op === "signup") {
    if (!(await allow("as:" + ip, ACCOUNT_LIMITS.signup))) return limited();
    const name = cleanName(b.name);
    if (!name) return json({ ok: false, error: "name" }, 400);
    if (!validPassword(b.password)) return json({ ok: false, error: "password" }, 400);
    const data = saveJson(b.data);
    if (data === undefined) return json({ ok: false, error: "save too big" }, 413);
    const salt = hex(rand(16)), code = newRecoveryCode();
    const pw = await pbkdf2(b.password, salt, PBKDF2_ITER);
    const ins = await env.DB.prepare(
      "INSERT INTO accounts(name, nkey, pw_hash, pw_salt, pw_iter, rec_hash, created, rev, data, updated, hidden) VALUES(?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?7, ?10) ON CONFLICT(nkey) DO NOTHING"
    ).bind(name, name.toLowerCase(), pw, salt, PBKDF2_ITER, await sha256(normCode(code)), at, data ? 1 : 0, data, isTestName(name) ? 1 : 0).run();
    if (!ins.meta || !ins.meta.changes) return json({ ok: false, error: "taken" }, 409);
    const aid = ins.meta.last_row_id;
    const token = await newSession(env, aid, now);
    console.log("account-signup", JSON.stringify({ aid, migrated: !!data }));
    return json(profile(await env.DB.prepare(SELECT + " WHERE id = ?1").bind(aid).first(), { recovery: code }), 200, { "set-cookie": setCookie(request, token, SESSION_DAYS * 86400) });
  }

  if (op === "login") {
    const name = cleanName(b.name);
    if (!(await allow("al:" + ip, ACCOUNT_LIMITS.login))) return limited();
    if (!name || typeof b.password !== "string" || b.password.length > PW_MAX) return json({ ok: false, error: "wrong name or password" }, 401);
    if (!(await allow("aln:" + name.toLowerCase(), ACCOUNT_LIMITS.loginName))) return limited();
    const row = await env.DB.prepare(SELECT + " WHERE nkey = ?1").bind(name.toLowerCase()).first();
    // unknown names cost the same PBKDF2 as known ones (no cheap "does this name exist" oracle beyond signup itself)
    const h = await pbkdf2(b.password, row ? row.pw_salt : "00".repeat(16), row ? row.pw_iter : PBKDF2_ITER);
    if (!row || !same(h, row.pw_hash)) return json({ ok: false, error: "wrong name or password" }, 401);
    if (row.pw_iter < PBKDF2_ITER) { const salt = hex(rand(16)); await env.DB.prepare("UPDATE accounts SET pw_hash = ?2, pw_salt = ?3, pw_iter = ?4 WHERE id = ?1").bind(row.id, await pbkdf2(b.password, salt, PBKDF2_ITER), salt, PBKDF2_ITER).run(); }
    await dropCurrentSession(env, request);
    const token = await newSession(env, row.id, now);
    return json(profile(row), 200, { "set-cookie": setCookie(request, token, SESSION_DAYS * 86400) });
  }

  if (op === "recover") {
    const name = cleanName(b.name);
    if (!(await allow("ar:" + ip, ACCOUNT_LIMITS.recover))) return limited();
    if (!name) return json({ ok: false, error: "wrong name or recovery code" }, 401);
    if (!(await allow("arn:" + name.toLowerCase(), ACCOUNT_LIMITS.recoverName))) return limited();
    if (!validPassword(b.password)) return json({ ok: false, error: "password" }, 400);
    const row = await env.DB.prepare(SELECT + " WHERE nkey = ?1").bind(name.toLowerCase()).first();
    if (!row || !same(await sha256(normCode(b.code)), row.rec_hash)) return json({ ok: false, error: "wrong name or recovery code" }, 401);
    const salt = hex(rand(16)), code = newRecoveryCode();
    await env.DB.batch([
      env.DB.prepare("UPDATE accounts SET pw_hash = ?2, pw_salt = ?3, pw_iter = ?4, rec_hash = ?5 WHERE id = ?1").bind(row.id, await pbkdf2(b.password, salt, PBKDF2_ITER), salt, PBKDF2_ITER, await sha256(normCode(code))),
      env.DB.prepare("DELETE FROM sessions WHERE aid = ?1").bind(row.id), // every other browser is logged out
    ]);
    const token = await newSession(env, row.id, now);
    console.log("account-recover", JSON.stringify({ aid: row.id }));
    return json(profile(row, { recovery: code }), 200, { "set-cookie": setCookie(request, token, SESSION_DAYS * 86400) });
  }

  if (op === "logout") {
    await dropCurrentSession(env, request);
    return json({ ok: true }, 200, { "set-cookie": setCookie(request, "", 0) });
  }

  // everything below needs a session
  const s = await sessionAccount(env, request, now);
  if (!s) return json({ ok: false, error: "not logged in" }, 401, { "set-cookie": setCookie(request, "", 0) });

  if (op === "save") {
    // {rev: the revision this save is based on, data: {...}} -> {rev} | 409 {rev, data} (somebody else wrote first: take the server copy)
    if (!(await allow("asv:" + s.aid, ACCOUNT_LIMITS.save))) return limited();
    const data = saveJson(b.data);
    if (!data) return json({ ok: false, error: data === undefined ? "save too big" : "no data" }, data === undefined ? 413 : 400);
    const base = Number(b.rev);
    if (!Number.isInteger(base) || base < 0) return json({ ok: false, error: "rev" }, 400);
    const r = await env.DB.prepare("UPDATE accounts SET data = ?3, rev = rev + 1, updated = ?4 WHERE id = ?1 AND rev = ?2 RETURNING rev").bind(s.aid, base, data, at).first();
    if (r) return json({ ok: true, rev: r.rev });
    const cur = await env.DB.prepare(SELECT + " WHERE id = ?1").bind(s.aid).first();
    return json({ ...profile(cur), ok: false, error: "stale" }, 409);
  }

  if (op === "delete") {
    if (!(await allow("ad:" + s.aid, ACCOUNT_LIMITS.del)) || !(await allow("adi:" + ip, ACCOUNT_LIMITS.del))) return limited();
    const row = await env.DB.prepare(SELECT + " WHERE id = ?1").bind(s.aid).first();
    if (typeof b.password !== "string" || !row || !same(await pbkdf2(b.password, row.pw_salt, row.pw_iter), row.pw_hash)) return json({ ok: false, error: "wrong password" }, 401);
    await env.DB.batch([env.DB.prepare("DELETE FROM sessions WHERE aid = ?1").bind(s.aid), env.DB.prepare("DELETE FROM accounts WHERE id = ?1").bind(s.aid)]);
    console.log("account-delete", JSON.stringify({ aid: s.aid }));
    return json({ ok: true }, 200, { "set-cookie": setCookie(request, "", 0) });
  }

  return json({ ok: false, error: "unknown" }, 404);
}

async function dropCurrentSession(env, request) {
  const tok = cookieValue(request, COOKIE);
  if (tok && tok.length <= 64) await env.DB.prepare("DELETE FROM sessions WHERE th = ?1").bind(await sha256(tok)).run();
}
