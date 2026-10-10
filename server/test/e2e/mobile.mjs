// Desktop-only gate (phase 4 step 2): phones and tablets get a message instead of the game; desktop is unaffected.
import { chromium, devices } from "playwright-core";
import { launch } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8787/";
const b = await launch();
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };

async function visit(label, ctxOpts, url = BASE) {
  const ctx = await b.newContext(ctxOpts);
  const p = await ctx.newPage();
  const reqs = [];
  p.on("request", (r) => reqs.push(r.url()));
  await p.goto(url);
  await p.waitForTimeout(2500);
  const st = await p.evaluate(() => ({
    gate: getComputedStyle(document.querySelector("#gate")).display,
    stage: getComputedStyle(document.querySelector("#stage")).display,
    gateText: document.querySelector("#gate").innerText,
    gated: !!window.zbGated,
    unity: !!window.unityInstance,
    title: document.title,
    scrollW: document.documentElement.scrollWidth, innerW: window.innerWidth,
  }));
  const build = reqs.filter((u) => /\/Build\/WebGL\.(data|wasm|framework)/.test(u));
  return { ctx, p, st, build, reqs };
}

for (const [label, dev] of [["iPhone", devices["iPhone 13"]], ["Android phone", devices["Pixel 7"]], ["iPad", devices["iPad Pro 11"]], ["Android tablet", devices["Galaxy Tab S4"]]]) {
  const r = await visit(label, dev);
  check(`${label}: gate message shown, game stage hidden`, r.st.gate === "block" && r.st.stage === "none" && /Desktop browser needed/i.test(r.st.gateText), JSON.stringify(r.st));
  check(`${label}: nothing of the game is downloaded`, r.build.length === 0 && !r.st.unity, JSON.stringify(r.build));
  check(`${label}: fits the screen (no horizontal scroll)`, r.st.scrollW <= r.st.innerW + 1, JSON.stringify([r.st.scrollW, r.st.innerW]));
  await r.ctx.close();
}
{
  const r = await visit("iPhone invite", devices["iPhone 13"], BASE + (BASE.includes("?") ? "&" : "?") + "room=ABCDE");
  check("iPhone invite link: message names the room", /ABCDE/.test(r.st.gateText), r.st.gateText);
  await r.p.screenshot({ path: "m_gate_iphone.png" });
  await r.ctx.close();
}
{
  // narrow desktop window: a desktop UA is never gated by size alone
  const r = await visit("narrow desktop", { viewport: { width: 420, height: 800 } });
  check("narrow desktop window is not gated", r.st.gate === "none" && !r.st.gated, JSON.stringify(r.st));
  await r.ctx.close();
}
{
  const r = await visit("desktop", { viewport: { width: 1400, height: 900 } });
  check("desktop: no gate, game loads", r.st.gate === "none" && r.build.length > 0, JSON.stringify([r.st, r.build.length]));
  await r.p.waitForFunction(() => document.querySelector("#loading").style.display === "none", null, { timeout: 120000 });
  check("desktop: game instance created", await r.p.evaluate(() => !!window.unityInstance));
  await r.ctx.close();
}
{
  const r = await visit("override", devices["iPhone 13"], BASE + (BASE.includes("?") ? "&" : "?") + "desktop=1");
  check("?desktop=1 skips the gate (testing)", r.st.gate === "none" && !r.st.gated, JSON.stringify(r.st));
  await r.ctx.close();
}
console.log(`${pass} passed, ${fail} failed`);
await Promise.race([b.close(), new Promise((r) => setTimeout(r, 3000))]);
process.exit(fail ? 1 : 0);
