#!/usr/bin/env node
// Durable Object usage of today (UTC) against the Workers Free plan limit, from the Cloudflare GraphQL Analytics API.
// docs/MULTIPLAYER.md "Free plan budget".
//
//   node server/scripts/do-usage.mjs            today, per hour, per namespace (Room / Lobby)
//   node server/scripts/do-usage.mjs 2026-10-10 another UTC day
//
// Auth: CLOUDFLARE_API_TOKEN (a token with "Account Analytics: Read"), otherwise the OAuth token of `npx wrangler login`
// (~/Library/Preferences/.wrangler/config/default.toml or ~/.wrangler/config/default.toml; run any wrangler command first if it expired).
// Account: CLOUDFLARE_ACCOUNT_ID or the account_id in server/wrangler.jsonc.
//
// What the free plan counts (100,000 Durable Object requests a day, reset at 00:00 UTC): HTTP requests and RPC calls to an object, alarms,
// WebSocket upgrades, and incoming WebSocket messages at 20:1. The "requests" in the analytics are raw (one per incoming WebSocket message),
// so the script prints both the raw number and the billed estimate. Observed on 2026-10-10: the limit tripped at about 1.17-1.26 M raw requests
// (almost all WebSocket messages, ~1.2 M / 20 = ~60 k, plus ~12 k Lobby calls and the room HTTP / alarm requests).
import { readFileSync, existsSync } from "node:fs";
import { homedir } from "node:os";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const day = process.argv[2] || new Date().toISOString().slice(0, 10);
let account = process.env.CLOUDFLARE_ACCOUNT_ID;
if (!account) { const m = /"account_id"\s*:\s*"([0-9a-f]{32})"/.exec(readFileSync(join(here, "..", "wrangler.jsonc"), "utf8")); account = m && m[1]; }
let token = process.env.CLOUDFLARE_API_TOKEN;
if (!token) {
  for (const p of [join(homedir(), "Library/Preferences/.wrangler/config/default.toml"), join(homedir(), ".wrangler/config/default.toml"), join(homedir(), ".config/.wrangler/config/default.toml")]) {
    if (!existsSync(p)) continue;
    const m = /oauth_token\s*=\s*"([^"]+)"/.exec(readFileSync(p, "utf8")); if (m) { token = m[1]; break; }
  }
}
if (!token || !account) { console.error("need CLOUDFLARE_API_TOKEN (or `npx wrangler login`) and an account id"); process.exit(2); }

const q = `query($a: String!, $s: Time!, $e: Time!) { viewer { accounts(filter: { accountTag: $a }) {
  byHour: durableObjectsInvocationsAdaptiveGroups(limit: 100, filter: { datetimeHour_geq: $s, datetimeHour_lt: $e }, orderBy: [datetimeHour_ASC]) { sum { requests errors } dimensions { datetimeHour } }
  byNs: durableObjectsInvocationsAdaptiveGroups(limit: 20, filter: { datetimeHour_geq: $s, datetimeHour_lt: $e }) { sum { requests errors } dimensions { namespaceId } }
  ws: durableObjectsPeriodicGroups(limit: 20, filter: { datetimeHour_geq: $s, datetimeHour_lt: $e }) { sum { inboundWebsocketMsgCount outboundWebsocketMsgCount activeTime } dimensions { namespaceId } }
} } }`;
const start = `${day}T00:00:00Z`, end = new Date(Date.parse(start) + 86400000).toISOString();
const res = await fetch("https://api.cloudflare.com/client/v4/graphql", { method: "POST", headers: { authorization: `Bearer ${token}`, "content-type": "application/json" }, body: JSON.stringify({ query: q, variables: { a: account, s: start, e: end } }) });
const j = await res.json();
if (j.errors && j.errors.length) { console.error("GraphQL error:", JSON.stringify(j.errors).slice(0, 500)); process.exit(1); }
const acc = j.data.viewer.accounts[0];
let names = {};
try {
  const r = await fetch(`https://api.cloudflare.com/client/v4/accounts/${account}/workers/durable_objects/namespaces`, { headers: { authorization: `Bearer ${token}` } });
  for (const n of (await r.json()).result || []) names[n.id] = n.class || n.name;
} catch {}
const total = acc.byHour.reduce((a, r) => a + r.sum.requests, 0);
console.log(`Durable Object requests on ${day} (UTC), account ${account.slice(0, 6)}...`);
for (const r of acc.byHour) console.log(`  ${r.dimensions.datetimeHour.slice(11, 16)}  ${String(r.sum.requests).padStart(9)}  errors ${r.sum.errors}`);
for (const r of acc.byNs) console.log(`  ${(names[r.dimensions.namespaceId] || r.dimensions.namespaceId).padEnd(10)} ${String(r.sum.requests).padStart(9)} requests (raw)`);
let wsIn = 0; for (const r of acc.ws || []) wsIn += r.sum.inboundWebsocketMsgCount || 0;
// inbound WebSocket messages are billed 20:1; everything else 1:1. When the periodic WebSocket counter is empty (it often is), assume the Room
// requests are WebSocket messages and the Lobby requests are RPC calls (a slight under-estimate).
const lobby = acc.byNs.filter((r) => /lobby/i.test(names[r.dimensions.namespaceId] || "")).reduce((a, r) => a + r.sum.requests, 0);
const billed = wsIn > 0 ? Math.round(wsIn / 20 + (total - wsIn)) : Math.round((total - lobby) / 20 + lobby);
console.log(`  total ${total} raw requests; billed estimate ~${billed} of 100,000 (${Math.round(billed / 1000)} %)${wsIn ? `; inbound WebSocket messages ${wsIn}` : ""}`);
console.log(`  practical limit: on 2026-10-10 the free tier refused requests from about 1.2 M raw (~74 k by the formula above), so ${Math.round(total / 12000)} % of that`);
console.log(`  resets at 00:00 UTC = ${new Date(Date.parse(end)).toLocaleString()}`);
