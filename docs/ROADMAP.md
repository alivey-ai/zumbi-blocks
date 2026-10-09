# Roadmap — from revival to a proper game

Rule: **gameplay stays exactly like the original v0.7.** Everything here is around it: connection quality, getting into games, community.

Live: https://zumbi-blocks.vito-senic.workers.dev (Adrian approved public hosting 2026-10-09).

## Phase 1 — Feel (in progress)
Make online play feel smooth without changing the network architecture.
- **Net stats overlay** (toggle with a key): ping to host, jitter, messages/s, KB/s, packet backlog. Used to measure every later change.
- **Interpolation** for remote players and zombies: render ~100 ms in the past and glide between snapshots instead of snapping.
- **Instant local feedback** for shooting: hit marker / blood / sound shown on the shooter's screen immediately; the host still decides damage.
- **Batching**: send all messages from one frame in one WebSocket frame.
- **Latency test mode**: artificial delay/jitter in local tests so we can prove improvements.

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
- List it on web game portals (CrazyGames, Poki, itch.io, Y8 again) — this is where the original audience came from.
