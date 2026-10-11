# Roadmap — from revival to a proper game

Rule: **gameplay stays exactly like the original v0.7.** Everything here is around it: connection quality, getting into games, community.

Live: https://zumbi-blocks-classic.vitosenic.com (also https://zumbi-blocks.vito-senic.workers.dev). Adrian approved public hosting 2026-10-09.

**Cost rule: must stay $0.** Cloudflare Workers Free plan only (free limits fail closed, never bill). Phase 2 (WebRTC) is also the cost fix: the more traffic goes over direct channels, the less the Durable Object receives (first release: host state only, see Phase 2). No paid TURN; strict-NAT players fall back to the WebSocket relay. If it ever outgrows the free quota, the backstop is a flat-price Hetzner VPS relay (~EUR 4-5/mo), not the $5 Cloudflare plan.

## Phase 1 — Feel (done, see docs/MULTIPLAYER.md "Feel layer")
Make online play feel smooth without changing the network architecture.
- [x] **Net stats overlay** (toggle with F3): ping to host, jitter, messages/s, KB/s, packet backlog. Used to measure every later change.
- [x] **Interpolation** for remote players and zombies: render ~100 ms in the past and glide between snapshots instead of snapping.
- [x] **Instant local feedback** for shooting (already how the original works, verified and documented): hit marker / blood / sound shown on the shooter's screen immediately; damage is applied by the shooter's bullet as in the original.
- [x] **Batching**: send all messages from one frame in one WebSocket frame (existed; relay stamps and tests added) + compact state messages (about half the bytes per update) + about 20 Hz state rate.
- [x] **Latency test mode** (`?netsim=RTT,JITTER`): artificial delay/jitter in local tests so we can prove improvements.

## Phase 2 — Direct connection (browser to browser) (first release done, see docs/MULTIPLAYER.md "Data channel lane")
- [x] WebRTC DataChannel between host and each client, unordered / `maxRetransmits 0`, for host to client position updates and echo probes. Everything else (RPC, INST, DESTROY) stays on the WebSocket relay.
- [x] The Durable Object also does signaling (JSON text frames on the existing socket); health checks with hysteresis, automatic fallback to the WebSocket path, `?p2p=0` kill switch, F3 overlay shows DC/WS and RTT per peer.
- [x] Cost: STUN only (free public servers), no TURN, nothing that needs the Workers Paid plan. Peers that cannot connect directly stay on the relay.
- [x] Privacy note (the host sees client IPs and vice versa) on the page and in the README.
- [x] **2b** (2026-10-11, after the free plan quota incident): direct clients send state and RPC / INST / DESTROY to the host over the data channels (a second, reliable channel), the host is their gateway; the relay keeps the buffer and serves peers without a channel. With every client direct a room sends ~2 WebSocket frames/s to the Durable Object instead of 25 (docs/MULTIPLAYER.md "Free plan budget").
- [x] Free plan budget: fixed-rate WebSocket batching (host 15 Hz, clients 10 Hz), keepalive auto-response, fewer pings / lobby calls, edge-cached room list, "online is full for today" screen, `server/scripts/do-usage.mjs`.
- [ ] Real-network verification (home + phone hotspot, different regions) and a week of `p2p-stat` log lines to learn the real direct-connect rate (decides how far the free plan stretches: strict-NAT players cost 10 frames/s each).
- Not planned: TURN (costs money).

