// Save persistence: set money / inventory / nickname / look / volume through sc_gamecontrol.save() (test hook DebugSetSave),
// close the tab right away, then open the game again in a NEW browser context that reuses the same Chrome profile
// (persistent user-data-dir, i.e. a real "closed the tab / restarted the browser") and check the values are back.
//   CH=chrome BASE=http://localhost:8787/ node save.mjs
import { chromium } from "playwright-core";
import { mkdtempSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
const BASE = process.env.BASE || "http://localhost:8787/";
const dir = mkdtempSync(join(process.env.ZBTMP || tmpdir(), "zb-profile-"));
const args = ["--use-angle=metal", "--ignore-gpu-blocklist", "--autoplay-policy=no-user-gesture-required"];
async function open() {
  const ctx = await chromium.launchPersistentContext(dir, { headless: true, channel: process.env.CH || undefined, args, viewport: { width: 1400, height: 900 } });
  const p = ctx.pages()[0] || (await ctx.newPage());
  p.logs = [];
  p.on("console", (m) => p.logs.push(m.text()));
  await p.goto(BASE);
  await p.waitForFunction(() => document.querySelector("#loading").style.display === "none", null, { timeout: 120000 });
  await p.waitForTimeout(2000);
  p.prefs = async () => { const n = p.logs.length; await p.evaluate(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugPrefs", "")); for (let i = 0; i < 40; i++) { await p.waitForTimeout(100); const l = p.logs.slice(n).find((x) => x.startsWith("ZBPREFS ")); if (l) return JSON.parse(l.slice(8)); } return null; };
  return { ctx, p };
}
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };
const want = { money: 4321, skin: 2, beard: 3, beardcolor: 1, shirt: 4, nick: "Savey", inv3: 5, vol: 0.5 };
try {
  let { ctx, p } = await open();
  const first = await p.prefs();
  check("first run: defaults loaded", first && first.money === 0, JSON.stringify(first));
  await p.evaluate((c) => window.unityInstance.SendMessage("ZbNetRunner", "DebugSetSave", c), "4321,2,3,1,4,Savey");
  await p.waitForTimeout(300);
  const now = await p.prefs();
  check("values set in the running game", now && now.money === 4321, JSON.stringify(now));
  await p.waitForTimeout(Number(process.env.WAIT || 0));
  await p.close();           // close the tab right away
  await ctx.close();
  ({ ctx, p } = await open());
  let back; // the game reads its prefs a moment after the loading overlay is gone: poll
  for (let i = 0; i < 15; i++) { back = await p.prefs(); if (back && back.money === want.money) break; await p.waitForTimeout(1000); }
  check("after closing the tab and reopening: all values are back", back && Object.keys(want).every((k) => back[k] === want[k]), JSON.stringify(back));
  await ctx.close();
} finally { rmSync(dir, { recursive: true, force: true }); }
console.log(`${pass} passed, ${fail} failed`);
process.exit(fail ? 1 : 0);
