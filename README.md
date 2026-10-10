# Zumbi Blocks — web revival

Bringing the original **Zumbi Blocks** (AdrianKS, 2013, Kongregate / Y8) back to the browser: first single player via Unity WebGL, then real online multiplayer.

## Source
- `original/zb07web.unity3d` — the original Unity Web Player build (v0.7, Unity 4.2.0f4), from the Flashpoint Archive (entry `9d0d078b-3741-4c5a-ad30-460a0e4b1b83`, launch `kongregate.com/zumbiblocks/zumbiblocks.html`).
  sha256 `d099f5e3ad09c18bd85817c61a013bbfecfe0fe40bbfce7b66ae278d7f6a1afd`
- `original/zumbiblocks.html` — the original embed page.

## Rights
The game and its assets belong to Adrian (AdrianKS, now making Zumbi Blocks 2). On 2026-10-09 Adrian gave Vito permission to host this revival publicly with multiplayer (keep full credit to him + link to Zumbi Blocks 2). Keep a copy of his reply in `docs/permission/`.

## Status
1. Recovered the project from the bundle (AssetRipper) into the `game/` Unity project (`docs/RECOVERY.md`).
2. Ported to Unity 6 (6000.2.14f1) WebGL, single player works.
3. Online multiplayer on Cloudflare: one player's browser hosts, a Worker + Durable Object relays (`docs/MULTIPLAYER.md`, `server/`).
4. Resilience (phase 4): rate limits, desktop-only gate for phones, reconnect after a short drop, host leaves = players keep their money and a remaining player takes over the room, hidden host tab warning. Playtest additions: timed waves, wave number on the HUD, teammates' health.

## Layout
- `game/` Unity project. `Assets/Scripts/LegacyNetworkShim.cs` + `ZbNet.cs` + `Assets/Plugins/WebGL/ZbNet.jslib` implement the old Unity networking API on top of the relay. `Assets/Scripts/ZbMenu.cs` has the phase 3 menu logic (Quick Play, invite links, nickname prompt, settings, server list columns).
- `server/` Cloudflare Worker (plain JS): serves the WebGL build, `/api/rooms`, `/api/room/:code`, `/api/quickplay`, `/api/ping/:code`, `/ws/host`, `/ws/room/:code`, Durable Objects `Room` and `Lobby`. `server/test/relay.test.mjs` tests the relay routing. `game/Assets/WebGLTemplates/ZumbiBlocks/zbrtc.js` is the WebRTC lane (page script).
- `docs/` notes: `RECOVERY.md` (how the project was rebuilt), `MULTIPLAYER.md` (protocol, routing, limitations), `permission/` (Adrian's OK).

## Build the game
```
tools/unity.sh build1 -buildTarget WebGL -executeMethod BuildWebGL.Build     # -> game/Build/WebGL (gzip compressed, ~16 MB)
tools/unity.sh dbg1   -buildTarget WebGL -executeMethod BuildWebGL.BuildDebug # -> game/Build/WebGL-debug (uncompressed, stack traces)
```
(`tools/` is git-ignored scratch; `unity.sh` just wraps Unity 6000.2.14f1 batch mode and writes `tools/<name>.log`.)
The build is gzip compressed (`*.gz`): the raw wasm is 35 MB and Workers static assets allow 25 MiB per file. It therefore must be served with
`Content-Encoding: gzip`; the Worker does that (a plain `python -m http.server` will not work).

## Run locally
```
cd server && npm install
npx wrangler dev          # http://localhost:8787  (serves ../game/Build/WebGL, /api, /ws; Durable Objects run locally)
```
Open the page in two or more browser tabs/windows (keep the host's visible). Create server on one, join from the list or with the room code on the others.
Relay tests (with `wrangler dev` running): `cd server && npm test`.

## Deploy (Cloudflare account Vito.senic@gmail.com, id c7d9cc74b0354c939de1012aebe158cb)
```
cd server
npx wrangler whoami       # must show Vito.senic@gmail.com's Account; otherwise run: npx wrangler login
npx wrangler deploy       # uploads game/Build/WebGL as static assets + the Worker + Durable Object migrations
```
Worker name `zumbi-blocks` on workers.dev, `account_id` is pinned in `server/wrangler.jsonc`. Rebuild the game first if the game code changed.

## Privacy: peer-to-peer connection
Online play tries a direct browser-to-browser connection (WebRTC data channel, free public STUN servers only, no TURN) between the host and each player for the high-rate position updates. **This exposes IP addresses to the other end: the host's browser sees every player's IP and each player sees the host's.** Everything else, and everyone who cannot connect directly, goes through the Cloudflare relay as before. Add `?p2p=0` to the page address to turn it off completely. (Same note is shown on the game page.)

## How to play online
**Quick Play** (first button of the main menu) joins the best open game or starts one; a Quick Play room starts by itself after a short countdown. **Invite**: while you are in a room the page shows "Copy invite link" (`https://<site>/?room=CODE`); whoever opens it joins that room. Options > All settings (also reachable with ESC > Options in a match): field of view, sensitivity, volume, graphics Low / Medium / High, invert vertical look.

Manual: Start Game, then Create server (give it a name of 4+ characters; "Show in server list" toggles public listing; optional password).
Everyone else: Join server (pick from the list) or Direct IP, where the "Room code" field takes the 5 letter code that the host sees in the top right corner of the screen.
The host presses ENTER to start the match.
The create-server screen has **Waves: Timed / Classic** (Timed is the default: the next wave also starts 2 minutes after the previous one even if zombies are left; Classic is the original rule). If the connection drops for a few seconds the game reconnects by itself ("Connection lost - reconnecting"). If the host leaves, you keep the match money and a remaining player becomes the new host of the same room (a new match starts after a few seconds; "Main menu" cancels). Hosts: keep your tab visible, the others see "host paused" when it is in the background. Phones and tablets are not supported yet (a message is shown).
