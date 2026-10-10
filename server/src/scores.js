// Phase 5: leaderboards and game counts in D1 (docs/LEADERBOARD.md).
// Multiplayer results are recorded by the Room Durable Object (the relay knows when the match started and what wave the host reported);
// offline single player runs post their result to /api/score with a signed start token from /api/run. Both end up in recordRun().
// Free plan budget: one match writes 4 rows (runs, best all-time, best today, stats); reads are 8 indexed LIMIT 20 queries per cache miss.

export const BOARD_SIZE = 20;
export const MIN_WAVE_SECS = 5;      // a wave cannot be cleared faster than this on average (Timed waves: at most 120 s, usually 30-90 s)
export const KILLS_PER_SEC = 3;      // per player, averaged over the match
const TOKEN_MAX_AGE_MS = 12 * 3600_000;
const enc = new TextEncoder();

export const utcDay = (ms) => new Date(ms).toISOString().slice(0, 10);

// The game's nickname rule (ZbMenu.CleanNick): letters, digits, space _ - . ; 2..10 characters, not "player".
export function cleanName(s) {
  if (typeof s !== "string") return null;
  s = s.normalize("NFC").replace(/\s+/g, " ").trim();
  if (!/^[\p{L}\p{N} _.\-]{2,10}$/u.test(s) || s.toLowerCase() === "player") return null;
  return s;
}
// Test runs (local and live e2e) use names starting with "zbt-": stored with hidden = 1, never on the public boards.
export const isTestName = (n) => /^zbt-/i.test(n);

// Zombies the host can have spawned up to `wave` (Timed waves: (15 + w - 1) x (1 + 0.5 x (players - 1)) at the start of wave w, nothing in
// between; Classic spawns fewer). One extra player of slack for players that left before the end, plus a constant.
export function spawnBound(wave, players) {
  const f = 1 + 0.5 * Math.max(1, players); // = the factor for players + 1
  let n = 30;
  for (let w = 1; w <= wave; w++) n += Math.round((14 + w) * f);
  return n;
}

// null = plausible, otherwise the reason. elapsed = real seconds since the match started (server clock).
export function implausible({ wave, kills, secs, elapsed, players }) {
  if (!Number.isInteger(wave) || wave < 1 || wave > 2000) return "wave";
  if (!Number.isInteger(kills) || kills < 0) return "kills";
  if (!Number.isFinite(elapsed) || elapsed < 0) return "elapsed";
  if (wave > 1 + Math.floor(elapsed / MIN_WAVE_SECS)) return "wave too fast";
  if (kills > spawnBound(wave, players)) return "kills vs wave";
  if (kills > (elapsed + 10) * KILLS_PER_SEC * Math.max(1, players)) return "kills vs time";
  if (Number.isFinite(secs) && secs > elapsed + 30) return "match time vs real time";
  return null;
}

// ---- solo start token: "<ms>.<nonce>.<sig>", HMAC-SHA256 with ZB_SECRET (a wrangler secret in production) -----------------------------
async function hmac(env, text) {
  const key = await crypto.subtle.importKey("raw", enc.encode(env.ZB_SECRET || "zb-dev-secret"), { name: "HMAC", hash: "SHA-256" }, false, ["sign"]);
  const sig = await crypto.subtle.sign("HMAC", key, enc.encode(text));
  return [...new Uint8Array(sig).slice(0, 16)].map((x) => x.toString(16).padStart(2, "0")).join("");
}
export async function makeToken(env, now = Date.now()) {
  const nonce = [...crypto.getRandomValues(new Uint8Array(9))].map((x) => x.toString(16).padStart(2, "0")).join("");
  return `${now}.${nonce}.${await hmac(env, `${now}.${nonce}`)}`;
}
// -> {ms, nonce} or null
export async function checkToken(env, tok, now = Date.now()) {
  if (typeof tok !== "string" || tok.length > 80) return null;
  const m = /^(\d{13})\.([0-9a-f]{18})\.([0-9a-f]{32})$/.exec(tok);
  if (!m) return null;
  const ms = Number(m[1]);
  if (now - ms < 0 || now - ms > TOKEN_MAX_AGE_MS) return null;
  if ((await hmac(env, `${m[1]}.${m[2]}`)) !== m[3]) return null;
  return { ms, nonce: m[2] };
}

