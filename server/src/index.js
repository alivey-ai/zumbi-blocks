// Zumbi Blocks Worker: static WebGL build + room list + WebSocket relay (Durable Object per room).
export { Room } from "./room.js";
export { Lobby } from "./lobby.js";
import { publicRows, quickplayRows } from "./lobby.js";
import { bumpStats, checkToken, cleanName, implausible, leaderboardResponse, makeToken, recordRun, statsResponse, utcDay } from "./scores.js";
import { creditStats, handleAccount, isTestRequest, protectNames, sessionAccount } from "./accounts.js";

const CODE_ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no I, O, 0, 1
const CODE_RE = /^[A-Z0-9]{3,8}$/;

function newCode() {
  const b = crypto.getRandomValues(new Uint8Array(5));
  let s = "";
  for (const x of b) s += CODE_ALPHABET[x % CODE_ALPHABET.length];
  return s;
}

// ---- rate limits (free plan friendly: counters live in memory / in the Lobby DO, nothing paid) --------------------------------
// Sized for NATs (a school or hackathon: dozens of players behind one IP) and reconnect storms (a Wi-Fi blip makes all of them retry
// at once): the limits are per IP and per minute, rejoins have their own, larger bucket. See docs/MULTIPLAYER.md "Limits".
//   /api/rooms, /api/room, /api/quickplay   240/min   (in this isolate: a request must not cost a Durable Object call just to be refused)
//   /api/ping                               600/min   (the server list probes up to 8 rows twice per 10 s per open list)
//   new rooms (/ws/host)                     20/min and 200/hour   (Lobby DO counter, shared by all isolates)
//   first joins (/ws/room)                   90/min
//   rejoins (/ws/room?rejoin=)              180/min
// A refused WebSocket upgrade is answered with a normal socket that carries REJECT(6) and closes, because a browser cannot read the
// status code of a failed upgrade; the client treats reason 6 as "retry later".
export const LIMITS = {
  api: [240, 60_000], ping: [600, 60_000], create: [20, 60_000], createHour: [200, 3_600_000], join: [90, 60_000], rejoin: [180, 60_000],
  run: [30, 60_000], score: [10, 60_000], // phase 5: solo start tokens / solo results (in this isolate, on top of the api bucket)
};
const local = new Map();
function localHit(key, limit, win) {
  const now = Date.now();
  let r = local.get(key);
  if (!r || now - r.t0 >= win) {
    if (local.size > 5000) for (const [k, v] of local) if (now - v.t0 >= 120_000) local.delete(k);
    if (local.size > 20000) local.clear();
    r = { t0: now, n: 0 }; local.set(key, r);
  }
  return ++r.n <= limit;
}
function clientIp(request, env) {
  // X-Zb-Test-Ip is honoured only when the worker runs with --var ZB_TEST:1 (local tests), never in production.
  if (env.ZB_TEST) { const t = request.headers.get("X-Zb-Test-Ip"); if (t) return t; }
  return request.headers.get("CF-Connecting-IP") || "0.0.0.0";
}
const tooMany = (win) => Response.json({ error: "rate limited" }, { status: 429, headers: { "cache-control": "no-store", "retry-after": String(Math.ceil(win / 1000)) } });
function rejectSocket(reason) {
  const pair = new WebSocketPair();
  const [client, server] = Object.values(pair);
  server.accept();
  server.send(new Uint8Array([2, 0, 0x87, reason]));
  server.close(1008, "rate limited");
  return new Response(null, { status: 101, webSocket: client });
}

// ---- free plan budget (docs/MULTIPLAYER.md "Free plan budget") ---------------------------------------------------------------------
// The Workers Free plan allows 100,000 Durable Object requests a day (incoming WebSocket messages count 20:1), reset at 00:00 UTC. Once it is
// used up every Durable Object call throws "Exceeded allowed volume of requests in Durable Objects free tier." Online play needs the Room / Lobby
// objects, so it stops until the reset; the Worker answers with a clear error (JSON 503 {error: "quota"} / a socket that carries REJECT(11))
// and the game explains it. Single player does not need any Durable Object.
const R_QUOTA = 11;
function isQuota(e) { return /exceeded allowed volume|free tier|daily (request )?limit/i.test(String(e && (e.message || e))); }
function nextUtcMidnight(now = Date.now()) { const d = new Date(now); return Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate() + 1); }
function doError(e) {
  const quota = isQuota(e);
  console.log(quota ? "do-quota" : "do-error", String(e && (e.message || e)).slice(0, 200));
  return Response.json(quota ? { error: "quota", resetAt: nextUtcMidnight() } : { error: "unavailable" }, { status: 503, headers: { "cache-control": "no-store", "retry-after": "60" } });
}