## Phase 3 — Getting into a game (done, see docs/MULTIPLAYER.md "Getting into a game")
- [x] Nickname: the original's Name field is used everywhere; asked once (before create / join / Quick Play / invite) while it is still the default.
- [x] **Quick Play** button on the main menu: join the best open room or create a listed one; Quick Play rooms start by themselves after a visible 10 s countdown (5 s once a second player joins), normal rooms still wait for ENTER.
- [x] Better server list: players/max, current wave, ping, region, room name; full rooms marked and sorted last; refresh every 10 s.
- [x] **Invite links**: `.../?room=ABCDE` joins directly (build check, clean errors); copy button on the page while in a room.
- [x] Settings: mouse sensitivity and volume (the original's), FOV, graphics quality (Low / Medium / High), invert Y. Saved locally; also from the in-game pause panel.
- [x] Desktop-only gate for phones and tablets (phase 4; lifted by Phase 5 touch controls); `/api/*` rate limits (phase 4 step 1).

## Phase 4 — Resilience (done except level C, see docs/MULTIPLAYER.md "Resilience (phase 4)")
- [x] **Limits**: per IP rate limits on `/api/*`, new rooms, joins and rejoins (NAT and reconnect-storm sized), per-socket message budget; all in memory / in the Lobby DO, free plan only.
- [x] **Desktop-only gate**: phones and tablets see a message, nothing is downloaded.
- [x] **Reconnect** after a short drop (keep your player for 20 s, host 15 s): resume token, limbo slot, seq replay, count based resend, WebRTC lane comes back. Intentional leave / tab close skips the grace.
- [x] **Host leaves, level A** (approved): players keep the money of the match, clean "host left" screen.
- [x] **Host leaves, level B "claim restart"**: a remaining player takes over the same room code and the group continues in a new match (wave 1) without the menu.
- [x] **Host leaves, level B+ "continue"** (after playtests): the claimed match continues at the wave the old one was on, with the old room's rules; every player gets its own items, health, revives and kills back. Lost: zombies alive, ground loot, constructions, vehicles.
- [ ] **Host migration level C** (state preserving, mid-match): NOT built, see "Why not level C" in MULTIPLAYER.md.
- [x] **Hidden host tab**: host's tab title blinks a warning, clients see "host paused" instead of a silent freeze, hidden tabs are never dropped. Spike: a hidden tab cannot be reproduced with Playwright (see MULTIPLAYER.md), numbers still to be taken by hand in a real browser.
- [x] Rate and message size limits against griefing (first layer; RPC / INST content validators in shadow mode are not done).

## Playtest features (asked for after playtesting; not in v0.7, everything else stays original)
- [x] **Timed waves** (default; host option "Waves: Timed / Classic" on the create-server screen, Quick Play is always Timed): the next wave starts when (almost) all zombies are dead OR 120 s after the wave started, whichever is first; leftover zombies stay. 120 s is the original's own "long wave" timeout, which the original arms only if the previous wave was cleared; Timed arms it for every wave. Classic = exactly the original. The host owns the clock, it is synced to everybody twice a second.
- [x] **Wave number on the HUD** next to the match time ("Wave 3   1:24") and "Next wave in 1:50" below it.
- [x] **Teammates' health**: name + health bar list at the left edge and a small bar above teammates' heads; each player sends its own health, "down" for dead players, removed when they leave.
- [x] **More loot**: host option "Loot: More / Classic" (default More): item interval 10 / 7 / 5 / 4 s and cap 40 / 50 / 60 / 70-80 by player count, 50 % real items, 10 % zombie backpacks, grenades in the random map loot. Classic = original numbers.
- [x] **Zombies per wave scale with the players** (Timed only): x1 / x1.5 / x2 / x2.5 for 1-4 players.
- [x] **Zombies pile up, nothing dropped** (Timed only, after playtests): zombies over the alive cap wait in a host backlog and spawn 4/s with their own wave's stats when there is room; HUD "Zombies 64 (+20)"; crash-guard ceiling 300 alive (host + client ~30+ fps measured, docs/MULTIPLAYER.md table) and a host frame rate guard (below 25 fps for 3 s: spawning pauses above 50 alive). Relay host message budget raised to 15000/s for relay-only clients. Classic keeps the original 50 and drops the rest.

## Phase 5 — Community
- [x] **Leaderboards** (Cloudflare D1, see docs/LEADERBOARD.md): best wave and most kills, Solo / Team, today (UTC) and all time; main menu > Leaderboard, a panel on the web page, the rank on the end-of-match screen. Multiplayer is recorded by the relay, single player with a signed start token + plausibility checks; default rules only.
- [x] **Game counts** per day in D1 (matches started / finished, player slots), unlisted `/api/stats`.
- [x] **Visitor analytics**: Cloudflare Web Analytics (cookieless), privacy note on the page.
- [x] Custom domain (zumbi-blocks-classic.vitosenic.com).
- [x] **Player profiles** (docs/ACCOUNTS.md): profile name (= nickname, unique) + password, one-time recovery code instead of e-mail, cloud save in D1 (money, items, look, settings; debounced upload, revision check against stale tabs, sendBeacon on unload), guest save migrated on the first signup, stats (best wave, total kills, games played) on main menu > Profile, leaderboard names tied to the profile ("name (guest)" for anybody else), Delete profile. Guest play unchanged.
- Discord server.
- Gamepad support; maybe touch controls for tablets.

## Phase 6 — Official & distribution (with Adrian)
- Brand it **Zumbi Blocks Classic** with Adrian; "Play Zumbi Blocks 2 on Steam" in the main menu.
- List it on web game portals (itch.io, Y8, CrazyGames, Poki) — this is where the original audience came from.
  - Prereqs: Adrian's written OK for portals specifically + agreement on ad revenue (ideally he submits under his account, or revenue goes to/split with him); CrazyGames SDK (portal build only); smaller initial download; desktop-only gate for mobile visitors.

## Backlog (from playtesting)
- ~~**Appearance screen shows no character.**~~ DONE. The original shows a 3D mannequin in the left sidebar (`sc_gamecontrol.update_manequim`, `sc_manequim`), not on the right. It works at the default 1280x720; the web page's Fullscreen button changes the screen size without re-placing it, so it drifted over the sidebar / off screen. Now re-placed when the screen size changes (docs/img/appearance-*-fullscreen.png).
- ~~**Saves must survive closing the tab.**~~ DONE: `PlayerPrefs.Save()` at the end of `save()`; test `server/test/e2e/save.mjs`.
- ~~**Missing effects:** rifle muzzle flash, flare (Rflare_out), RPG smoke (shotRPG).~~ DONE (nothing to convert): the original never used legacy particle emitters. All 29 particle components in v0.7 are Shuriken `ParticleSystem`s with their original data, and they render in the Unity 6 build. Verified in the editor and in the real WebGL build, screenshots `docs/img/effects-*.png`, test `server/test/e2e/effects.mjs` (see RECOVERY.md).
- ~~**Multiplayer airdrop bag on clients:** e2e saw 0 bags on the client side.~~ DONE: test artifact, clients do see (and can open) the bag. The test's bag counter went through `sc_matchcontroller`, which only the host has. Now counted by name; `supply.mjs` asserts the client sees exactly 1 bag and can open it (RECOVERY.md).
- **Live e2e shooting checks** (2026-10-09, after the save/mannequin deploy): e2e.mjs against the live URL, 3 runs: 42/42, 41/42, 42/42. The one failure was "shooter sees own bullets immediately" (sample [0,186]: a point-in-time count of the shooter's bullets that read 0 once while 186 shots were counted overall). Local runs pass; B's received bullets passed in all 3 live runs. Judged a timing flake of the point sample (live RTT is higher, short bullet life), roughly 1 in 3 runs for that one check; no consistent live failure, nothing changed.
- ~~**Phase 2b**~~ DONE (2026-10-11). Still to do: check relay `p2p-stat` logs for the real-world direct-connect rate.
- ~~**Live e2e must not touch real players.**~~ DONE: every test page carries the cookie `zbtest=1`; rooms made by such a page are hidden test rooms (never in the server list or Quick Play of real players, and test pages only ever see test rooms). Test rooms are unlisted by default (`createRoom`), only the server list tests list them.
- **Hidden host tab:** measure real behaviour once manually (Playwright cannot background a page).
