# Phase 2: WebRTC fast lane (judged plan)

Status: design only. Hard rule: gameplay stays exactly v0.7. Only the carrier of already-compact state messages changes; the WebSocket relay stays the reference path and the fallback for everything.

Verified against code at time of judging (`server/src/room.js`, `ZbNet.cs`, `ZbNet.jslib`): `webSocketMessage` returns early on string frames (text frames are free for signaling); `handle()` `C_STATE` fans out to every player except the sender (single place for a lane switch); `S_TIME(sender, stamp)` is prepended per recipient frame by the relay; `C_ECHO` routing already enforces "client may only target host"; `PROTO = 2`; `ZbNet.Queue/StampFrame/FlushOut` build one frame per game frame; `ZbInterp.Feed` already drops snapshots with `t <= last.t`.

## 1. Scores (1-10, higher is better; for effort and cost, 10 = cheapest/least)

| Criterion | D1 fast lane | D2 max-perf (host relay, v3 rooms) | D3 robustness lane |
|---|---|---|---|
| Feel / latency gain | 6: host to client state on DC; client to host stays on WS in 2a (2b closes it) | 9: everything incl. RPC and state direct, per-link minDelay, rate tuning | 6: same lane scope as D1, adds health/path logic |
| Connectivity success | 6: STUN only at first, failures just stay on WS | 7: TURN plus per-peer WS pipe, but cutover failure disconnects | 9: full ladder, ICE restart, TURN gate, hysteresis |
| Regression risk | 9: WS byte-identical, no PROTO bump, no room-logic change | 3: ports the whole relay (buffer, compaction, kill, owner checks) into C#; reliable cutover with no failover | 8: same as D1 plus more relay ops and more state machine |
| Implementation effort | 8: ~4-5 d for 2a | 3: 10-12 d, new ZbHostRelay.cs | 6: ~2 weeks incl. telemetry, restarts, banner |
| Cost | 9: STUN is free, TURN optional | 6: host uplink (N-1 copies), TURN needed to be useful | 7: TURN minted per room, billed relay, caps |
| Overall | 7.6 | 5.6 | 7.2 |

Winner: D1. Grafts: D3 health/hysteresis, relay-path gate and telemetry; D2 MTU-sized chunks, bufferedAmount gating, link-dependent interpolation delay, loss in netsim, sorted `Feed` insert (optional), "measure first" discipline.

