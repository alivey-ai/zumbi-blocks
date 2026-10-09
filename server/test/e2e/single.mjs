// Single player smoke test: Start Game > Play alone, Enter starts wave 1, then a bot (aim helper: NetRunner.DebugAim) shoots
// the zombies that reach it. Checks: loads offline, wave 1 spawns zombies, bullets damage zombies, a zombie dies.
// The bot is mortal and the pistol misses a lot (original behaviour), so up to 4 attempts are made to get a kill.
//   CH=chrome BASE=http://localhost:8787/ node single.mjs
import { launch, game } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8787/";
const b = await launch();
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };
let loaded = false, started = false, minLife = 100, killed = 0, shots = 0, attempts = 0;
for (; attempts < 4 && killed < 1; attempts++) {
  const P = await game(b, "S", BASE);
  await P.click_(750, 281); await P.click_(750, 281); await P.waitForTimeout(6000);
  let d = await P.dump();
  loaded = loaded || (d && d.mode === "offline" && d.players.length === 1);
  await P.click_(640, 360); await P.keyboard.press("Enter"); await P.waitForTimeout(10000);
  d = await P.dump();
  started = started || (d.zombies > 0 && d.wave === 1);
  const aim = () => P.evaluate(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugAim", ""));
  let melee = false, n = 0;
  for (let i = 0; i < 300 && killed < 1; i++) {
    const cur = await P.dump();
    if (!cur) continue;
    if (cur.zlifeMin !== undefined) minLife = Math.min(minLife, cur.zlifeMin);
    killed = Math.max(killed, cur.killed || 0);
    if (cur.hp === undefined) break; // the bot died
    if (cur.zdist !== undefined && cur.zdist < 2.6 && !melee) { await P.keyboard.press("2"); melee = true; await P.waitForTimeout(700); } // wrench for adjacent zombies
    if (cur.zdist !== undefined && cur.zdist < 30) {
      await aim(); await P.waitForTimeout(40);
      await P.mouse.down(); await P.waitForTimeout(60); await P.mouse.up(); shots++; n++;
      await P.waitForTimeout(melee ? 300 : 450);
      if (!melee && n % 14 === 0) { await P.keyboard.press("r"); await P.waitForTimeout(2500); }
    } else await P.waitForTimeout(400);
  }
  console.log("attempt", attempts + 1, JSON.stringify({ killed, minLife, shots }));
  await P.shot("single_end.png");
  await P.context().close();
}
check("single player loaded (offline mode, own avatar)", loaded);
check("wave 1 started: zombies spawned", started);
check("shooting damages zombies (life below 100)", minLife < 100, minLife);
check("shooting kills zombies (kill counter > 0)", killed > 0, killed);
console.log(`${pass} passed, ${fail} failed`);
await Promise.race([b.close(), new Promise((r) => setTimeout(r, 3000))]);
process.exit(fail ? 1 : 0);
