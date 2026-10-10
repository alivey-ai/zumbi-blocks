import { chromium } from "playwright-core";
export async function launch() {
  return chromium.launch({ headless: true, channel: process.env.CH || undefined, args: process.env.CH ? ["--use-angle=metal", "--ignore-gpu-blocklist", "--enable-gpu-rasterization", "--autoplay-policy=no-user-gesture-required", "--disable-features=WebRtcHideLocalIpsWithMdns"] : ["--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist", "--enable-webgl", "--autoplay-policy=no-user-gesture-required", "--disable-features=WebRtcHideLocalIpsWithMdns"] });
}
export async function game(b, name, base = "http://localhost:8787/", opts = {}) {
  const ctx = await b.newContext({ viewport: { width: 1400, height: 900 } });
  const p = await ctx.newPage();
  p.name = name; p.logs = [];
  p.on("console", (m) => { const t = m.text(); p.logs.push(t); if (/zbnet|shim|Exception|Error|error/i.test(t) && !/GL Driver|ReadPixels/.test(t)) console.log(`[${name}]`, t.slice(0, 400)); });
  p.on("pageerror", (e) => console.log(`[${name}] pageerror`, String(e).slice(0, 300)));
  p.stats = { sent: {}, recv: {}, instSent: {}, instRecv: {} };
  const count = (st, inst, payload, base) => { const f = Buffer.from(payload); let o = 0; while (o + 2 <= f.length) { const len = f[o] | (f[o + 1] << 8); o += 2; const op = f[o]; st[op] = (st[op] || 0) + 1; if (op === base) { const g = f[o + (base === 0x83 ? 5 : 1)]; inst[g] = (inst[g] || 0) + 1; } o += len; } };
  p.on("websocket", (ws) => { ws.on("framesent", (f) => count(p.stats.sent, p.stats.instSent, f.payload, 2)); ws.on("framereceived", (f) => count(p.stats.recv, p.stats.instRecv, f.payload, 0x83));
  });
  p.wsFrames = { sent: 0, sentText: 0, recv: 0 }; // frames this page sent to / got from the relay = messages the Durable Object receives / sends
  p.on("websocket", (ws) => { ws.on("framesent", (f) => { p.wsFrames.sent++; if (typeof f.payload === "string") p.wsFrames.sentText++; }); ws.on("framereceived", () => { p.wsFrames.recv++; }); });
  p.on("websocket", (ws) => { console.log(`[${name}] ws open`, ws.url()); ws.on("framesent", (f) => { if (process.env.WSLOG) console.log(`[${name}] >>`, Buffer.from(f.payload).toString("hex").slice(0, 120)); }); ws.on("framereceived", (f) => { if (process.env.WSLOG) console.log(`[${name}] <<`, Buffer.from(f.payload).toString("hex").slice(0, 120)); }); ws.on("close", () => console.log(`[${name}] ws close`)); });
  await p.goto(base);
  await p.waitForFunction(() => document.querySelector("#loading").style.display === "none", null, { timeout: 120000 });
  await p.waitForTimeout(1500);
  // A fresh profile is asked for a nickname before hosting / joining (phase 3). Tests that are not about that skip the prompt.
  if (!opts.fresh) await p.evaluate(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugSkipNick", ""));
  p.click_ = async (x, y) => { const r = await p.evaluate(() => { const r = document.querySelector("#unity-canvas").getBoundingClientRect(); return { x: r.x, y: r.y }; }); await p.mouse.move(r.x + x, r.y + y); await p.waitForTimeout(80); await p.mouse.down(); await p.waitForTimeout(80); await p.mouse.up(); await p.waitForTimeout(400); };
  p.dump = async () => { const n = p.logs.length; await p.evaluate(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugDump", "")); for (let i = 0; i < 40; i++) { await p.waitForTimeout(100); const l = p.logs.slice(n).find((x) => x.startsWith("ZBDUMP ")); if (l) return JSON.parse(l.slice(7)); } return null; };
  const ask = async (msg, prefix, tries = 60) => { const n = p.logs.length; await p.evaluate((m) => window.unityInstance.SendMessage("ZbNetRunner", m, ""), msg); for (let i = 0; i < tries; i++) { await p.waitForTimeout(100); const l = p.logs.slice(n).find((x) => x.startsWith(prefix)); if (l) return JSON.parse(l.slice(prefix.length)); } return null; };
  p.menu = () => ask("DebugMenu", "ZBMENU ");                  // phase 3: menu state, countdown, server list as the game sees it
  p.prefs = () => ask("DebugPrefs", "ZBPREFS ");               // what the game holds of its saved settings (nick, volume, fov, invert Y, quality ...)
  p.reloadGame = async (skipNick = true) => {                   // F5: same browser profile, so PlayerPrefs come back from IndexedDB
    await p.reload();
    await p.waitForFunction(() => document.querySelector("#loading").style.display === "none", null, { timeout: 120000 });
    await p.waitForTimeout(1500);
    if (skipNick) await p.evaluate(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugSkipNick", ""));
  };
  p.statsDump = () => ask("StatsDump", "ZBSTATS ");           // cumulative traffic / rtt / interpolation counters of this page
  p.sampleStart = () => p.evaluate(() => window.unityInstance.SendMessage("ZbNetRunner", "SampleStart", ""));
  p.sampleStop = async () => { // per-frame positions of every player / zombie that moved: [{id, mine, z, t0, p:[dt,x,z,...]}]
    const n = p.logs.length; await ask("SampleStop", "ZBSAMPLE ", 300);
    const m = new Map();
    for (const l of p.logs.slice(n)) { if (!l.startsWith("ZBT ")) continue; const [h, d] = l.split(" | "); const f = h.split(" "); const k = f[1]; if (!m.has(k)) m.set(k, { id: Number(k), mine: Number(f[2]), z: Number(f[3]), t0: Number(f[4]), p: [] }); for (const v of d.split(",")) m.get(k).p.push(Number(v)); }
    return [...m.values()];
  };
  p.rtc = () => p.evaluate(() => (window.zbrtc ? window.zbrtc.info() : null)); // WebRTC lane state of this page (zbrtc.js)
  p.shot = async (f) => { await p.locator("#unity-canvas").screenshot({ path: f }); };
  return p;
}

// ---- phase 4 helpers (same click paths as e2e.mjs) -------------------------------------------------------------------------------
// A creates a listed room (the original menu: Play > Create server > name > Start); resolves with the room code from /api/rooms.
export async function createRoom(A, base, name = "P4 Room") {
  await A.click_(750, 281); await A.click_(750, 331);
  await A.click_(850, 185); await A.keyboard.type(name, { delay: 30 });
  await A.click_(750, 331); await A.click_(750, 481);
  await A.waitForTimeout(5000);
  const rooms = await (await fetch(new URL("/api/rooms", base))).json();
  if (!rooms.some((x) => x.name === name)) console.log("createRoom: rooms are", JSON.stringify(rooms.map((x) => [x.code, x.name, x.age])));
  const r = rooms.filter((x) => x.name === name).sort((a, b) => a.age - b.age)[0]; // newest (older runs may linger for a while)
  return r ? r.code : null;
}
// B joins the first room of the server list.
export async function joinFromList(B) {
  await B.click_(750, 281); await B.click_(750, 381); await B.waitForTimeout(1500);
  await B.click_(400, 45); await B.click_(750, 561); await B.waitForTimeout(5000);
}
// join by room code (Direct IP)
export async function joinByCode(C, code) {
  await C.click_(750, 281); await C.click_(750, 431); await C.waitForTimeout(800);
  await C.click_(850, 265); await C.keyboard.press("Control+a"); await C.keyboard.type(code, { delay: 30 }); await C.waitForTimeout(300);
  await C.click_(750, 431); await C.waitForTimeout(5000);
}
export const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
// wait until fn() (async) is truthy, polling every 300 ms
export async function until(fn, ms = 15000, step = 300) { const t0 = Date.now(); for (;;) { const v = await fn(); if (v) return v; if (Date.now() - t0 > ms) return v; await sleep(step); } }
// kill the page's WebSocket the way a network drop does (no clean close code: the relay sees 4000 and keeps the slot in limbo)
export const dropSocket = (p) => p.evaluate(() => { const s = window.__zbws; if (s && s.ws) s.ws.close(4000, "test drop"); });

export const heal = (p) => p.evaluate(() => window.unityInstance.SendMessage("ZbNetRunner", "DebugHeal", ""));