// ---- writes ----------------------------------------------------------------------------------------------------------------------------
export function bumpStats(env, day, mode, started, finished, players) {
  if (!env.DB) return Promise.resolve();
  return env.DB.prepare(
    "INSERT INTO stats(day, mode, started, finished, players) VALUES(?1, ?2, ?3, ?4, ?5) ON CONFLICT(day, mode) DO UPDATE SET started = started + ?3, finished = finished + ?4, players = players + ?5"
  ).bind(day, mode, started, finished, players).run();
}

const UPSERT_BEST = `INSERT INTO best(mode, period, nkey, names, hidden, wave, wave_kills, wave_at, kills, kills_wave, kills_at)
VALUES(?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?7, ?6, ?8)
ON CONFLICT(mode, period, nkey) DO UPDATE SET
  names = excluded.names,
  wave_kills = CASE WHEN excluded.wave > best.wave OR (excluded.wave = best.wave AND excluded.wave_kills > best.wave_kills) THEN excluded.wave_kills ELSE best.wave_kills END,
  wave_at = CASE WHEN excluded.wave > best.wave OR (excluded.wave = best.wave AND excluded.wave_kills > best.wave_kills) THEN excluded.wave_at ELSE best.wave_at END,
  wave = MAX(best.wave, excluded.wave),
  kills_wave = CASE WHEN excluded.kills > best.kills OR (excluded.kills = best.kills AND excluded.kills_wave > best.kills_wave) THEN excluded.kills_wave ELSE best.kills_wave END,
  kills_at = CASE WHEN excluded.kills > best.kills OR (excluded.kills = best.kills AND excluded.kills_wave > best.kills_wave) THEN excluded.kills_at ELSE best.kills_at END,
  kills = MAX(best.kills, excluded.kills)`;
const RANK = "SELECT COUNT(*) + 1 AS r FROM best WHERE mode = ?1 AND period = ?2 AND hidden = ?3 AND (wave > ?4 OR (wave = ?4 AND wave_kills > ?5))";

// run: {id, players: [[name, kills], ...] (cleaned names), wave, secs, source, now}. Returns {ok, mode, today, all} (ranks of this run's wave
// on the best-wave boards) or {ok: false, dup: true} when that match / token was already recorded.
export async function recordRun(env, run) {
  const now = run.now || Date.now();
  const list = run.players.slice(0, 16);
  const mode = list.length >= 2 ? "team" : "solo";
  const kills = list.reduce((a, p) => a + p[1], 0);
  const names = list.map((p) => p[0]).join(", ").slice(0, 200);
  const nkey = list.map((p) => p[0].toLowerCase()).sort().join(",").slice(0, 200);
  const hidden = list.some((p) => isTestName(p[0])) ? 1 : 0;
  const day = utcDay(now), at = Math.floor(now / 1000);
  const ins = await env.DB.prepare(
    "INSERT INTO runs(id, created, day, mode, names, nkey, players, wave, kills, secs, detail, source, hidden) VALUES(?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12, ?13) ON CONFLICT(id) DO NOTHING"
  ).bind(run.id, at, day, mode, names, nkey, list.length, run.wave, kills, Math.max(0, run.secs | 0), mode === "team" ? JSON.stringify(list) : null, run.source, hidden).run();
  if (!ins.meta || !ins.meta.changes) return { ok: false, dup: true };
  const up = env.DB.prepare(UPSERT_BEST), rank = env.DB.prepare(RANK);
  const res = await env.DB.batch([
    up.bind(mode, "all", nkey, names, hidden, run.wave, kills, at),
    up.bind(mode, day, nkey, names, hidden, run.wave, kills, at),
    rank.bind(mode, day, hidden, run.wave, kills),
    rank.bind(mode, "all", hidden, run.wave, kills),
  ]);
  lbMemo.clear();
  return { ok: true, mode, today: res[2].results[0].r, all: res[3].results[0].r, hidden };
}

