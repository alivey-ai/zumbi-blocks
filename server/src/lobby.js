import { DurableObject } from "cloudflare:workers";

const EXPIRY_MS = 75_000;

// Room list ("master server"). Rooms heartbeat every ~25 s; entries not refreshed within 75 s disappear.
export class Lobby extends DurableObject {
  constructor(ctx, env) {
    super(ctx, env);
    this.rooms = new Map();
    ctx.blockConcurrencyWhile(async () => {
      const saved = await ctx.storage.get("rooms");
      if (saved) this.rooms = new Map(saved);
    });
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

  async list() {
    this.prune();
    return [...this.rooms.values()].map(({ t, ...r }) => r).sort((a, b) => b.players - a.players);
  }
}
