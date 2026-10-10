// Rough performance check: frames per second of one headless Chrome page (host, solo) with many zombies alive. Used to pick the zombie ceiling of
// Timed waves (ZbMenu.ZOMBIE_CEILING). The cap is lifted with the DebugZmax hook and waves are advanced with DebugWaveClock.
import { launch, game, createRoom, heal } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8787/";
const b = await launch();
const A = await game(b, "A", BASE);
await createRoom(A, BASE, "Fps1");
const send = (m, a = "") => A.evaluate(([m, a]) => window.unityInstance.SendMessage("ZbNetRunner", m, a), [m, a]);
await A.click_(640, 360); await A.keyboard.press("Enter"); await A.waitForTimeout(6000);
await send("DebugZmax", "400");
const hb = setInterval(() => heal(A).catch(() => {}), 500);
for (let target of [25, 50, 80, 110, 150]) {
  for (let i = 0; i < 12 && (await A.dump()).zombies < target; i++) { await send("DebugWaveClock", "116"); await A.waitForTimeout(1500); }
  await A.waitForTimeout(3000);
  const s = await A.statsDump(), d = await A.dump();
  console.log("FPS zombies=" + d.zombies + " fps=" + s.fps);
}
clearInterval(hb);
await b.close(); process.exit(0);