// ---- reads -----------------------------------------------------------------------------------------------------------------------------
const lbMemo = new Map(); // per isolate: hidden flag -> {t, body}
export const LB_MEMO_MS = 30_000, LB_EDGE_S = 60;

export async function leaderboard(env, hidden = 0, now = Date.now()) {
  const day = utcDay(now);
  const qWave = env.DB.prepare(`SELECT names AS n, wave AS w, wave_kills AS k, wave_at AS t FROM best WHERE mode = ?1 AND period = ?2 AND hidden = ?3 ORDER BY wave DESC, wave_kills DESC, wave_at ASC LIMIT ${BOARD_SIZE}`);
  const qKills = env.DB.prepare(`SELECT names AS n, kills_wave AS w, kills AS k, kills_at AS t FROM best WHERE mode = ?1 AND period = ?2 AND hidden = ?3 ORDER BY kills DESC, kills_wave DESC, kills_at ASC LIMIT ${BOARD_SIZE}`);
  const ids = [], stmts = [];
  for (const mode of ["solo", "team"]) for (const [pname, period] of [["today", day], ["all", "all"]]) for (const [metric, q] of [["wave", qWave], ["kills", qKills]]) {
    ids.push(`${mode}.${pname}.${metric}`); stmts.push(q.bind(mode, period, hidden));
  }
  const res = await env.DB.batch(stmts);
  // shape kept JsonUtility friendly (the game parses it): {day, boards: [{id, rows: [{n, w, k, d}]}]}
  return { day, boards: ids.map((id, i) => ({ id, rows: res[i].results.map((r) => ({ n: r.n, w: r.w, k: r.k, d: utcDay(r.t * 1000) })) })) };
}

// GET /api/leaderboard: per isolate memo (30 s), then the edge cache (60 s, custom domain only; a no-op on workers.dev), then D1.
// ?hidden=1 shows the test rows instead, never cached (tests read their own fresh results).
export async function leaderboardResponse(request, env, ctx) {
  const url = new URL(request.url);
  const hidden = url.searchParams.get("hidden") === "1" ? 1 : 0;
  const json = (body, cc) => new Response(body, { headers: { "content-type": "application/json; charset=utf-8", "cache-control": cc } });
  if (!env.DB) return Response.json({ error: "no database" }, { status: 503 });
  if (hidden) return json(JSON.stringify(await leaderboard(env, 1)), "no-store");
  const now = Date.now();
  const m = lbMemo.get(0);
  if (m && now - m.t < LB_MEMO_MS) return json(m.body, `public, max-age=${LB_EDGE_S / 2}`);
  const cache = typeof caches !== "undefined" ? caches.default : null;
  const key = new Request(url.origin + "/api/leaderboard?v=1");
  if (cache) {
    const hit = await cache.match(key).catch(() => null);
    if (hit) { const body = await hit.text(); lbMemo.set(0, { t: now, body }); return json(body, `public, max-age=${LB_EDGE_S / 2}`); }
  }
  const body = JSON.stringify(await leaderboard(env, 0, now));
  lbMemo.set(0, { t: now, body });
  if (cache) {
    const put = cache.put(key, new Response(body, { headers: { "content-type": "application/json", "cache-control": `public, max-age=${LB_EDGE_S}` } })).catch(() => {});
    if (ctx && ctx.waitUntil) ctx.waitUntil(put);
  }
  return json(body, `public, max-age=${LB_EDGE_S / 2}`);
}

// GET /api/stats (unlisted): games per day for the last 30 days.
export async function statsResponse(env) {
  if (!env.DB) return Response.json({ error: "no database" }, { status: 503 });
  const since = utcDay(Date.now() - 29 * 86400_000);
  const { results } = await env.DB.prepare("SELECT day, mode, started, finished, players FROM stats WHERE day >= ?1 ORDER BY day DESC, mode").bind(since).all();
  const days = {};
  for (const r of results) (days[r.day] ||= {})[r.mode] = { started: r.started, finished: r.finished, players: r.players };
  const tot = { started: 0, finished: 0, players: 0 };
  for (const r of results) { tot.started += r.started; tot.finished += r.finished; tot.players += r.players; }
  return Response.json({ since, total30d: tot, days }, { headers: { "cache-control": "no-store" } });
}
