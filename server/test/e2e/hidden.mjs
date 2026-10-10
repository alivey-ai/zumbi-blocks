// Phase 4 step 5: a hidden host tab. Playwright keeps every page "visible" (a page cannot be backgrounded in automation), so the page's visibility is
// simulated (visibilityState + visibilitychange event, exactly what the browser fires). Covers: C_VIS from the page, the relay's host state, the
// clients' "host paused" state, the host's tab-title warning and the banner on return.
import { launch, game, createRoom, joinByCode, until, sleep, heal } from "./lib.mjs";
const BASE = process.env.BASE || "http://localhost:8787/";
const b = await launch();
let pass = 0, fail = 0;
const check = (name, cond, extra = "") => { if (cond) { pass++; console.log("ok   " + name); } else { fail++; console.log("FAIL " + name + " " + extra); } };
const J = (x) => JSON.stringify(x);
const setVis = (p, v) => p.evaluate((v) => { Object.defineProperty(document, "visibilityState", { get: () => v, configurable: true }); Object.defineProperty(document, "hidden", { get: () => v !== "visible", configurable: true }); document.dispatchEvent(new Event("visibilitychange")); }, v);
const net = async (p) => (await p.menu()).net;

const A = await game(b, "A", BASE);
const code = await createRoom(A, BASE, "Hidden1");
const B = await game(b, "B", BASE); await joinByCode(B, code);
await A.click_(640, 360); await A.keyboard.press("Enter"); await B.click_(640, 360); await A.waitForTimeout(4000);
const hb = setInterval(() => { heal(A).catch(() => {}); heal(B).catch(() => {}); }, 700);
const title0 = await A.title();
check("host page knows it is hosting", await A.evaluate(() => window.zbHosting === true));

await setVis(A, "hidden");
const st = await until(async () => (await net(B)).hostState === 2, 5000, 300);
check("client learns that the host's tab is hidden (host state 2, overlay 'host paused')", st, J(await net(B)));
const info = await (await fetch(new URL("/api/room/" + code, BASE))).json();
check("room info reports hostState hidden", info.hostState === "hidden", J(info));
await sleep(2200);
const titles = new Set(); for (let i = 0; i < 4; i++) { titles.add(await A.title()); await sleep(600); }
check("the hidden host's tab title blinks a warning", [...titles].some((t) => /HOST PAUSED/.test(t)) && titles.size >= 2, J([...titles]));
const dA = await A.dump(), dB = await B.dump();
check("nobody is dropped while the host is hidden (host still has its client)", dA.remote === 1 && dB.mode === "client", J([dA.remote, dB.mode]));
await sleep(3500);
await setVis(A, "visible");
const back = await until(async () => (await net(B)).hostState === 0, 5000, 300);
check("host visible again: clients' state goes back to normal", back, J(await net(B)));
const banner = await A.evaluate(() => document.querySelector("#warning").innerText);
check("the title is restored and a banner tells the host what happened", (await A.title()) === title0 && /background for/.test(banner), J([await A.title(), banner]));
check("room info back to ok", (await (await fetch(new URL("/api/room/" + code, BASE))).json()).hostState === "ok");

// a hidden client is announced to nobody but is never dropped
await setVis(B, "hidden"); await sleep(1500);
check("hidden client is not dropped and the host sees no change", (await A.dump()).remote === 1 && (await net(A)).hostState === 0, J(await A.dump()));
await setVis(B, "visible");

clearInterval(hb);
console.log(`${pass} passed, ${fail} failed`);
await Promise.race([b.close(), new Promise((r) => setTimeout(r, 3000))]);
process.exit(fail ? 1 : 0);
