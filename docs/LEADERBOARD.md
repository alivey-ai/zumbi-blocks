# Leaderboards, game counts, visitor analytics (phase 5)

All free plan: Cloudflare D1 (database `zumbi-blocks` on Vito's account, binding `DB`), Cloudflare Web Analytics. D1's free limits fail closed (a refused write loses one result, nothing is billed).

## Boards
Eight boards: **Solo / Team** (2+ players in the match) x **Today (UTC) / All time** x **Best wave / Most kills**, top 20 each. One row per player (solo) or per team (the same set of names, any order), holding that name's best run: best wave (ties: more kills) and, separately, most kills. Team kills = the sum of the players' kills.

Only matches with the **default rules** are ranked: Timed waves + More loot (Quick Play always uses them). Classic matches (either option on Classic) are counted in the game stats but never ranked.

Where to see them:
* in the game: main menu > **Leaderboard** (it takes the original's grey, never implemented "Credits" row; the credits are on the web page). Switches Solo/Team, Today/All time, Wave/Kills; your own nickname is green.
* the web page: a Leaderboard panel under the game (also shown on phones, where the game itself is gated).
* the end-of-match screen: "Leaderboard (solo|team, best wave): #N today, #M all time" for the run that just ended (rank of this run's wave among the best rows, counting your own earlier best too), plus a Leaderboard button.

Nicknames: the game's own rule (2-10 letters / digits / space `_ - .`, not "player"). Single player now asks for the nickname before "Play alone" too (once per profile, like multiplayer), because it is shown publicly.

## How results are recorded
**Multiplayer (server side).** The Room Durable Object already knows the wave (`C_META` from the host) and sees `started` / `ended`. When `started` first goes true it notes the start time (server clock). The host sends `C_SCORE` (rules, wave, every player's name + kills; op 20, docs/MULTIPLAYER.md) on each new wave, every 15 s and right before the `C_META` that ends the match. The relay records **once per match** (`r:CODE:startMs` is the row id): when the match ends (everybody dead), when the host leaves for good (before the claim restart), or when the room closes. Then it sends `S_RESULT` (0x96) with the ranks to everybody still connected. A lone host in an online room counts as Solo.

**Single player (offline, client side).** Play alone runs without any server. When the match starts the game calls `POST /api/run` and gets a signed start token (`ms.nonce.hmac`, HMAC-SHA256 with the `ZB_SECRET` wrangler secret). When the match ends (or is left via ESC > Disconnect) it posts `POST /api/score {t, nick, wave, kills, secs, rules}`; the answer carries the ranks. No token (offline, blocked) = no ranking.

Writes per match: 1 `runs` row + 2 `best` upserts (all time, today) + 1 `stats` upsert; a solo start adds 1 `stats` upsert.

## Anti-cheat (what it does and does not do)
A browser game can be faked; this only makes it not trivial.
* Wave vs real time: wave <= 1 + elapsed / 5 s, with elapsed = server time since the match started (relay) or since the token was issued (solo).
* Kills vs wave: kills <= every zombie that can have spawned by that wave (Timed waves: (15 + w - 1) x (1 + 0.5 x players) per wave, one extra player of slack, + 30), and kills <= 3 per second per player.
* Solo match time <= real time since the token + 30 s; tokens are single use (the run id is the nonce) and expire after 12 h.
* Rate limits per IP (in the isolate, on top of the 240/min `/api/*` bucket): `/api/run` 30/min, `/api/score` 10/min.
* Names are re-validated on the server; an invalid name makes a solo run unranked; in a team it shows as "anon" (the team is dropped if no name is valid).
* Not covered: a modified host or client that reports a plausible but false wave / kills. Rejections are logged (`score-rejected`, `match-not-ranked` in the Worker logs). Removing a row by hand: `npx wrangler d1 execute zumbi-blocks --remote --command "DELETE FROM best WHERE nkey = 'name'"` (and the same for `runs`).

## Reading
`GET /api/leaderboard` -> `{day, boards: [{id: "solo.today.wave", rows: [{n, w, k, d}]}, ...]}` (8 boards, ids `solo|team` . `today|all` . `wave|kills`). Cached in the isolate for 30 s and in the edge cache for 60 s (custom domain; workers.dev has no edge cache); one cache miss = one D1 batch of 8 indexed `LIMIT 20` queries.

**Test rows.** Names starting with `zbt-` are stored with `hidden = 1` and never appear on the public boards; `GET /api/leaderboard?hidden=1` shows only them (uncached). All automated tests, also against the live site, use such names. Cleaning them up: `npx wrangler d1 execute zumbi-blocks --remote --command "DELETE FROM runs WHERE hidden = 1; DELETE FROM best WHERE hidden = 1"`.

## Game counts
`GET /api/stats` (unlisted, uncached): games per UTC day for the last 30 days, `mode` `single` (offline single player) / `online` (relay rooms, any player count), `started`, `finished` (ended or left after starting), `players` (player slots of finished games, not unique people). Any rules, Classic included.

## Visitor analytics
Cloudflare Web Analytics, site `zumbi-blocks-classic.vitosenic.com` (site tag `c20dd802882040d9bdd2171419040aee`, manual snippet; the existing automatic setup of the `vitosenic.com` zone does not reach the Worker's page). The page loads `static.cloudflareinsights.com/beacon.min.js` with the site token, except on localhost and in automated browsers (`navigator.webdriver`, i.e. the e2e tests). Cookieless, no personal data. Dashboard: dash.cloudflare.com > Vito.senic@gmail.com's Account > **Analytics & Logs > Web Analytics** > zumbi-blocks-classic.vitosenic.com (visits, page views, countries, browsers per day). The workers.dev address reports into the same site when visited.

## Schema and deploy
`server/migrations/0001_leaderboard.sql` (`runs`, `best`, `stats`). Apply with `npx wrangler d1 migrations apply zumbi-blocks --remote` (production) / `--local` (wrangler dev; `npm run dev:test` clears the local state, applies them and starts `wrangler dev --port 8799 --var ZB_TEST:1`).

## Tests
* `server/test/scores.test.mjs`: unit (`--unit`: nickname rule, plausibility, token) and integration against a running Worker (team match recorded at the end with S_RESULT to both, host leaving mid-match, Classic and impossible wave unranked, best keeps the better run, solo token / score / reuse / checks, rate limit, stats, board shape and caching headers).
* `server/test/e2e/leaderboard.mjs`: the real game: nickname prompt before Play alone, a solo run with rank on the end screen and the row on the solo board, the Leaderboard screen from the main menu, the web panel, a host + client match in an unlisted room (joined by code) recorded by the relay with the host's per-player kills and the rank on both screens.