Rejections and why:
- D2 v3 rooms (host-authoritative relay in C#): rejected for this phase. It duplicates `Room.handle()` correctness-critical logic (late-join buffer, `killmebuffered` compaction, ownership) in the host browser, makes the host uplink N-1x, and a hidden/slow host tab would then also stall relay duties. Reliable RPC over DC is deferred (Phase 2c, gated on data).
- D3 host-as-offerer and `C_LANE` from the host: rejected; client-as-offerer lets the host answer in JS without Unity's loop (works while the host tab is throttled and keeps signaling out of C#). Relay-side flag set by the client after a confirmed echo is simpler than a host-reported lane op.
- D3 per-room shared TURN credential: rejected; if TURN is ever added use per-IP-rate-limited `/api/ice` with short TTL.
- Claims doubted and resolved: (a) D1 "ordered + maxRetransmits 0" vs roadmap "unordered": ordered DCs still hold later packets behind a gap until SCTP forward-TSN advances, i.e. residual HoL on loss. Because `ZbInterp.Feed` already discards `t <= last.t`, reordering is harmless, so we choose **unordered, maxRetransmits 0**, tunable by `?life=` for A/B against `maxPacketLifeTime`. (b) D2's "losing one fragment loses the whole message" is standard SCTP behavior with partial reliability and is not verified in a source here; chunking at <= 1100 B sidesteps it regardless. (c) TURN pricing: sources disagree (D3 flagged); do not enable TURN until billing page is checked. TURN is also near-useless for latency here (about the same hops as the DO relay), so it is optional.

## 2. Chosen architecture

```
host browser                                client browser
 ZbNet (API unchanged)                      ZbNet (API unchanged)
  | WS binary: everything (as today) <-- Room DO --> WS binary: everything
  | WS text:   signaling JSON         (relay + signaling)  WS text: signaling JSON
  +==== RTCDataChannel "zb-fast" (negotiated id 1, unordered, maxRetransmits 0) ====+
        carries ONLY S_TIME + S_STATE + S_ECHO frames (same bytes the relay would emit)
```

Lane rule:

| Traffic | Path |
|---|---|
| RPC, INST, DESTROY, KICK, LISTING, PING, WELCOME, buffered replay | WS relay always |
| Host to client `S_STATE` (zombies, host avatar) | DC when client is `dcUp`; relay skips that client. Else WS. |
| Echo probes host <-> client | DC when up, else WS |
| Client to others `C_STATE` | WS in 2a. DC upstream is 2b. |

Star only: DC exists host<->client. Clients never learn each other's IPs. DC identity = the peer connection (SDP fingerprint rides the relay-vouched signaling path with relay-stamped `from`).

Honest expectation: 2a speeds up host to client direction (zombie and host positions, the bulk of bytes, and what the player sees most). Client to host (own input/position seen by the authority) stays on WS until 2b. Phase 1 stats overlay will show the real delta; do not promise "LAN feel" until 2b.

## 3. Protocol

Connect URL gains `&rtc=1` (client sets it unless `?p2p=0`). PROTO stays 2. Old builds never send it, never get text frames, never become `dcUp`.

All signaling is JSON text frames <= 8 KB.

Relay to client:
- `{"t":"hello","id":N,"host":bool}` right after `S_WELCOME`, only if `rtc=1`.
- `{"t":"sig","from":ID,"gen":G,"k":"offer|answer|ice|bye","sdp":"...","c":{...}}`, `from` stamped by relay.
- `{"t":"ice","iceServers":[...]}` only once TURN step exists (step 9).

Client to relay:
- `{"t":"sig","to":ID,"gen":G,"k":...}`; clients may target only 0, host any id; relay drops otherwise.
- `{"t":"dc","up":true|false}`.

Relay limits: length <= 8192 (charged to `takeTokens`), max 60 sig msgs per 10 s per socket, target socket must exist.

DC framing: identical to WS frames (`u16 len + body` repeated), using the server-to-client opcode set in both directions (`S_TIME 0x8b`, `S_STATE 0x8d`, `S_ECHO 0x8c`). Each chunk starts with its own `S_TIME(sender, stamp)` (sender field ignored by receiver; DC identity wins). Chunk size <= 1100 B on message boundaries.

Channel: `createDataChannel('zb-fast', {negotiated:true, id:1, ordered:false, maxRetransmits:0})` created on both sides before `createOffer`.

## 4. Lifecycle, health and fallback ladder

1. Client gets `hello`, creates pc with ICE servers (`stun:stun.cloudflare.com:3478`), channel, offer (generation `gen`), trickles ICE. Host answers in JS. Stale `gen` is ignored.
2. On channel open: 3-probe echo over DC. Client then sends `dc up:true`. Relay sets `att.dc` only after this round trip.
3. Health (from D3): echo each way every 500 ms over DC. Lane is Up while any DC frame arrived in the last 1500 ms. Down for 1500 ms: client sends `dc up:false` over WS, relay resumes WS fan-out immediately, both sides route echo/state back to WS. Recovery needs 3 consecutive good echoes (hysteresis) before `dc up:true` again.
4. Connect timeout 8 s. Retries: new `gen`, at 10 s and 30 s, then stay on WS for the session. ICE `disconnected` is transient: rely on the 1.5 s rule; ICE `failed`: close and retry per above.
5. Auto-demotion (D2/D3): host drops state frames for a peer whose DC `bufferedAmount` > 64 KB, and demotes the peer to WS if it stays above for 2 s. Cap `MAX_DC_PEERS = 8` (server var), extras stay on WS where the relay fans out for free.
6. Relay cleans flag on socket close, host close, `dc up:false`. Flag is stored in the socket attachment so it survives hibernation; `dcUp` Set rebuilt in the constructor.

Ladder: direct host/srflx DC -> (optional later) UDP TURN DC only if its median echo RTT <= 0.9 x WS echo RTT over 5 s, else drop it -> WS relay (today's behavior, never removed).

Switch safety: state is idempotent and latest-wins; during a flip a short duplicate window is possible and harmless. A gap is bounded by the 1.5 s watchdog plus the WS fallback.

## 5. File-level changes

### server/
- `src/room.js`
  - `fetch()`: if `rtc=1`, send text `hello` after welcome.
  - `webSocketMessage`: replace the string early-return with `handleText()` (size cap, JSON parse in try/catch, token charge, sig counter, star rule, `from` stamp, `dc` flag set via `serializeAttachment`).
  - `handle()` `C_STATE`: skip recipients where sender is host and `this.dcUp.has(id)`. Do not filter `C_ECHO`.
  - `dropPlayer()` / constructor: maintain `dcUp`.
  - Optional later: `MAX_DC_PEERS` var.
- `src/index.js`: nothing in 2a. Step 9 only: `GET /api/ice` (secrets `TURN_KEY_ID`, `TURN_KEY_API_TOKEN`, filter `:53` URLs, cache ~half TTL, per-IP limit, STUN-only when secrets missing).
- `wrangler.jsonc`: no change in 2a.
- `test/relay.test.mjs`: new cases (section 7).

### game/
- `Assets/WebGLTemplates/ZumbiBlocks/zbrtc.js` (new, plain page script, ES2015 OK; jslib is ES5 only per Unity docs). All RTCPeerConnection logic, queues, watchdog, netsim for DC. Fallback if Unity does not copy the extra template file: inline it in the template `index.html`.
- `Assets/Plugins/WebGL/ZbNet.jslib`: thin ES5 shims. In `ZbWsOpen` append `&rtc=1` and call `window.zbrtc.attach(...)`. Add polled functions `ZbRtcPeerUp`, `ZbRtcAnyUp`, `ZbRtcSend` (HEAPU8.slice copy), `ZbRtcRecv`, `ZbRtcLastPeer`, `ZbRtcLastArrival`, `ZbRtcClose`, `ZbRtcStats`. No JS-to-C# callbacks. Extend `?netsim=RTT,JITTER,LOSS%` so DC loss is simulated (loss only on DC; WS stays in-order, no loss).
- `Assets/Scripts/ZbRtc.cs` (new): static wrapper with `#if UNITY_WEBGL && !UNITY_EDITOR` DllImports and editor stubs. New file keeps merge conflicts with the Phase 1 agent small.
- `Assets/Scripts/ZbNet.cs` (about 30 lines, all additive):
  1. `StartHost/StartClient`: `&rtc=1` unless `ZbCfg.p2p == 0`. `ResetSession/Shutdown`: `ZbRtc.Close()`.
  2. `Queue()`: after the byte-identical WS append, if `ZbRtc.AnyUp` and opcode is host `C_STATE` or echo, also append to `fastBuf`.
  3. `FlushOut()`: after the WS send, per connected peer with `PeerUp`, chunk `fastBuf` (<= 1100 B, each chunk prefixed by fresh `S_TIME`), send; drop if `bufferedAmount` high.
  4. `SendEcho()`: if `PeerUp(target)`, DC only; else WS.
  5. `Tick()`: after the WS recv loop, drain `ZbRtc.Recv` into `ProcessFrame` with `ctxViaDc` and `forcedSender = ZbRtc.LastPeer()`; ignore sender fields inside DC frames.
  6. `S_TIME` case: skip `Clock(sender).Sample` for WS frames when `PeerUp(sender)` (two paths with different latency would corrupt one min-filter). Reset that peer's clock when the lane flips (small `ZbClock.Reset()` in `ZbNetSync.cs`).
  7. `DeliverRpc` for state messages only: an unknown root is now able to arrive before the WS INST; park the newest sample per view in a pending map with 1-2 s TTL and no warning log (not the 10 s replay used for RPCs).
  8. `ZbCfg.p2p` from `?p2p=0|1|force|drop` (0 = baseline for A/B, drop = kill the DC after N s test hook); optional `?life=`.
  9. `DrawStats/StatsDump`: add `path` (p2p/relay), DC rx/tx frames and bytes, DC loss % (from echo seq gaps), DC RTT (`Rtt(sender)` is already fed by `S_ECHO`), `bufferedAmount`, skipped sends, lane flips.
- `Assets/Scripts/ZbNetSync.cs`: `ZbCfg.p2p`, `ZbClock.Reset()`, per-link `minDelayFor(peer)` (100 ms on WS, `max(2 x tick interval, adaptive)` on a healthy DC, `?delay=` still overrides), optional sorted insert in `ZbInterp.Feed` only if measurements show late-but-useful snapshots (coordinate; shared with Phase 1).
- No gameplay script (`sc_*`) and no `LegacyNetworkShim.cs` change (optional later: `GetAveragePing` prefers DC RTT).

### docs/
`MULTIPLAYER.md` new "Data channel lane" section; `ROADMAP.md` tick Phase 2 items; privacy note (peer IPs visible to host).

## 6. Composing with Phase 1
- Batching: one DC send per peer per game frame at `LateTick`, same cadence; chunks bounded by MTU.
- Interpolation: sender stamp precedes every chunk; receiver samples a clock only from the lane that carries the state; unordered delivery is safe because `Feed` ignores older stamps.
- Netsim: existing `?netsim` models the relay path; add `LOSS` and (optional) `?netsimp2p=` so DC and WS can be shimmed separately. Honest A/B: `?p2p=0` vs `?p2p=1`, same netsim, plus real-network runs.

## 7. Test plan
1. Relay unit tests (`server/test/relay.test.mjs`): `rtc=1` gets `hello`, legacy client gets no text; sig routing (client to host OK, client to client dropped, spoofed `from` ignored, oversize/malformed dropped, rate limited); `dc up:true` for B: host `C_STATE` reaches C not B while host RPC still reaches B; `dc up:false`/disconnect restores; client `C_STATE` unaffected; flag survives hibernation.
2. Browser e2e (extend `server/test/e2e/e2e.mjs`, `lib.mjs`): Chrome with `--disable-features=WebRtcHideLocalIpsWithMdns`; assert DC up on all clients, zombie positions agree (`ZBDUMP`), client WS state rx near 0.
3. Fallbacks: `?p2p=0` runs the full existing e2e unchanged; `?p2p=drop` kills DC at 20 s, state resumes over WS in < 3 s without divergence; blocked ICE: game fully works on WS after 8 s; mixed room (host, A with DC, B `?p2p=0`).
4. Loss: `?netsim=RTT,JITTER,LOSS` plus macOS `dnctl/pfctl` on loopback UDP; compare starved%, extrap%, `stallFrames`, p99 state age for `p2p=0` vs `1`. Success bar: DC no worse on clean link, and clearly better p99 under 3% loss.
5. `measure.mjs`: localhost, deployed Worker, real cross-region pair. Manual: two real networks (home and phone hotspot), overlay shows `p2p`.
6. Hidden host tab: confirm DC and JS signaling survive, rAF-driven game loop still pauses (known limitation, not fixed here).
7. Telemetry: client reports connect result (ok/timeout/fail, candidate type) via a text `stat` message the relay logs; replaces guessed NAT failure rates.

## 8. Implementation steps (each shippable and revertable)
1. Relay: text routing, `hello`, `dcUp`, `C_STATE` exclusion, tests. Deploy; behavior identical because no client sets `rtc=1`. (0.5 d)
2. `zbrtc.js`, jslib shims, `ZbRtc.cs` stubs, `?p2p=force` opt-in. Prove channel open and echo over DC via overlay; no state moved. (1.5 d)
3. C# lane hooks: `fastBuf`, Tick drain, `S_TIME` guard and `ZbClock.Reset`, state pending-with-TTL, stats. Host state moves to DC. Run e2e and fallback tests. (1.5 d)
4. Health watchdog, hysteresis, retries, bufferedAmount demotion, `MAX_DC_PEERS`. (1 d)
5. Netsim loss, measurement runs, tune chunk size, `life`, per-link `minDelay`. (1 d)
6. Deploy default on behind `?p2p=0` kill switch; observe via overlay and telemetry for a week. Privacy note shipped here.
7. Phase 2b (only if step 6 data supports it): client upstream over DC. Client sends `S_`-format frames on the DC; host applies with `forcedSender`; host reflects via DC to other `dcUp` clients, and via a new host-only relay op `C_REFLECT(targets, body)` to the rest, with sender stamped by the relay; relay stops forwarding that client's WS `C_STATE`. (~2 d)
8. Phase 2c (reliable DC for RPCs, host-side buffer): not planned; revisit only if telemetry shows RPC head-of-line stalls matter.
9. Optional: `/api/ice` + TURN secrets + `?turn=1` with the D3 gate (keep UDP relayed pair only if faster than WS). Verify billing allowance first.

Total for 2a (steps 1-6): about 6-7 days.

## 9. Risks
- State may overtake the WS INST for a freshly spawned object (previously impossible with one ordered stream). Mitigation: step 3.7.
- Two paths feeding one `ZbClock` min-filter would corrupt interpolation timing. Mitigation: per-lane sampling and reset on flip; touches `ZbNetSync.cs` which the Phase 1 agent is editing, so rebase or sequence after Phase 1 lands.
- Gain in 2a is half of the path (host to client). Overselling it would be wrong; 2b is where client to host improves.
- Host uplink becomes N copies for DC peers plus the WS copy; capped and demoted as in section 4.
- Privacy: ICE exposes peer IPs (host sees clients' and vice versa), contradicting the anonymous-IP-id design. Needs disclosure and the `?p2p=0` switch; consider an in-game toggle.
- Real connectivity: symmetric NAT and corporate networks never connect without TURN; they simply stay on WS (no regression, no gain). Direct-success rate is unknown until telemetry (vendor 10-25% relay figures are not ours).
- Unity build must copy the extra template file (`zbrtc.js`); jslib must stay ES5 or the link fails.
- Headless Chrome mDNS candidate hiding breaks localhost ICE unless disabled; CI flake risk.
- A stale relay flag would silently starve a client of host positions. Mitigations: flag set only after confirmed echo, cleared on any client-detected failure, socket close, and hibernation-safe storage.
- Hidden host tab still freezes the match (rAF-driven loop); WebRTC does not fix this (Phase 4).
- Unordered vs ordered and `maxRetransmits` vs `maxPacketLifeTime` are hypotheses to settle with measurements, not assumptions.
