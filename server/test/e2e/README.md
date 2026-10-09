Browser end-to-end test: 4 headless Chrome pages (host, list joiner, code joiner, late joiner) play against a running server.

```
cd server && npm install --no-save playwright-core
npx wrangler dev                                  # or use the deployed URL
cd test/e2e
CH=chrome node e2e.mjs                            # BASE=https://zumbi-blocks.vito-senic.workers.dev/ CH=chrome node e2e.mjs
```
`CH=chrome` uses the installed Google Chrome with GPU rendering (needed for a playable frame rate; software GL is too slow and
the game's remote-player interpolation diverges at very low fps). Clicks use fixed canvas coordinates of the original menus.
Zombies can kill idle test players, so rarely a check is flaky; rerun.

With the WebRTC lane (phase 2) the headless Chrome gets `--disable-features=WebRtcHideLocalIpsWithMdns` (see `lib.mjs`) so local ICE candidates work. `e2e.mjs` checks the channel when the page has it on (D joins with `?p2p=0` to make a mixed room); run it also with `BASE=http://localhost:8787/?p2p=0`.
`fallback.mjs` sabotages the lane (stalled channel, dropped channel with `?p2p=drop`, blocked ICE with `?p2p=block`) and checks that play continues on the WebSocket and that the lane recovers.
`measure.mjs` also prints `DO-MSGS`: WebSocket frames per second the pages send, i.e. what the Durable Object receives (compare `?p2p=0` with the default).
`phase3.mjs` (needs `ws`, which is in the server devDependencies) covers phase 3: nickname prompt on a fresh profile, Quick Play (creates a room, then a second browser joins it), the auto start countdown, the server list fields, invite links (join, unknown room, other build, closed room), and settings persistence (menu and in-game). Pages created with `game(b, name, base, { fresh: true })` keep the first-run nickname prompt; all others skip it (`DebugSkipNick`) so the older tests are unchanged.

`supply.mjs` checks the radio airdrop (single player: one crate, one bag; host + client: both see one crate then exactly one bag, the client can open it). `effects.mjs` spawns every particle effect prefab in the real build (`DebugFx` hook) and writes `docs/img/effects-ingame-*.png`; the editor renderer `Assets/Editor/EffectsShots.cs` writes `docs/img/effects-*.png` (see docs/RECOVERY.md).

If `e2e.mjs` late-joiner checks fail locally but pass live, clear stale local Durable Object state: `rm -rf server/.wrangler/state` and restart `wrangler dev`.