// The room list (Lobby DO) cached at the edge for a few seconds: an open server list polls every 10 s and Quick Play asks once, so most of
// these are answered without a Durable Object request. Local tests (ZB_TEST) bypass it unless they ask for it (header x-zb-cache: 1).
const ROWS_TTL_S = 5;
async function lobbyRows(env, request, ctx) {
  const useCache = typeof caches !== "undefined" && (!env.ZB_TEST || request.headers.get("x-zb-cache") === "1");
  const key = new Request("https://zb-cache.invalid/lobby-rows");
  if (useCache) {
    const hit = await caches.default.match(key).catch(() => null);
    if (hit) return { rows: await hit.json(), cached: true };
  }
  const rows = await env.LOBBY.get(env.LOBBY.idFromName("lobby")).rows();
  if (useCache) ctx.waitUntil(caches.default.put(key, new Response(JSON.stringify(rows), { headers: { "content-type": "application/json", "cache-control": `public, max-age=${ROWS_TTL_S}` } })).catch(() => {}));
  return { rows, cached: false };
}

const TYPES = { ".wasm": "application/wasm", ".js": "application/javascript", ".data": "application/octet-stream", ".json": "application/json" };

async function readJson(request) {
  if (request.method !== "POST") return null;
  const t = await request.text();
  if (t.length > 2048) return null;
  try { const o = JSON.parse(t); return o && typeof o === "object" ? o : null; } catch { return null; }
}
const noStore = { headers: { "cache-control": "no-store" } };

// POST /api/score (offline single player, docs/LEADERBOARD.md): {t: token from /api/run, nick, wave, kills, secs, rules: "std" | "classic"}
async function soloScore(request, env) {
  const b = await readJson(request);
  if (!b) return Response.json({ ok: false, error: "bad request" }, { status: 400, ...noStore });
  const now = Date.now();
  const tok = await checkToken(env, b.t, now);
  if (!tok) return Response.json({ ok: false, error: "bad token" }, { status: 403, ...noStore });
  const wave = Number(b.wave), kills = Number(b.kills), secs = Number(b.secs);
  const elapsed = (now - tok.ms) / 1000;
  const day = utcDay(now);
  if (b.rules !== "std") {
    // Classic rules (or an unknown value) never go on the boards; the game still counts (and so does the profile's stats when logged in)
    const s = await sessionAccount(env, request, now);
    if (s && !implausible({ wave, kills, secs, elapsed, players: 1 })) await creditStats(env, [[s.aid, kills]], wave);
    await bumpStats(env, day, "single", 0, 1, 1);
    return Response.json({ ok: true, board: false, reason: "classic" }, noStore);
  }
  const nick = cleanName(b.nick);
  if (!nick) return Response.json({ ok: false, error: "nickname" }, { status: 400, ...noStore });
  const why = implausible({ wave, kills, secs, elapsed, players: 1 });
  if (why) { console.log("score-rejected", JSON.stringify({ why, wave, kills, secs, elapsed: Math.round(elapsed) })); return Response.json({ ok: false, error: "implausible" }, { status: 422, ...noStore }); }
  // a registered profile name counts only for its owner (session cookie); anybody else is recorded as "name (guest)" (docs/ACCOUNTS.md)
  const s = await sessionAccount(env, request, now);
  const prot = await protectNames(env, [[nick, kills]], s ? { [s.nkey]: s.aid } : {});
  const res = await recordRun(env, { id: "s:" + tok.nonce, players: prot.list, wave, secs, source: "client", now });
  if (!res.ok) return Response.json({ ok: false, error: "already recorded" }, { status: 409, ...noStore });
  await creditStats(env, prot.credit, wave);
  await bumpStats(env, day, "single", 0, 1, 1);
  return Response.json({ ok: true, board: true, mode: res.mode, today: res.today, all: res.all, name: prot.list[0][0] }, noStore);
}

export default {
  async fetch(request, env, ctx) {
    try { return await handle(request, env, ctx); }
    catch (e) {
      // a Durable Object call failed (free plan quota used up, or the object is unavailable): never a bare 1101
      const p = new URL(request.url).pathname;
      if (p.startsWith("/ws/") && request.headers.get("Upgrade") === "websocket") { console.log(isQuota(e) ? "do-quota" : "do-error", String(e && (e.message || e)).slice(0, 200)); return rejectSocket(isQuota(e) ? R_QUOTA : 1); }
      return doError(e);
    }
  },
};

