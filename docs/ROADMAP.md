# Roadmap — from revival to a proper game

Rule: **gameplay stays exactly like the original v0.7.** Everything here is around it: connection quality, getting into games, community.

Live: https://zumbi-blocks-classic.vitosenic.com (also https://zumbi-blocks.vito-senic.workers.dev). Adrian approved public hosting 2026-10-09.

**Cost rule: must stay $0.** Cloudflare Workers Free plan only (free limits fail closed, never bill). Phase 2 (WebRTC) is also the cost fix: the Durable Object only signals, so relay traffic leaves the free quota. No paid TURN; strict-NAT players fall back to the WebSocket relay. If it ever outgrows the free quota, the backstop is a flat-price Hetzner VPS relay (~EUR 4-5/mo), not the $5 Cloudflare plan.

## Phase 1 — Feel (done, see docs/MULTIPLAYER.md "Feel layer")
Make online play feel smooth without changing the network architecture.
- [x] **Net stats overlay** (toggle with F3): ping to host, jitter, messages/s, KB/s, packet backlog. Used to measure every later change.
- [x] **Interpolation** for remote players and zombies: render ~100 ms in the past and glide between snapshots instead of snapping.
- [x] **Instant local feedback** for shooting (already how the original works, verified and documented): hit marker / blood / sound shown on the shooter's screen immediately; damage is applied by the shooter's bullet as in the original.
- [x] **Batching**: send all messages from one frame in one WebSocket frame (existed; relay stamps and tests added) + compact state messages (about half the bytes per update) + about 20 Hz state rate.
- [x] **Latency test mode** (`?netsim=RTT,JITTER`): artificial delay/jitter in local tests so we can prove improvements.

## Phase 2 — Direct connection (browser to browser)
- WebRTC DataChannels between host and each client: unreliable/unordered channel for position updates, reliable channel for RPCs.
- The Durable Object becomes only the matchmaker/signaling server; Cloudflare TURN as fallback when a direct connection fails; WebSocket relay stays as last fallback.
- Expected: no extra hop through Cloudflare, no head-of-line blocking — LAN-like feel for nearby players.

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
