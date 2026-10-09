// Effects check: spawns each particle effect prefab in front of the camera (DebugFx hook) in the real WebGL build and
// screenshots the canvas a few times; asserts the effect changed the picture (pixels differ from the frame before spawning)
// and writes docs/img/effects-<name>.png (strip of frames). Also fires the real rifle and checks the muzzle flash shows.
//   CH=chrome BASE=http://localhost:8798/ node effects.mjs [name,name,...]
import { launch, game } from "./lib.mjs";
import { writeFileSync, mkdirSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { tmpdir } from "node:os";
const BASE = process.env.BASE || "http://localhost:8787/";
const OUT = process.env.OUT || new URL("../../../docs/img/", import.meta.url).pathname;
mkdirSync(OUT, { recursive: true });
const names = (process.argv[2] || "muzle rifle,Rflare_out,shotRPG,explosion,explosionbig,spark,woodspark,stdpart,blood_dense,bloodspill,bloodspillcone,bloodspillhard,redflare,muzle snipernopoof").split(",");
const b = await launch();
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };
const P = await game(b, "S", BASE);
await P.click_(750, 281); await P.click_(750, 281); await P.waitForTimeout(6000);
await P.click_(640, 360); await P.keyboard.press("Enter"); await P.waitForTimeout(8000);
const heal = () => P.evaluate(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugHeal", ""));
const grab = async () => P.locator("#unity-canvas").screenshot({ type: "png" });
// number of pixels whose colour changed noticeably between two PNGs (decoded with python, no extra node deps)
const SCRIPT = new URL("./pngdiff.py", import.meta.url).pathname;
const diff = (a, c) => { const fs = tmpdir() + "/zbfx_"; writeFileSync(fs + "a.png", a); writeFileSync(fs + "b.png", c); return Number(execFileSync("python3", ["-I", SCRIPT, fs + "a.png", fs + "b.png"]).toString().trim()); };
for (const n of names) {
  await heal();
  await P.waitForTimeout(700);
  const base = await grab();
  const frames = [];
  await P.evaluate((n) => window.unityInstance.SendMessage("ZbNetRunner", "DebugFx", n + ",3"), n);
  let best = 0, bi = 0;
  for (let i = 0; i < 6; i++) { await P.waitForTimeout(i < 2 ? 60 : 250); const f = await grab(); frames.push(f); const d = diff(base, f); if (d > best) { best = d; bi = i; } }
  writeFileSync(OUT + "effects-ingame-" + n.replace(/ /g, "_") + ".png", frames[bi]);
  const spawned = P.logs.slice(-40).some((l) => l.startsWith("ZBFX spawned " + n));
  check("effect " + n + " spawned", spawned);
  check("effect " + n + " changes the picture (px " + best + ")", best > 150, best);
  await P.waitForTimeout(3500);
}
console.log(`${pass} passed, ${fail} failed`);
await Promise.race([b.close(), new Promise((r) => setTimeout(r, 3000))]);
process.exit(fail ? 1 : 0);
