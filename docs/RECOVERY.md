# Recovery notes: Zumbi Blocks 0.7 (Unity 4.2.0f4 Web Player) to Unity 6 WebGL

## Tools
- AssetRipper 2.0.0 (mac_arm64, free edition), run headless: `AssetRipper.GUI.Free --headless --port 8765`, driven via HTTP (`POST /LoadFolder Path=...`, `POST /Export/UnityProject Path=...`). Default settings: script content level 2, script export Hybrid (decompiled C#), shaders Dummy.
- Unity 6000.2.14f1 (macOS, WebGL module), built-in render pipeline, batchmode.
- Python 3 (own UnityWeb extractor, `scripts/extract_unityweb.py`).

## Steps (reproducible)
1. `python3 -I scripts/extract_unityweb.py original/zb07web.unity3d <dir>`. The UnityWeb container is one LZMA ("alone") stream; it holds 4 managed DLLs (Assembly-UnityScript, firstpass x2, Boo.Lang), level0-1, mainData, sharedassets0-2, and unity_builtin_extra.
   AssetRipper opened the .unity3d directly but only exported **dummy script stubs**, because it does not pull the DLLs out of a web bundle.
2. Lay the files out like a Windows standalone player: `ripped/src/ZB_Data/{mainData,level*,sharedassets*,Managed/*.dll,resources/unity_builtin_extra}` plus an empty `ZB.exe`. Load that folder in AssetRipper (`/LoadFolder`) and `/Export/UnityProject`. Now the scripts are real decompiled C# (about 16k lines in 68 classes).
3. Copy `ExportedProject/Assets` + `ProjectSettings` into `game/`, delete the stray `Assets/Plugins/*.dll` and `Properties/AssemblyInfo.cs`, write a trimmed `Packages/manifest.json`, then apply the scripted rewrites below, and open with Unity 6 in batchmode.
4. Build: `Unity -batchmode -quit -projectPath game -buildTarget WebGL -executeMethod BuildWebGL.Build -logFile -` -> `game/Build/WebGL` (gitignored). Serve with any static server, e.g. `python3 -m http.server 8137 --directory game/Build/WebGL`.
   (Unity prints "metal toolchain missing" errors in the log from editor shader compile; they do not affect the WebGL build.)

## What was recovered
- Scenes in original build order: `menu` (0), `city` (1), `empty` (2) in `Assets/Meus assets/Cenas/`. Menu -> Play alone loads city.
- 68 UnityScript classes (decompiled to C#), 329 meshes, 125 textures, 25 audio clips, 141 materials, 153 prefabs, 89 animation clips, terrain data, GUI skins, fonts.
- Original UnityScript is gone, so code is ILSpy-style C# (casts like `(Object)(object)x`, `Main()` stubs, magic ints for enums, all methods were `override` which we dropped).

## Changes made and why
- `scripts/fix1..3`: mechanical rewrites of Unity 4 APIs: removed shorthand properties (`.animation/.rigidbody/.renderer/.light/.camera/.audio/.collider/.particleSystem` -> `GetComponent<T>()`), `ParticleSystem.emissionRate` -> `emission.rateOverTime`, `Screen.showCursor/lockCursor` -> `Cursor`, ILSpy `(Type)(ref x)` artefacts, `Physics.Raycast(..., ref hit)` -> `out`, `Object.op_Implicit`, `LayerMask.op_Implicit`, removed bogus `override`, `PhysicMaterial` -> `PhysicsMaterial`, `using Object/Random = UnityEngine.*` aliases, dropped ILSpy `_002Ector()` calls. `Application.LoadLevel` still compiles (obsolete warning only).
- **Shaders**: AssetRipper exported dummy copies of Unity 4 built-ins. `Assets/Editor/FixLegacyShaders.cs` (run once) re-pointed 58 materials to `Legacy Shaders/...` (Specular, Transparent/*, Cutout, Self-Illumin, Particles) and `Skybox/6 Sided`, then the dummy shader folder was deleted. Diffuse materials already resolved to Legacy Diffuse. Colour space set to Gamma to match the Unity 4 look.
- **Networking stub**: `Assets/Scripts/LegacyNetworkShim.cs` (see below).
- Build settings: WebGL, 1280x720, gzip compression (served with Content-Encoding by the Worker, see README), IL2CPP, strip minimal (reflection used by the RPC shim). See `Assets/Editor/BuildWebGL.cs`.
- Packages: Unity re-added its default packages to the manifest on first open.

## Networking in the original (for the multiplayer phase)
Unity legacy networking (RakNet based): `Network`, `NetworkView`, `[RPC]`, `MasterServer`. Not Photon.
- Single player = hosting a local server: `Network.InitializeServer(1, 50123, false)` then `LoadLevel(1)`.
- Hosting: `Network.InitializeServer(16, port, useNat)`, optional password (`Network.incomingPassword`), `MasterServer.RegisterHost`/`RequestHostList("Zumbiblocks " + buildversion)`, `PollHostList`, NAT checks (`TestConnection`, `HavePublicAddress`). Server list UI in `sc_gamecontrol` (HostData: name, players /16, password flag, ip, port, comment).
- Joining: `Network.Connect(ips, port, password)`; callbacks `OnConnectedToServer`, `OnFailedToConnect`, `OnDisconnectedFromServer`, `OnPlayerConnected/Disconnected` (in `sc_gamecontrol`, `sc_matchcontroller`).
- Server-authoritative style: server runs `sc_matchcontroller` (waves, zombie/item/supply spawning via `Network.Instantiate` with group ids: 0 player/spawner, 1 bag, 3 particles, 4 terrain, 5 world objects, 6 ground items, 10 constructions, 11 sound), `sc_zumbi` AI runs on server. 36 `Network.Instantiate` calls, 319 `networkView.RPC` calls (RPCMode Others=1, All=2, OthersBuffered=5, AllBuffered=6, Server=0), ~76 `[RPC]` methods (animation fades, hurtme, killmebuffered, syncinfo, setskin, chat `sendchat/receivechat/servermessage`, `receiveitem/receiveammo`, `setpermission`...). 29 uses of `networkView.isMine`, plus `sc_sendpositionupdate`/`sc_updateposition` for position sync. Players are tracked in `sc_matchcontroller.everybodyspawners` and `sc_matchdata` (names, pings).
- Done: the shim now has a real transport (WebSocket) and a Cloudflare Durable Object relay, see `docs/MULTIPLAYER.md`.

### Shim behaviour (single player)
`Network.Instantiate` = `Instantiate`; `networkView` -> `NetView()` extension that lazily adds a `NetworkView` MonoBehaviour; `RPC(name, mode, args)` invokes the matching method on the same GameObject by reflection for Server/All/AllBuffered, ignores Others*; `MasterServer` returns no hosts; `Network.Connect` fails. (Superseded: the shim now also implements online play, see `docs/MULTIPLAYER.md`; the offline loopback is still what `InitializeServer(1, ...)` does.)

## Known issues / not verified vs original
- Verified only: menu renders, "Play alone" loads the city with HUD, weapon and the "Press ENTER to start" prompt, no console errors in a short session. Not played through: waves, zombies, vehicles, inventory, building.
- Lightmaps/lighting and skyboxes from Unity 4 may differ in brightness; scene looks darker than the original in places (needs side by side comparison).
- (Resolved, see "Particle effects" below: the 3 prefabs `muzle rifle`, `Rflare_out`, `shotRPG` were never legacy-emitter based; they work.)
- Terrain details/trees (old terrain shaders) not checked.
- Multiplayer buttons are inert. Cursor lock in WebGL needs a click.
- The decompiled code is unreviewed; ILSpy can mis-render some constructs (check coroutines, `yield`).
- Rights: Adrian's game; he permitted public hosting on 2026-10-09 (see `docs/permission/`), keep the credit and the Zumbi Blocks 2 link on the page.

## Performance (FPS in Chrome)
Symptom: low FPS on a retina Mac. Changes:
- **Canvas scale capped at 1x** (`config.devicePixelRatio = 1`, patched into `index.html` by `BuildWebGL.PatchIndexHtml`). Unity 6 defaulted to devicePixelRatio 2, so the 1280x720 canvas rendered 2560x1440 (4x the pixels, on fill-rate-heavy legacy shaders). The original web player rendered 1x. Main cause.
- **`Application.targetFrameRate`**: `sc_gamecontrol` set 120; on WebGL a fixed rate switches Unity from requestAnimationFrame to setTimeout pacing. Now -1 under `UNITY_WEBGL`.
- **RPC shim**: `LegacyNetworkShim.Dispatch` cached MethodInfo lookups per (type, name, argc) instead of reflecting on every call.
- **Terrain pixel error** 1 -> 5 in `city.unity` (Unity default; 1 is extremely heavy on mesh LOD).
- Checked and left as is: build is release (not development), explicit-exceptions-only, IL2CPP OptimizeSpeed, no light shadows in scene or prefabs, WebGL default quality "Good", single camera (far 1000, no HDR/MSAA), few per-frame Find calls (throttled).
- Measuring: the automated Chrome tab is hidden (rAF throttled to ~0.2 fps), so no reliable FPS numbers; only verified canvas size 2560x1440 -> 1280x720.

## Rendering artifacts (flicker, diagonal hatching, dark blotches)
Symptom (reported 2026-10-09, Chrome on an Apple Silicon Mac): the whole city "flickers like an old TV". Walls, ground, house and tree surfaces are covered in diagonal hatching and tile-shaped dark blotches that change every frame as the camera moves. Not in the original. Before/after at the identical frozen game state: `docs/img/render-artifacts-before.png` / `render-artifacts-after.png` (and `-2`).

**Root cause**: Unity's built-in Legacy `Diffuse`/`Specular` shaders draw every extra per-pixel light as a second pass over the object (ForwardAdd: blend ONE,ONE, no depth write, depth test LEQUAL). On Apple GPUs (ANGLE on Metal) the base pass and the additive pass do not produce bit-identical depth for the same triangle (different compiled vertex shaders), so LEQUAL fails for part of the pixels (in GPU tile / triangle shaped patches), those pixels miss the extra light and the pattern re-rolls whenever the camera moves by a sub-pixel. The scene always has such a pass (the player's own point light "lumiando", the second scene light), so every surface is affected.

**How it was found** (frozen game via `ZbNetRunner.DebugFreeze`, fixed spot via `DebugPose`, one stable blotchy frame, then toggling one thing at a time and scoring high-frequency noise):
- Not shadows (quality shadows off, light shadows are None), not lightmaps (clearing them changed nothing), not depth range/precision (near 0.01..1, far 100..1000 barely change it; depth buffer is 24 bit, no reversed Z), not pixel light count or turning lights off, not shader precision (Unified model tried, no real gain), not z-fighting geometry (a ray through a blotch hits exactly one renderer; the 78 near-coplanar renderer pairs are small window/trim quads), not textures, not material overrides (VertexLit and Unlit versions of the same wall are clean).
- A WebGL draw-call tap (wrapping `drawElements*` in the page) showed each opaque draw (program 5, 103 draws) followed by an additive draw of the same objects (program 18, 103 draws, `blend ONE,ONE`, `depthMask false`, `LEQUAL`). Skipping the blend draws removes all artifacts (score 0.3); drawing only those with `polygonOffset(-1,-1)` brings it to the same level (0.6). Software GL (SwiftShader) never showed the problem, which fits a GPU compiler precision issue.

**Fix**: `game/Assets/WebGLTemplates/ZumbiBlocks/index.html` wraps the WebGL2 state and draw calls before Unity loads and draws exactly those additive passes with `polygonOffset(-1, -2)` (a hair closer to the camera). Lighting is unchanged, only the depth test now passes. `?nodepthfix` in the URL (or `window.zbDepthFix = false` at runtime) turns it off for A/B comparison. Measured on 20 frozen frames across 5 spots (same state, fix off vs on): hatching score 1.1 to 6.4 off, 0.33 to 0.46 on (0.3 is the flat-surface floor from stars and fine textures). Single player e2e (4) and the multiplayer e2e (35) pass.

If this ever has to be fixed at the shader level instead: copy the Legacy shaders from the Unity builtin_shaders package, add `Offset -1,-2` to their ForwardAdd passes and re-point the materials the way `FixLegacyShaders.cs` did.

Also checked and unchanged: Unity 6 reads lightmap index 255 (Unity 4's "not lightmapped", still set at runtime by `sc_gamecontrol` / `sc_turnofflightmapp`) as an out-of-range slot, but changing it to -1 had no visible effect, so it was left alone.

## Gameplay fixes
- **Airdrop (radio / S.O.S) dropped hundreds of crates in single player.** Root cause: `Network.Destroy(NetworkViewID)` (`LegacyNetworkShim.cs` -> `ZbNet.DestroyView`, `ZbNet.cs`) only destroyed objects registered in `roots`, and `ZbNet.Instantiate` registers a root only when a session is `Active`. Single player is offline, so the call silently did nothing. `sc_suplly.Update` (`Assembly-UnityScript/sc_suplly.cs`) spawns the bag with `Network.Instantiate`, then relies on `Network.Destroy(viewID)` to remove the falling crate. The crate survived and spawned a new bag every frame once `conter > timetofall` (measured 452 bags about 7 s after landing, ~67 per second, still growing). Multiplayer was not affected (roots registered, one bag). Fix: `DestroyView` falls back to the registered NetworkView GameObject. The decompiled UnityScript was faithful; only the shim differed.
- Verification: `server/test/e2e/supply.mjs` (hooks `DebugSupply`, `DebugHeal`, `bags` in `DebugDump`). Before: single player peak 452+ bags. After: exactly 1 falling crate, none left falling, 1 bag; host + client: 1 crate, 1 bag on the host, never more than 1 on the client.
- **Saves lost when closing the tab.** The original `sc_gamecontrol.save()` never calls `PlayerPrefs.Save()`; the web player flushed on its own, Unity 6 WebGL only writes PlayerPrefs to IndexedDB on `Save()` (verified: before, the ZB_* keys never reached IndexedDB; a reopened profile had defaults). Fix: `PlayerPrefs.Save()` at the end of `save()`. Test: `server/test/e2e/save.mjs` (persistent Chrome profile, new context; hooks `DebugSetSave`/`DebugPrefs`). Note the game reads its prefs about a second after the loading overlay disappears.
- **Menu mannequin misplaced after page fullscreen.** `update_manequim()` positions the mannequin from the screen size at call time; only the in-game fullscreen button re-called it (`mane_up_cont`). With the page's Fullscreen button the character stayed at the old world position (overlapping the sidebar, off screen on big displays). `sc_gamecontrol` now remembers the screen size (`mane_w/mane_h`) and re-places it when it changes while a sidebar menu screen is shown. Gameplay unaffected. Screenshots: `docs/img/appearance-before-fullscreen.png`, `appearance-after-fullscreen.png`.

## Phase 3 hooks in the recovered scripts (re-apply after a re-rip)
New logic lives in `ZbMenu.cs` (`ZbMenu`, `ZbSettings`). The decompiled scripts only got these lines (grep `ZbMenu` / `ZbSettings`):
- `sc_gamecontrol.OnGUI`: `ZbMenu.EntryState` in the `gamestate == -1` block (the menu scene actually starts at 0, `ZbMenu.Gate` decides the first screen), `ZbMenu.Gate(this)` after it, `ZbMenu.MainMenuQuick` in state 0, `FilterNick` after the nickname text field (state 22), the state 9 Options screen gets an "All settings" row (Back moves down one row), state 26: `ListHeader`, `ListRow`, `"/" + playerLimit` instead of `"/16"`, `ZbMenu.Full(server_select)` in the Join condition, and `ZbMenu.Draw(this, num7)` before state 13.
- `sc_gamecontrol.save()`: `ZbMenu.BeforeSave(this)` first line. `sc_gamecontrol.OnFailedToConnect`: `if (ZbMenu.QuickFail(this, error)) return;` first line.
- `sc_player`: `headang += ZbSettings.MouseY() * mouse_sense;` (invert Y).
- `sc_spawner` pause panel (state 2): the Options button, empty in the original, sets `ZbMenu.overlay = true`.

## Phase 4 / playtest hooks in the recovered scripts (re-apply after a re-rip)
New logic: `ZbNetSession.cs` (resume), `ZbTeam.cs` (teammates' health), `ZbMenu.cs` (host-left screen, waves toggle). One-line hooks:
- `sc_gamecontrol.OnDisconnectedFromServer`: `ZbMenu.HostLeftBank(this)` before `gamestate = 8` (banks the match money when the host left), `ZbMenu.HostLeftScreen(this)` after `Application.LoadLevel(2)` (host-left screen / claim restart; also re-parents the camera to DontDestroyOnLoad: the original unparents it into the city scene, which kills it, and the next match then throws).
- `sc_gamecontrol.OnGUI` state 25: `ZbMenu.WavesToggle(this, num7)` replaces the NAT toggle (ignored in this port).
- `sc_matchcontroller` (wave timer): `ZbMenu.TimedWaves()` arms the 120 s timer for every wave; `matchdata.waveleft` is set from it.
- `sc_matchdata`: field `waveleft`, RPCs `syncwavetimer` and `synchealth` (-> `ZbTeam.Set`), `syncwavetimer` is sent next to `sendstuff`; `OnGUI` draws "Wave N" before the clock and "Next wave in m:ss" below it.

## Particle effects (checked 2026-10-09)
The backlog said `muzle rifle`, `Rflare_out` and `shotRPG` used Unity 4 legacy particles (Ellipsoid/Mesh Particle Emitter, Particle Animator, Particle Renderer) that Unity 6 removed. **That was wrong.**
- Scan of the raw original data (`ripped/src/ZB_Data`, every object of mainData, level0-1, sharedassets0-2, read with UnityPy): no object of class 12 (ParticleAnimator), 15 (EllipsoidParticleEmitter), 26/98 (ParticleEmitter, MeshParticleEmitter) or 27 (ParticleRenderer) exists. The only particle components are 29 `ParticleSystem` (class 198) + 29 `ParticleSystemRenderer` (199), i.e. Shuriken, which Unity 6 still has and imports from the Unity 4 YAML unchanged (AssetRipper exported them; the exported project and `game/` are identical for these prefabs).
- Prefabs with particles (all kept as recovered, nothing converted): `muzle rifle`, `muzle snipernopoof` (guns), `Rflare_out`, `redflare` (flares), `shotRPG` (RPG smoke trail + fire), `explosion`, `explosionbig`, `spark`, `woodspark`, `stdpart`, `blood_dense`, `bloodspill`, `bloodspillcone`, `bloodspillhard`, plus the ones embedded in `m202rocket`, `mightyplayer`, `zumbidoll`, `terrinha`, `manequim_hmmm_manequim`. Their materials (`Spark`, `Fairy Dust`, `Poof1`, `Fire Add`, `pixelpart`) resolve to `Legacy Shaders/Particles/Additive` or `Alpha Blended`.
- Verification 1 (editor, `Assets/Editor/EffectsShots.cs`: `Unity -batchmode -projectPath game -executeMethod EffectsShots.Run`, env `ZB_FX_OUT`, `ZB_FX`): each prefab is simulated frame by frame and rendered to `docs/img/effects-<prefab>.png` (7 frames over time; the muzzle flash lives 0.1 s, the flares and RPG loop 5 s).
- Verification 2 (real WebGL build): `server/test/e2e/effects.mjs` spawns each prefab in front of the camera with the `DebugFx` hook and checks that the canvas changes (14 prefabs, all pass); the best frame of each is saved as `docs/img/effects-ingame-<prefab>.png`.
- Not fixed because nothing was broken. If someone saw no muzzle flash: the Glock (starting weapon) has no muzzle prefab (only the flash light); rifles, SMGs, shotguns do, and the flash lives only 0.1 s.

## Multiplayer airdrop bag on clients (checked 2026-10-09)
Question: the earlier multiplayer e2e saw 0 bags on the client after an airdrop (host saw 1). Finding: **test artifact, not a bug.** The `bags` counter in `ZbNetRunner.DebugDump` found the bag prefab through `FindFirstObjectByType<sc_matchcontroller>().supllyprefab`, and `sc_matchcontroller` exists only on the host (`sc_gamecontrol` creates it with a plain `Object.Instantiate` in the server branch), so on a client the lookup was null and `bags` was always 0.
- Fix (test side only, no gameplay change): `bags` is now counted by clone name (`supplybaground(Clone)`, also `bagsAll` without the parent check). New hook `DebugOpenBag` does what pressing E on the bag does (`RPC("openbag", RPCMode.All)`).
- `server/test/e2e/supply.mjs` now asserts for host + client: one falling crate seen by both, crate gone after landing, exactly one bag on the host and exactly one on the client (peak and final), and that the client opening the bag removes it on both.
- Measured locally: host and client both show crate 1 -> 0 and bags 0 -> 1 in the same second; client opens it, bag gone on both.
- Local verification of this change (own build, `wrangler dev --port 8798`): single.mjs 4/4, supply.mjs 9/9, effects.mjs 28/28, e2e.mjs 42/42. Three earlier e2e runs on a machine at load average ~25 failed the late-joiner group: the idle test players were killed before D joined ("End of match: Everybody is dead"). It passes at normal load.
