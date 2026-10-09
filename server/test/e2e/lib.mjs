import { chromium } from "playwright-core";
export async function launch() {
  return chromium.launch({ headless: true, channel: process.env.CH || undefined, args: process.env.CH ? ["--use-angle=metal", "--ignore-gpu-blocklist", "--enable-gpu-rasterization", "--autoplay-policy=no-user-gesture-required"] : ["--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist", "--enable-webgl", "--autoplay-policy=no-user-gesture-required"] });
}
export async function game(b, name, base = "http://localhost:8787/") {
  const ctx = await b.newContext({ viewport: { width: 1400, height: 900 } });
  const p = await ctx.newPage();
  p.name = name; p.logs = [];
  p.on("console", (m) => { const t = m.text(); p.logs.push(t); if (/zbnet|shim|Exception|Error|error/i.test(t) && !/GL Driver|ReadPixels/.test(t)) console.log(`[${name}]`, t.slice(0, 400)); });
  p.on("pageerror", (e) => console.log(`[${name}] pageerror`, String(e).slice(0, 300)));
  p.stats = { sent: {}, recv: {}, instSent: {}, instRecv: {} };
  const count = (st, inst, payload, base) => { const f = Buffer.from(payload); let o = 0; while (o + 2 <= f.length) { const len = f[o] | (f[o + 1] << 8); o += 2; const op = f[o]; st[op] = (st[op] || 0) + 1; if (op === base) { const g = f[o + (base === 0x83 ? 5 : 1)]; inst[g] = (inst[g] || 0) + 1; } o += len; } };
  p.on("websocket", (ws) => { ws.on("framesent", (f) => count(p.stats.sent, p.stats.instSent, f.payload, 2)); ws.on("framereceived", (f) => count(p.stats.recv, p.stats.instRecv, f.payload, 0x83));
  });
  p.on("websocket", (ws) => { console.log(`[${name}] ws open`, ws.url()); ws.on("framesent", (f) => { if (process.env.WSLOG) console.log(`[${name}] >>`, Buffer.from(f.payload).toString("hex").slice(0, 120)); }); ws.on("framereceived", (f) => { if (process.env.WSLOG) console.log(`[${name}] <<`, Buffer.from(f.payload).toString("hex").slice(0, 120)); }); ws.on("close", () => console.log(`[${name}] ws close`)); });
  await p.goto(base);
  await p.waitForFunction(() => document.querySelector("#loading").style.display === "none", null, { timeout: 120000 });
  await p.waitForTimeout(1500);
  p.click_ = async (x, y) => { const r = await p.evaluate(() => { const r = document.querySelector("#unity-canvas").getBoundingClientRect(); return { x: r.x, y: r.y }; }); await p.mouse.move(r.x + x, r.y + y); await p.waitForTimeout(80); await p.mouse.down(); await p.waitForTimeout(80); await p.mouse.up(); await p.waitForTimeout(400); };
  p.dump = async () => { const n = p.logs.length; await p.evaluate(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugDump", "")); for (let i = 0; i < 40; i++) { await p.waitForTimeout(100); const l = p.logs.slice(n).find((x) => x.startsWith("ZBDUMP ")); if (l) return JSON.parse(l.slice(7)); } return null; };
  p.shot = async (f) => { await p.locator("#unity-canvas").screenshot({ path: f }); };
  return p;
}
