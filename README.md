# Zumbi Blocks — web revival

Bringing the original **Zumbi Blocks** (AdrianKS, 2013, Kongregate / Y8) back to the browser: first single player via Unity WebGL, then real online multiplayer.

## Source
- `original/zb07web.unity3d` — the original Unity Web Player build (v0.7, Unity 4.2.0f4), from the Flashpoint Archive (entry `9d0d078b-3741-4c5a-ad30-460a0e4b1b83`, launch `kongregate.com/zumbiblocks/zumbiblocks.html`).
  sha256 `d099f5e3ad09c18bd85817c61a013bbfecfe0fe40bbfce7b66ae278d7f6a1afd`
- `original/zumbiblocks.html` — the original embed page.

## Rights
The game and its assets belong to Adrian (AdrianKS, now making Zumbi Blocks 2). On 2026-10-09 Adrian gave Vito permission to host this revival publicly with multiplayer (keep full credit to him + link to Zumbi Blocks 2). Keep a copy of his reply in `docs/permission/`.

## Plan
1. Recover the project from the bundle (AssetRipper) → `game/` Unity project.
2. Port to Unity 6 (6000.2.14f1) + WebGL build, single player parity with the original.
3. Online multiplayer.