async function handle(request, env, ctx) {
    const url = new URL(request.url);
    const path = url.pathname;
    // local tests only (wrangler dev --var ZB_TEST:1): cookie zbquota=1 makes every Durable Object path fail like a used-up free plan
    if (env.ZB_TEST && /(^|;\s*)zbquota=1/.test(request.headers.get("cookie") || "") && /^\/(ws\/|api\/(rooms|room\/|ping\/|quickplay))/.test(path))
      throw new Error("Exceeded allowed volume of requests in Durable Objects free tier.");

    if (path.startsWith("/api/")) {
      const ip = clientIp(request, env);
      const [lim, win] = path.startsWith("/api/ping/") ? LIMITS.ping : LIMITS.api;
      if (!localHit((path.startsWith("/api/ping/") ? "p:" : "a:") + ip, lim, win)) return tooMany(win);
    }

    // ---- player profiles + cloud save (docs/ACCOUNTS.md) ----
    if (path.startsWith("/api/account/")) {
      const lobby = env.LOBBY.get(env.LOBBY.idFromName("lobby"));
      // cloud saves are frequent: their limit is counted in this isolate (no Durable Object request); the rare ones (signup, login, ...) in the Lobby
      const allow = async (key, [lim, win]) => { if (key.startsWith("asv:")) return localHit(key, lim, win); try { return await lobby.hit(key, lim, win); } catch { return true; } };
      return handleAccount(request, env, ctx, { ip: clientIp(request, env), allow });
    }

    // ---- phase 5: leaderboards, solo results, game counts (docs/LEADERBOARD.md) ----
    if (path === "/api/leaderboard") return leaderboardResponse(request, env, ctx);
    if (path === "/api/stats") {
      // D1 only, but cached for a minute anyway (the web page polls it)
      const key = new Request("https://zb-cache.invalid/stats");
      const hit = typeof caches !== "undefined" && !env.ZB_TEST ? await caches.default.match(key).catch(() => null) : null;
      if (hit) return hit;
      const res = await statsResponse(env);
      if (typeof caches !== "undefined" && !env.ZB_TEST && res.ok) { const body = await res.clone().text(); ctx.waitUntil(caches.default.put(key, new Response(body, { headers: { "content-type": "application/json", "cache-control": "public, max-age=60" } })).catch(() => {})); }
      return res;
    }
    if (path === "/api/run" || path === "/api/score") {
      if (request.method !== "POST") return Response.json({ error: "POST only" }, { status: 405, ...noStore });
      if (!env.DB) return Response.json({ ok: false, error: "no database" }, { status: 503, ...noStore });
      const ip = clientIp(request, env);
      const [lim, win] = path === "/api/run" ? LIMITS.run : LIMITS.score;
      if (!localHit((path === "/api/run" ? "sr:" : "ss:") + ip, lim, win)) return tooMany(win);
      if (path === "/api/score") return soloScore(request, env);
      // a single player match starts (offline): count it, hand out the signed start time that /api/score checks the run against
      await bumpStats(env, utcDay(Date.now()), "single", 1, 0, 0);
      return Response.json({ t: await makeToken(env) }, noStore);
    }

    if (path === "/api/rooms") {
      const { rows, cached } = await lobbyRows(env, request, ctx);
      const rooms = publicRows(rows, isTestRequest(request)); // automated tests (cookie zbtest=1) only ever see their own rooms, everybody else never sees them
      return Response.json(rooms, { headers: { "cache-control": "no-store", "x-zb-cached": cached ? "1" : "0" } });
    }

    if (path.startsWith("/api/room/") || path.startsWith("/api/ping/")) {
      const code = decodeURIComponent(path.slice("/api/room/".length)).toUpperCase();
      if (!CODE_RE.test(code)) return Response.json({ exists: false }, { status: 404, headers: { "cache-control": "no-store" } });
      const stub = env.ROOMS.get(env.ROOMS.idFromName(code));
      if (path.startsWith("/api/ping/")) { await stub.ping(); return Response.json({ ok: 1 }, { headers: { "cache-control": "no-store" } }); }
      const info = await stub.info();
      return Response.json(info, { status: info.exists ? 200 : 404, headers: { "cache-control": "no-store" } });
    }

    if (path === "/api/quickplay") {
      const cf = request.cf || {};
      const { rows, cached } = await lobbyRows(env, request, ctx);
      const rooms = quickplayRows(rows, url.searchParams.get("type") || "", Number(cf.latitude) || 0, Number(cf.longitude) || 0, isTestRequest(request));
      return Response.json(rooms, { headers: { "cache-control": "no-store", "x-zb-cached": cached ? "1" : "0" } });
    }

    if (path === "/ws/host" || path.startsWith("/ws/room/")) {
      if (request.headers.get("Upgrade") !== "websocket") return new Response("Expected WebSocket", { status: 426 });
      const ip = clientIp(request, env);
      const rejoin = path.startsWith("/ws/room/") && url.searchParams.has("rejoin");
      const lobby = env.LOBBY.get(env.LOBBY.idFromName("lobby"));
      const allow = async (key, [lim, win]) => { try { return await lobby.hit(key, lim, win); } catch { return true; } }; // fail open
      if (path === "/ws/host") {
        if (!(await allow("c:" + ip, LIMITS.create)) || !(await allow("ch:" + ip, LIMITS.createHour))) return rejectSocket(6);
      } else if (!(await allow((rejoin ? "r:" : "j:") + ip, rejoin ? LIMITS.rejoin : LIMITS.join))) return rejectSocket(6);
      // profiles: a socket that joins with a session cookie carries its account (checked here, never taken from the client) so the relay
      // can tie leaderboard names to accounts; resumes and claims are members already. Test pages (cookie zbtest=1) make hidden test rooms.
      const acct = url.searchParams.has("rejoin") || url.searchParams.has("claim") ? null : await sessionAccount(env, request).catch(() => null);
      const test = isTestRequest(request);
      const fwd = (code, host) => {
        const stub = env.ROOMS.get(env.ROOMS.idFromName(code));
        const u = new URL(request.url);
        u.searchParams.set("code", code);
        u.searchParams.set("host", host ? "1" : "0");
        u.searchParams.set("ipid", ip);
        u.searchParams.delete("acct"); u.searchParams.delete("test");
        if (acct) u.searchParams.set("acct", acct.aid + ":" + acct.nkey);
        if (test) u.searchParams.set("test", "1");
        // where the host is (server side only: the room list publishes colo + country, never lat/lon)
        const cf = request.cf || {};
        for (const [k, v] of [["colo", cf.colo], ["country", cf.country], ["lat", cf.latitude], ["lon", cf.longitude]]) { u.searchParams.delete(k); if (host && v != null) u.searchParams.set(k, String(v)); }
        return stub.fetch(new Request(u, request));
      };
      if (path === "/ws/host") {
        const claim = url.searchParams.get("claim"); // "CODE.ID.TOKEN": a remaining player takes over the room after the host left (phase 4, level B)
        if (claim) {
          const ccode = claim.split(".")[0].toUpperCase();
          if (!CODE_RE.test(ccode)) return new Response("Bad room code", { status: 400 });
          return fwd(ccode, true);
        }
        for (let i = 0; i < 6; i++) {
          const res = await fwd(newCode(), true);
          if (res.status !== 409) return res;
        }
        return new Response("Could not allocate a room code", { status: 503 });
      }
      const code = decodeURIComponent(path.slice("/ws/room/".length)).toUpperCase().replace(/\s+/g, "");
      if (!CODE_RE.test(code)) return new Response("Bad room code", { status: 400 });
      return fwd(code, false);
    }

    if (path.startsWith("/Build/")) {
      // Unity build is gzip compressed (*.gz; the raw wasm exceeds the 25 MiB asset limit). Static assets would serve
      // those as opaque files, so add the Content-Encoding and the type of the uncompressed file.
      const res = await env.ASSETS.fetch(request);
      const enc = path.endsWith(".gz") ? "gzip" : path.endsWith(".br") ? "br" : null;
      if (enc && res.ok) {
        const inner = path.slice(0, path.lastIndexOf("."));
        const ext = inner.slice(inner.lastIndexOf("."));
        const h = new Headers(res.headers);
        h.set("Content-Encoding", enc);
        h.set("Content-Type", TYPES[ext] || "application/octet-stream");
        h.set("Cache-Control", "public, max-age=0, must-revalidate");
        // encodeBody "manual": the body is already encoded, the runtime must not compress it again.
        return new Response(res.body, { status: res.status, headers: h, encodeBody: "manual" });
      }
      return res;
    }

    return env.ASSETS.fetch(request);
}
