import { DurableObject } from "cloudflare:workers";

const EXPIRY_MS = 75_000;

// Room list ("master server"). Rooms heartbeat every ~25 s; entries not refreshed within 75 s disappear.
export class Lobby extends DurableObject {
  constructor(ctx, env) {
    super(ctx, env);
    this.rooms = new Map();
    this.hits = new Map(); // rate limit counters (memory only: losing them on a restart just resets the windows)
    ctx.blockConcurrencyWhile(async () => {
      const saved = await ctx.storage.get("rooms");
      if (saved) this.rooms = new Map(saved);
    });
  }

  // Fixed window counter used by the Worker for room creation and joins: true = allowed.
  hit(key, limit, windowMs) {
    const now = Date.now();
    let r = this.hits.get(key);
    if (!r || now - r.t0 >= windowMs) {
      if (this.hits.size > 20000) for (const [k, v] of this.hits) if (now - v.t0 >= 3_600_000) this.hits.delete(k);
      if (this.hits.size > 50000) this.hits.clear();
      r = { t0: now, n: 0 }; this.hits.set(key, r);
    }
    return ++r.n <= limit;
  }

  prune() {
    const now = Date.now();
    for (const [k, v] of this.rooms) if (now - v.t > EXPIRY_MS) this.rooms.delete(k);
  }

  async upsert(rec) {
    this.rooms.set(rec.code, { ...rec, t: Date.now() });
    this.prune();
    await this.ctx.storage.put("rooms", [...this.rooms]);
  }

  async remove(code) {
    if (this.rooms.delete(code)) await this.ctx.storage.put("rooms", [...this.rooms]);
  }

  // Public rows: never lat/lon (city level location of the host), only colo + country.
  // test = the caller is an automated test (cookie zbtest=1): it sees only test rooms; everybody else never sees them.
  async list(test = false) {
    this.prune();
    const now = Date.now();
    return [...this.rooms.values()].filter((r) => !!r.test === !!test).map(({ t, lat, lon, created, test: _t, ...r }) => ({ ...r, age: created ? Math.max(0, Math.round((now - created) / 1000)) : 0 }))
      .sort((a, b) => b.players - a.players);
  }

  // Quick Play: up to 3 candidates of this build that can be joined now, best first. Not full, no password, not ended; rooms that wait in
  // the lobby for more than 3 minutes (abandoned) go last. Then most players, nearest (distance computed here from the caller's lat/lon), newest.
  async quickplay(type, lat, lon, test = false) {
    this.prune();
    const now = Date.now();
    const dist = (r) => (lat || lon) && (r.lat || r.lon) ? Math.hypot(r.lat - lat, (r.lon - lon) * Math.cos((lat * Math.PI) / 180)) : 999;
    const rows = [...this.rooms.values()].filter((r) => r.type === type && !r.pw && !r.ended && r.players < r.max && r.players >= 1 && !!r.test === !!test);
    const stale = (r) => (!r.started && now - (r.created || now) > 180_000 && !r.quick ? 1 : 0);
    rows.sort((a, b) => stale(a) - stale(b) || b.players - a.players || dist(a) - dist(b) || (b.created || 0) - (a.created || 0));
    return rows.slice(0, 3).map(({ t, lat, lon, created, test: _t, ...r }) => r);
  }
}
