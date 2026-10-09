// Zumbi Blocks Worker: static WebGL build + room list + WebSocket relay (Durable Object per room).
export { Room } from "./room.js";
export { Lobby } from "./lobby.js";

const CODE_ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no I, O, 0, 1
const CODE_RE = /^[A-Z0-9]{3,8}$/;

function newCode() {
  const b = crypto.getRandomValues(new Uint8Array(5));
  let s = "";
  for (const x of b) s += CODE_ALPHABET[x % CODE_ALPHABET.length];
  return s;
}

const TYPES = { ".wasm": "application/wasm", ".js": "application/javascript", ".data": "application/octet-stream", ".json": "application/json" };

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    const path = url.pathname;

    if (path === "/api/rooms") {
      const lobby = env.LOBBY.get(env.LOBBY.idFromName("lobby"));
      const rooms = await lobby.list();
      return Response.json(rooms, { headers: { "cache-control": "no-store" } });
    }

    if (path === "/ws/host" || path.startsWith("/ws/room/")) {
      if (request.headers.get("Upgrade") !== "websocket") return new Response("Expected WebSocket", { status: 426 });
      const ip = request.headers.get("CF-Connecting-IP") || "0.0.0.0";
      const fwd = (code, host) => {
        const stub = env.ROOMS.get(env.ROOMS.idFromName(code));
        const u = new URL(request.url);
        u.searchParams.set("code", code);
        u.searchParams.set("host", host ? "1" : "0");
        u.searchParams.set("ipid", ip);
        return stub.fetch(new Request(u, request));
      };
      if (path === "/ws/host") {
        for (let i = 0; i < 6; i++) {
          const res = await fwd(newCode(), true);
          if (res.status !== 409) return res;
        }
        return new Response("Could not allocate a room code", { status: 503 });
      }
      const code = decodeURIComponent(path.slice("/ws/room/".length)).toUpperCase().replace(/\s+/g, "");
      if (!CODE_RE.test(code)) return new Response("Bad room code", { status: 400 });
      return fwd(code, false);
    }

    if (path.startsWith("/Build/")) {
      // Unity build is gzip compressed (*.gz; the raw wasm exceeds the 25 MiB asset limit). Static assets would serve
      // those as opaque files, so add the Content-Encoding and the type of the uncompressed file.
      const res = await env.ASSETS.fetch(request);
      const enc = path.endsWith(".gz") ? "gzip" : path.endsWith(".br") ? "br" : null;
      if (enc && res.ok) {
        const inner = path.slice(0, path.lastIndexOf("."));
        const ext = inner.slice(inner.lastIndexOf("."));
        const h = new Headers(res.headers);
        h.set("Content-Encoding", enc);
        h.set("Content-Type", TYPES[ext] || "application/octet-stream");
        h.set("Cache-Control", "public, max-age=0, must-revalidate");
        // encodeBody "manual": the body is already encoded, the runtime must not compress it again.
        return new Response(res.body, { status: res.status, headers: h, encodeBody: "manual" });
      }
      return res;
    }

    return env.ASSETS.fetch(request);
  },
};
