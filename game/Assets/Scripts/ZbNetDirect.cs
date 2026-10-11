// Phase 2b (docs/MULTIPLAYER.md "Data channel lane", "Free plan budget"): a client with a healthy WebRTC lane to the host sends everything to the
// host directly (state on the unreliable "zb-fast" channel, RPC / INST / DESTROY on the reliable ordered "zb-rel" channel), and the host sends its
// RPC / INST / DESTROY to that client over "zb-rel" too. The host is the gateway of its direct clients: it applies what they send and passes it on
// (to other direct clients over their channels, to everybody else through the relay with C_FWD). The relay keeps doing what only it can do: the
// buffer of buffered operations for late joiners and resumes (the host still sends every buffered operation to it, batched at a low rate), and
// all traffic of peers without a direct connection (strict NAT: STUN only, no TURN). Incoming WebSocket messages are what the Durable Object free
// plan counts, so a room whose clients are all direct costs about 2 frames a second instead of 25.
//
// Per peer and direction ("link"), the sender decides the path of every message, so each message travels exactly once:
//   Ws      -> Fencing: the reliable channel is up. The sender puts C_FENCE on the WebSocket and holds everything for that peer; the relay delivers
//              S_FENCE after everything sent before it; the receiver answers FENCE_ACK over the channel; the held messages go over the channel.
//              (The host also tells the relay to stop forwarding its messages to that client: C_SKIP, in order with its other messages.)
//   Fencing -> Dc:      FENCE_ACK arrived.           Fencing -> Ws: no ack within 4 s or the channel went down; the held messages go over the relay.
//   Dc      -> Ws:      the channel went down (probe watchdog, closed, cannot drain). Every message the receiver has not acknowledged is sent again
//              through the relay with its sequence number (S_TAG); the receiver drops the ones it already had.
// Reliable channel message: u32 sequence number of the first item, then items (u16 length + body); bodies are client messages (C_ form) from a
// client and relay style messages (S_ form, with the original sender) from the host; control items D_ACK / D_FENCE_ACK carry no sequence number.
using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class ZbNet
{
    const byte C_SKIP = 21, C_FWD = 22, C_FENCE = 23, C_UP = 24;
    const byte S_FENCE = 0x97, S_TAG = 0x98;
    const byte D_ACK = 0xA0, D_FENCE_ACK = 0xA1;
    const float FENCE_TIMEOUT = 4f, RETRY_AFTER_ABORT = 10f, RETRY_AFTER_DEMOTE = 5f, ACK_EVERY = 0.1f;
    const int REL_CHUNK = 60000, UNACKED_MAX = 30000, UNACKED_BYTES = 8 * 1024 * 1024, DEAD_MAX = 50000;

    enum Dir { Ws = 0, Fencing = 1, Dc = 2 }
    struct RelMsg { public uint seq; public int from; public byte[] body; } // body: client message (C_ form); from: original sender (host: forwarded)
    sealed class Link
    {
        public int id; public Dir dir; public uint nextSeq = 1, maxSeen, ackSent, fenceId; public float fenceAt, retryAt, ackAt;
        public bool sendFail;
        public readonly List<RelMsg> unacked = new List<RelMsg>(); public int unackedBytes;
        public readonly List<RelMsg> hold = new List<RelMsg>();
        public readonly List<byte[]> pend = new List<byte[]>(); public uint pendFirst;   // wire items of this frame (sequenced)
        public readonly List<byte[]> ctl = new List<byte[]>();                          // control items of this frame
        public readonly List<RelMsg> fwdState = new List<RelMsg>();                     // host: client state to pass on over zb-fast (seq = sender clock)
        public long txMsgs, rxMsgs, resent, dups;
    }
    static readonly Dictionary<int, Link> links = new Dictionary<int, Link>();
    static uint fenceCounter;
    static readonly HashSet<long> gwDead = new HashSet<long>(); static readonly Queue<long> gwDeadQ = new Queue<long>();
    static bool skipDirty; static float skipSentAt; static int skipSentCount;
    static bool tagPending; static uint tagSeq; static int tagFrom;
    static readonly List<byte[]> deferredWs = new List<byte[]>(); // resends that could not go out while the WebSocket was reconnecting
    public static long gwForwarded, gwSwallowed, promotions, demotions, fenceAborts;

    static bool DirectOn { get { return ZbCfg.p2p && ZbCfg.rel; } }
    static Link LinkOf(int id) { Link l; if (!links.TryGetValue(id, out l)) { l = new Link { id = id }; links[id] = l; } return l; }
    static bool IsDirect(int id) { Link l; return links.TryGetValue(id, out l) && l.dir == Dir.Dc; }
    public static bool DirectTo(int id) { return IsDirect(id); }

    static void ResetDirect()
    {
        links.Clear(); gwDead.Clear(); gwDeadQ.Clear(); skipDirty = false; skipSentCount = 0; tagPending = false; deferredWs.Clear();
    }
    static void DropLink(int id)
    {
        Link l;
        if (!links.TryGetValue(id, out l)) return;
        if (l.dir != Dir.Ws) skipDirty = true;
        links.Remove(id);
    }

    static byte[] Copy(byte[] b, int n) { var c = new byte[n]; Buffer.BlockCopy(b, 0, c, 0, n); return c; }
    static bool IsRelOp(byte op) { return op == C_RPC || op == C_INST || op == C_DESTROY || op == C_REMOVE_RPCS || op == C_DESTROY_PLAYER; }
    static bool BookOp(byte[] b) { byte op = b[0]; return op == C_INST || op == C_DESTROY || op == C_REMOVE_RPCS || op == C_DESTROY_PLAYER || (op == C_RPC && (b[1] == 5 || b[1] == 6)); }
    static int RdI32(byte[] b, int o) { return b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24); }
    static long RdI64(byte[] b, int o) { return ((long)RdI32(b, o + 4) << 32) | (uint)RdI32(b, o); }
    static int OwnerOfView(long v) { return v >= SCENE_BASE ? 0 : (int)(v / OWNER_UNIT) - 1; }

    // Client message (C_ form) -> what the relay would deliver (S_ form) for sender `from`
    static byte[] ToS(int from, byte[] c)
    {
        if (c[0] == C_DESTROY) { var d = new byte[9]; d[0] = S_DESTROY; Buffer.BlockCopy(c, 1, d, 1, 8); return d; }
        var s = new byte[c.Length + 4];
        s[0] = c[0] == C_RPC ? S_RPC : S_INST;
        s[1] = (byte)from; s[2] = (byte)(from >> 8); s[3] = (byte)(from >> 16); s[4] = (byte)(from >> 24);
        Buffer.BlockCopy(c, 1, s, 5, c.Length - 1);
        return s;
    }
    // S_ form at buf[o..o+len) -> C_ form (null when not a reliable lane message)
    static byte[] ToC(byte[] buf, int o, int len)
    {
        byte op = buf[o];
        if (op == S_DESTROY && len >= 9) { var d = new byte[9]; d[0] = C_DESTROY; Buffer.BlockCopy(buf, o + 1, d, 1, 8); return d; }
        if ((op == S_RPC || op == S_INST) && len > 5) { var c = new byte[len - 4]; c[0] = op == S_RPC ? C_RPC : C_INST; Buffer.BlockCopy(buf, o + 5, c, 1, len - 5); return c; }
        return null;
    }

    // ---- outgoing --------------------------------------------------------------------------------------------------------------------
    // Called by Queue() with the message in `w`. True = handled here (over the channel / held / fanned out), false = plain WebSocket path.
    static bool RouteOwn()
    {
        if (!DirectOn || links.Count == 0 || !IsRelOp(w.b[0])) return false;
        if (mode == Mode.Client)
        {
            Link l;
            if (!links.TryGetValue(0, out l) || l.dir == Dir.Ws) return false;
            var body = Copy(w.b, w.n);
            if (l.dir == Dir.Fencing) l.hold.Add(new RelMsg { from = localId, body = body }); else AppendRel(l, localId, body);
            return true;
        }
        if (mode != Mode.Host) return false;
        var cur = Copy(w.b, w.n);
        if (KillGate(cur)) return true;
        Fanout(0, cur);
        return true;
    }

    // host: a reliable message of `from` (0 = the host itself) goes to every client except `from`: over the channel to the direct ones (held while
    // fencing), through the relay to the others; buffered operations always go to the relay too (its buffer serves late joiners and resumes).
    static void Fanout(int from, byte[] body)
    {
        bool book = BookOp(body);
        bool needWs = book;
        if (body[0] != C_REMOVE_RPCS && body[0] != C_DESTROY_PLAYER)
            foreach (var id in playerIp.Keys)
            {
                if (id == 0 || id == from) continue;
                Link l;
                if (links.TryGetValue(id, out l) && l.dir == Dir.Dc) AppendRel(l, from, body);
                else if (l != null && l.dir == Dir.Fencing) l.hold.Add(new RelMsg { from = from, body = body });
                else needWs = true;
            }
        if (!needWs) return;
        if (from == 0) QueueWsBytes(body);
        else QueueWsBytes(Fwd(from, -1, book, false, 0, false, 0, body));
    }

    static byte[] Fwd(int asSender, int only, bool book, bool hasStamp, uint stamp, bool hasTag, uint tag, byte[] inner)
    {
        var x = new byte[10 + (hasStamp ? 4 : 0) + (hasTag ? 4 : 0) + inner.Length];
        x[0] = C_FWD; PutI32(x, 1, asSender); PutI32(x, 5, only);
        x[9] = (byte)((book ? 1 : 0) | (hasStamp ? 2 : 0) | (hasTag ? 4 : 0));
        int o = 10;
        if (hasStamp) { PutI32(x, o, (int)stamp); o += 4; }
        if (hasTag) { PutI32(x, o, (int)tag); o += 4; }
        Buffer.BlockCopy(inner, 0, x, o, inner.Length);
        return x;
    }
    static void PutI32(byte[] a, int o, int v) { a[o] = (byte)v; a[o + 1] = (byte)(v >> 8); a[o + 2] = (byte)(v >> 16); a[o + 3] = (byte)(v >> 24); }

    // the same message through the plain WebSocket path (counted for the resume resend when it is a buffered operation)
    static void QueueWsBytes(byte[] body)
    {
        if (reconnecting && !IsCounted(body)) { deferredWs.Add(body); return; } // would be lost: send it after the resume
        if (w.b.Length < body.Length) Array.Resize(ref w.b, body.Length * 2);
        Buffer.BlockCopy(body, 0, w.b, 0, body.Length); w.n = body.Length;
        QueueWs();
    }
    static bool IsCounted(byte[] b) { return BookOp(b) || (b[0] == C_FWD && b.Length > 9 && (b[9] & 1) != 0); }

    // Kills of temporary objects: every peer's OnDestroy sends killmebuffered again. The relay swallows the echoes for what it routes; the host does
    // the same for what it passes on itself (the first kill of an object goes everywhere, the rest nowhere).
    static bool KillGate(byte[] c)
    {
        if (c[0] == C_DESTROY && c.Length >= 9) { MarkDead(RootOf(RdI64(c, 1))); return false; }
        if (c[0] != C_RPC || c.Length < 11 || (c[1] != 5 && c[1] != 6)) return false;
        int nl = c[10];
        if (nl != 14 || c.Length < 11 + nl || System.Text.Encoding.ASCII.GetString(c, 11, nl) != "killmebuffered") return false;
        long root = RootOf(RdI64(c, 2));
        if (gwDead.Contains(root)) { gwSwallowed++; return true; }
        MarkDead(root);
        return false;
    }
    static void MarkDead(long root)
    {
        if (!gwDead.Add(root)) return;
        gwDeadQ.Enqueue(root);
        while (gwDeadQ.Count > DEAD_MAX) gwDead.Remove(gwDeadQ.Dequeue());
    }
    // host: a killmebuffered that came through the relay (from a peer without a channel) also counts
    static void NoteRelayKill(long view, string name, int mode)
    {
        if ((mode == 5 || mode == 6) && name == "killmebuffered") MarkDead(RootOf(view));
    }

    static void AppendRel(Link l, int from, byte[] body)
    {
        var m = new RelMsg { seq = l.nextSeq++, from = from, body = body };
        l.unacked.Add(m); l.unackedBytes += body.Length;
        if (l.pend.Count == 0) l.pendFirst = m.seq;
        l.pend.Add(mode == Mode.Host ? ToS(from, body) : body);
        l.txMsgs++;
    }
    static void Ctl(Link l, byte op, uint v) { var c = new byte[5]; c[0] = op; PutI32(c, 1, (int)v); l.ctl.Add(c); }

    // every rendered frame (the channel is not billed): one data channel message per link with this frame's items
    static readonly byte[] relOut = new byte[REL_CHUNK + 8192];
    static void FlushRel()
    {
        if (links.Count == 0) return;
        foreach (var l in links.Values)
        {
            if (l.pend.Count == 0 && l.ctl.Count == 0) { FlushFwdState(l); continue; }
            uint seq = l.pendFirst; int i = 0;
            while (i < l.pend.Count || l.ctl.Count > 0)
            {
                int n = 0;
                PutI32(relOut, 0, (int)seq); n = 4;
                while (i < l.pend.Count && (n == 4 || n + 2 + l.pend[i].Length <= REL_CHUNK) && n + 2 + l.pend[i].Length <= relOut.Length)
                {
                    var b = l.pend[i++]; relOut[n++] = (byte)b.Length; relOut[n++] = (byte)(b.Length >> 8); Buffer.BlockCopy(b, 0, relOut, n, b.Length); n += b.Length; seq++;
                }
                if (i >= l.pend.Count) { foreach (var c in l.ctl) { relOut[n++] = (byte)c.Length; relOut[n++] = 0; Buffer.BlockCopy(c, 0, relOut, n, c.Length); n += c.Length; } l.ctl.Clear(); }
                if (!ZbRtc.SendRel(l.id, relOut, n)) l.sendFail = true;
                else { ZbSync.dcTxFrames++; ZbSync.dcTxBytes += n; }
            }
            l.pend.Clear();
            FlushFwdState(l);
        }
    }

    // ---- per tick: promotion / demotion, acks, the relay's skip list ----------------------------------------------------------------
    static void UpdateDirect()
    {
        if (!DirectOn || !Connected) return;
        float now = Time.unscaledTime;
        if (mode == Mode.Host) { foreach (var id in playerIp.Keys) if (id != 0) Step(LinkOf(id), now); }
        else Step(LinkOf(0), now);
        foreach (var l in links.Values)
            if (l.maxSeen != l.ackSent && now - l.ackAt >= ACK_EVERY) { Ctl(l, D_ACK, l.maxSeen); l.ackSent = l.maxSeen; l.ackAt = now; }
        if (mode == Mode.Host && !reconnecting && (skipDirty || (skipSentCount > 0 && now - skipSentAt > 15f))) SendSkip();
    }

    static void Step(Link l, float now)
    {
        bool ok = ZbRtc.RelUp(l.id) && !l.sendFail;
        switch (l.dir)
        {
            case Dir.Ws:
                if (ok && now >= l.retryAt && !reconnecting) StartFence(l, now);
                break;
            case Dir.Fencing:
                if (!ok || now - l.fenceAt > FENCE_TIMEOUT) AbortFence(l, now);
                break;
            case Dir.Dc:
                if (!ok || l.unacked.Count > UNACKED_MAX || l.unackedBytes > UNACKED_BYTES) Demote(l, now);
                break;
        }
    }

    static void StartFence(Link l, float now)
    {
        l.dir = Dir.Fencing; l.fenceId = ++fenceCounter; l.fenceAt = now; l.hold.Clear();
        if (mode == Mode.Host) SendSkip(); // the relay stops forwarding the host's messages to this client from here on
        var f = new byte[9]; f[0] = C_FENCE; PutI32(f, 1, mode == Mode.Host ? l.id : 0); PutI32(f, 5, (int)l.fenceId);
        QueueWsBytes(f);
        nextFlush = 0f;
    }

    static void OnFenceAck(Link l, uint id)
    {
        if (l.dir != Dir.Fencing || id != l.fenceId) return;
        l.dir = Dir.Dc; promotions++;
        foreach (var m in l.hold) AppendRel(l, m.from, m.body);
        l.hold.Clear();
        Debug.Log("[zbnet] direct lane to " + l.id + " up (fence " + id + ")");
    }

    static void AbortFence(Link l, float now)
    {
        l.dir = Dir.Ws; l.retryAt = now + RETRY_AFTER_ABORT; fenceAborts++;
        if (mode == Mode.Host)
        {
            SendSkip();
            foreach (var m in l.hold) QueueWsBytes(Fwd(m.from, l.id, false, false, 0, false, 0, m.body));
        }
        else foreach (var m in l.hold) QueueWsBytes(m.body);
        l.hold.Clear();
        Debug.Log("[zbnet] direct lane to " + l.id + ": fence aborted, staying on the relay");
    }

    static void Demote(Link l, float now)
    {
        l.dir = Dir.Ws; l.retryAt = now + RETRY_AFTER_DEMOTE; l.sendFail = false; demotions++;
        if (mode == Mode.Host) SendSkip();
        foreach (var m in l.unacked)
        {
            if (mode == Mode.Host) QueueWsBytes(Fwd(m.from, l.id, false, false, 0, true, m.seq, m.body));
            else { var u = new byte[5 + m.body.Length]; u[0] = C_UP; PutI32(u, 1, (int)m.seq); Buffer.BlockCopy(m.body, 0, u, 5, m.body.Length); QueueWsBytes(u); }
            l.resent++;
        }
        Debug.Log("[zbnet] direct lane to " + l.id + " down: " + l.unacked.Count + " unacknowledged messages sent again through the relay");
        l.unacked.Clear(); l.unackedBytes = 0; l.pend.Clear();
        nextFlush = 0f;
    }

    static void SendSkip()
    {
        var ids = new List<int>();
        foreach (var l in links.Values) if (l.dir != Dir.Ws && l.id != 0 && playerIp.ContainsKey(l.id)) ids.Add(l.id);
        var b = new byte[2 + 4 * ids.Count]; b[0] = C_SKIP; b[1] = (byte)ids.Count;
        for (int i = 0; i < ids.Count; i++) PutI32(b, 2 + 4 * i, ids[i]);
        skipDirty = false; skipSentAt = Time.unscaledTime; skipSentCount = ids.Count;
        if (reconnecting) { skipDirty = true; return; }
        QueueWsBytes(b);
    }

    static void DropAcked(Link l, uint ack)
    {
        int k = 0;
        while (k < l.unacked.Count && l.unacked[k].seq <= ack) { l.unackedBytes -= l.unacked[k].body.Length; k++; }
        if (k > 0) l.unacked.RemoveRange(0, k);
    }

    // after a resumed WebSocket: what could not go out meanwhile
    static void DirectResumed()
    {
        if (mode == Mode.Host) skipDirty = true;
        var l = new List<byte[]>(deferredWs); deferredWs.Clear();
        foreach (var b in l) QueueWsBytes(b);
    }

    // ---- incoming ----------------------------------------------------------------------------------------------------------------------
    // a message of the reliable channel from `peer` (rx[o..o+n))
    static void ProcessRel(int peer, int n)
    {
        if (n < 4) return;
        var l = LinkOf(peer);
        uint seq = (uint)RdI32(rx, 0);
        int o = 4;
        while (o + 2 <= n)
        {
            int len = rx[o] | (rx[o + 1] << 8); o += 2;
            if (len < 1 || o + len > n) break;
            byte op = rx[o];
            if (op == D_ACK && len == 5) DropAcked(l, (uint)RdI32(rx, o + 1));
            else if (op == D_FENCE_ACK && len == 5) OnFenceAck(l, (uint)RdI32(rx, o + 1));
            else
            {
                uint s = seq++;
                if (s <= l.maxSeen) l.dups++;
                else { l.maxSeen = s; l.rxMsgs++; DeliverRel(peer, rx, o, len); }
            }
            o += len;
        }
    }

    static void DeliverRel(int peer, byte[] buf, int o, int len)
    {
        if (mode == Mode.Host) { if (peer != 0) GatewayIn(peer, Copy2(buf, o, len)); return; }
        if (peer != 0) return;
        byte op = buf[o];
        if (op != S_RPC && op != S_INST && op != S_DESTROY) return;
        bool v = ctxViaDc; ctxViaDc = false;
        int cs = ZbSync.ctxSender; bool ch = ZbSync.ctxHasStamp;
        ZbSync.ctxSender = -1; ZbSync.ctxHasStamp = false;
        try { ProcessMessage(buf, o, len); }
        finally { ctxViaDc = v; ZbSync.ctxSender = cs; ZbSync.ctxHasStamp = ch; }
    }
    static byte[] Copy2(byte[] b, int o, int n) { var c = new byte[n]; Buffer.BlockCopy(b, o, c, 0, n); return c; }

    // relay S_TAG (a resend of a channel message) in front of the message at buf[o..o+len): true = handled here (a duplicate, or on the host a
    // client's message that has to go through the gateway: the relay delivered it to the host only), false = process it normally
    static bool TaggedHandled(byte[] buf, int o, int len)
    {
        if (!tagPending) return false;
        tagPending = false;
        byte op = buf[o];
        if (op != S_RPC && op != S_INST && op != S_DESTROY) return false;
        var l = LinkOf(mode == Mode.Host ? tagFrom : 0);
        if (tagSeq <= l.maxSeen) { l.dups++; return true; }
        l.maxSeen = tagSeq; l.rxMsgs++;
        if (mode != Mode.Host || tagFrom == 0) return false;
        var c = ToC(buf, o, len);
        if (c != null) GatewayIn(tagFrom, c);
        return true;
    }

    // host: a reliable message of direct client c (C_ form): apply it here, pass it on to everybody else. The same checks as the relay.
    static void GatewayIn(int c, byte[] body)
    {
        byte op = body[0];
        if (!IsRelOp(op)) return;
        switch (op)
        {
            case C_RPC: if (body.Length < 11 || body[1] > 6 || body[1] == 3 || body[1] == 4) return; break;
            case C_INST: if (body.Length != 40 || OwnerOfView(RdI64(body, 2)) != c) return; break;
            case C_DESTROY: if (body.Length < 9 || OwnerOfView(RootOf(RdI64(body, 1))) != c) return; break;
            default: if (body.Length < 5 || RdI32(body, 1) != c) return; break;
        }
        if (KillGate(body)) return;
        gwForwarded++;
        // pass it on first (the relay would have sent it to everybody at once; the host's reactions to it must not overtake it), then apply it here
        if (!(op == C_RPC && body[1] == 0)) Fanout(c, body); // Server mode: the host only
        if (op == C_DESTROY_PLAYER)
        {
            var dead = new List<GameObject>();
            foreach (var kv in roots) if (OwnerOfView(kv.Key) == c && kv.Value != null) dead.Add(kv.Value);
            foreach (var g in dead) UnityEngine.Object.Destroy(g);
        }
        else if (op != C_REMOVE_RPCS)
        {
            var s = ToS(c, body);
            int cs = ZbSync.ctxSender; bool ch = ZbSync.ctxHasStamp; bool v = ctxViaDc;
            ZbSync.ctxSender = -1; ZbSync.ctxHasStamp = false; ctxViaDc = false;
            try { ProcessMessage(s, 0, s.Length); }
            finally { ZbSync.ctxSender = cs; ZbSync.ctxHasStamp = ch; ctxViaDc = v; }
        }
    }

    // host: state of direct client c that came over its fast channel: to the other fast clients over theirs, to the rest through the relay
    static void GatewayState(int c, uint stamp, byte[] buf, int o, int len)
    {
        bool needWs = false;
        foreach (var id in playerIp.Keys)
        {
            if (id == 0 || id == c) continue;
            if (ZbCfg.p2p && ZbRtc.PeerUp(id)) LinkOf(id).fwdState.Add(new RelMsg { seq = stamp, from = c, body = Copy2(buf, o, len) });
            else needWs = true;
        }
        if (!needWs) return;
        var inner = Copy2(buf, o, len); inner[0] = C_STATE;
        QueueWsBytes(Fwd(c, -1, false, true, stamp, false, 0, inner));
    }

    static readonly byte[] fwdOut = new byte[DC_CHUNK + 64];
    static void FlushFwdState(Link l)
    {
        if (l.fwdState.Count == 0) return;
        int n = 0, curFrom = int.MinValue; uint curStamp = 0;
        foreach (var m in l.fwdState)
        {
            if (n > 0 && n + 2 + m.body.Length > DC_CHUNK) { SendFwd(l.id, n); n = 0; curFrom = int.MinValue; }
            if (curFrom != m.from || curStamp != m.seq)
            {
                fwdOut[n++] = 9; fwdOut[n++] = 0; fwdOut[n++] = S_TIME; PutI32(fwdOut, n, m.from); n += 4; PutI32(fwdOut, n, (int)m.seq); n += 4;
                curFrom = m.from; curStamp = m.seq;
            }
            fwdOut[n++] = (byte)m.body.Length; fwdOut[n++] = (byte)(m.body.Length >> 8);
            Buffer.BlockCopy(m.body, 0, fwdOut, n, m.body.Length); n += m.body.Length;
        }
        if (n > 0) SendFwd(l.id, n);
        l.fwdState.Clear();
    }
    static void SendFwd(int id, int n) { if (ZbRtc.PeerUp(id) && ZbRtc.Send(id, fwdOut, n)) { ZbSync.dcTxFrames++; ZbSync.dcTxBytes += n; } }

    // ---- WebSocket flush rate --------------------------------------------------------------------------------------------------------
    // Host: every client direct (both channels up) -> the WebSocket only carries buffer bookkeeping and room info: 2 Hz. Client: direct -> 1 Hz.
    static bool AllDirect()
    {
        if (!DirectOn) return false;
        if (mode == Mode.Client) return IsDirect(0) && ZbRtc.PeerUp(0);
        if (mode != Mode.Host) return false;
        int n = 0;
        foreach (var id in playerIp.Keys) { if (id == 0) continue; n++; if (!IsDirect(id) || !ZbRtc.PeerUp(id)) return false; }
        return n > 0;
    }

    public static string DirectJson()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("{\"on\":").Append(DirectOn ? 1 : 0).Append(",\"all\":").Append(AllDirect() ? 1 : 0).Append(",\"fwd\":").Append(gwForwarded).Append(",\"swallowed\":").Append(gwSwallowed)
          .Append(",\"promotions\":").Append(promotions).Append(",\"demotions\":").Append(demotions).Append(",\"aborts\":").Append(fenceAborts).Append(",\"links\":{");
        bool f = true;
        foreach (var l in links.Values)
        {
            sb.Append(f ? "" : ",").Append("\"").Append(l.id).Append("\":{\"dir\":").Append((int)l.dir).Append(",\"tx\":").Append(l.txMsgs).Append(",\"rx\":").Append(l.rxMsgs)
              .Append(",\"unacked\":").Append(l.unacked.Count).Append(",\"seen\":").Append(l.maxSeen).Append(",\"resent\":").Append(l.resent).Append(",\"dups\":").Append(l.dups).Append("}");
            f = false;
        }
        return sb.Append("}}").ToString();
    }
}
