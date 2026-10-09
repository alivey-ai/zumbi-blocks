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
- [ ] Client to host state over the channel (2b): the only way to take the clients' own upstream (about 25 frames/s each) off the Durable Object; today host to Durable Object traffic dropped by about half, total by 30 to 40 % with one client.
- [ ] Real-network verification (home + phone hotspot, different regions) and a week of `p2p-stat` log lines to learn the real direct-connect rate; only then decide on 2b.
- Not planned: TURN (costs money), reliable channel for RPCs.

## Phase 3 — Getting into a game
- Nickname on first launch (no accounts).
- **Quick Play** button: join the best open room or create one.
- Better server list: ping, players, current wave, map.
- **Invite links**: `.../?room=ABCDE` joins directly; copy button in the lobby.
- Settings menu: mouse sensitivity, FOV, graphics quality, volume, invert Y. Saved locally.

## Phase 4 — Resilience
- **Reconnect** after a short drop (keep your player for ~20 s).
- **Host migration**: if the host leaves, the next player takes over and the match continues.
- Host-tab-hidden warning/handling; basic rate limits and message size limits against griefing.

## Phase 5 — Community
- Leaderboards (Cloudflare D1): best wave solo / with friends, kills; daily and all-time.
- Personal stats saved per browser.
- Custom domain, Discord server, privacy-friendly analytics (Cloudflare Web Analytics) to see how many people play.
- Gamepad support; maybe touch controls for tablets.

## Phase 6 — Official & distribution (with Adrian)
- Brand it **Zumbi Blocks Classic** with Adrian; "Play Zumbi Blocks 2 on Steam" in the main menu.
- List it on web game portals (itch.io, Y8, CrazyGames, Poki) — this is where the original audience came from.
  - Prereqs: Adrian's written OK for portals specifically + agreement on ad revenue (ideally he submits under his account, or revenue goes to/split with him); CrazyGames SDK (portal build only); smaller initial download; desktop-only gate for mobile visitors.

## Backlog (from playtesting)
- ~~**Appearance screen shows no character.**~~ DONE. The original shows a 3D mannequin in the left sidebar (`sc_gamecontrol.update_manequim`, `sc_manequim`), not on the right. It works at the default 1280x720; the web page's Fullscreen button changes the screen size without re-placing it, so it drifted over the sidebar / off screen. Now re-placed when the screen size changes (docs/img/appearance-*-fullscreen.png).
- ~~**Saves must survive closing the tab.**~~ DONE: `PlayerPrefs.Save()` at the end of `save()`; test `server/test/e2e/save.mjs`.
- **Missing effects:** rifle muzzle flash, flare (Rflare_out), RPG smoke (shotRPG) used Unity 4 legacy particles, removed in Unity 6. Rebuild them with the Particle System to look like the originals.
- **Multiplayer airdrop bag on clients:** e2e saw 0 bags on the client side; confirm whether clients actually see the dropped bag.
- **Live e2e shooting checks failed once (3/42)** after the Phase 2 deploy; rerun and confirm flake vs real.
- **Phase 2b:** client to host state over the data channel (the real Durable Object cost cut); check relay `p2p-stat` logs for the real-world direct-connect rate.
- **Cloud "save code" (later, only if asked for):** short code like ZB-7K2M-Q9 to move a save between browsers, stored in D1, no accounts.
