// Phase 4 (docs/design/PHASE3-4-MATCHMAKING-RESILIENCE.md): the session survives a dropped WebSocket.
//   * protocol 3: the relay keeps a player's slot ("limbo") for a grace period (20 s clients, 15 s host) after the socket dies without a clean close;
//   * this file reopens the socket with the token from the last WELCOME (`rejoin=ID.TOKEN&seq=N`) WITHOUT going through StartClient (that resets the
//     whole session), the resumed WELCOME does not run OnConnectedToServer, the relay replays the buffered entries this peer missed, and the buffered
//     operations this peer sent that the relay never parsed are sent again (count based, so nothing is applied twice);
//   * host states (connection lost / tab hidden / new match being set up) for the overlay and for the "host left" screen (ZbMenu).
using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class ZbNet
{
    static string token = "";          // resume token of the last WELCOME ("" = protocol 2 relay or not connected)
    static int graceSec;               // how long the relay keeps our slot
    static int epoch;                  // host term of the room (a claim restart increases it)
    static uint lastSeq;               // newest buffered relay entry this peer has processed (S_SEQ)
    static uint sentBuffered;          // buffered operations this peer queued in this session (the relay counts the ones it parsed)
    const int RING_MAX = 4096, RING_BYTES = 512 * 1024;
    static readonly List<byte[]> ring = new List<byte[]>(); static int ringBytes;   // the newest buffered operations, for the resend

    public static bool reconnecting;   // socket lost, trying to take the slot back; the game keeps running meanwhile
    static bool resumeSent; static float reconnectStart, nextAttempt, attemptStart; static int attempt;
    static bool welcomeResumed; static uint welcomeAccepted; static int welcomeFlags;
    static string claimCode = "";

    public static int lastEndReason;   // reject reason that ended the last session (4 host left, 5 kicked, ...); 0 = connection lost / own leave
    public const int R_QUOTA = 11;     // the Worker refused the socket: the free plan's daily Durable Object quota is used up (until 00:00 UTC)
    public static int hostState;       // S_HOST_STATE: 0 ok, 1 host connection lost, 2 host tab hidden, 3 host left (restarting)
    static float hostStateUntil;
    public static bool restartPending; // the relay announced that a new match is being set up (the host left)
    public struct Lost { public string code; public int id; public string token; public int epoch; public bool wasHost; public bool restart; public int reason; }
    public static Lost lost;           // the session that just ended (ResetSession wipes id / token / code)
    public static int Epoch { get { return epoch; } }
    public static string Token { get { return token; } }
    public static uint SentBuffered { get { return sentBuffered; } }
    public static uint LastSeq { get { return lastSeq; } }
    public static int GraceSec { get { return graceSec; } }
    public static int Attempts { get { return attempt; } }

    static void ResetResume()
    {
        token = ""; graceSec = 0; lastSeq = 0; sentBuffered = 0; ring.Clear(); ringBytes = 0;
        reconnecting = false; resumeSent = false; welcomeResumed = false; hostState = 0; restartPending = false;
    }

    // called by Queue() before anything else
    static void NoteBuffered()
    {
        byte op = w.b[0];
        if (!(op == C_INST || op == C_DESTROY || op == C_REMOVE_RPCS || op == C_DESTROY_PLAYER || (op == C_RPC && (w.b[1] == 5 || w.b[1] == 6)) || (op == C_FWD && w.n > 9 && (w.b[9] & 1) != 0))) return;
        sentBuffered++;
        var copy = new byte[w.n]; Buffer.BlockCopy(w.b, 0, copy, 0, w.n);
        ring.Add(copy); ringBytes += copy.Length;
        while (ring.Count > RING_MAX || ringBytes > RING_BYTES) { ringBytes -= ring[0].Length; ring.RemoveAt(0); }
    }

    static void QueueRaw(byte[] body)
    {
        if (w.b.Length < body.Length) Array.Resize(ref w.b, body.Length * 2);
        Buffer.BlockCopy(body, 0, w.b, 0, body.Length); w.n = body.Length;
        QueueOut(); // not counted again: the relay's count and ours are brought in line after the resend
    }

    // WELCOME of protocol 3: str8 token, u8 graceSec, u8 epoch, u8 flags (1 resumed, 2 restart claim), i32 hostId, u32 acceptedBufferedOps
    static bool ReadWelcomeExt(ZbR r)
    {
        welcomeResumed = false;
        if (r.o >= r.end) { token = ""; graceSec = 0; return false; }
        string tok = r.Str8(); int grace = r.U8(); int ep = r.U8(); int flags = r.U8(); r.I32(); uint accepted = (uint)r.I32();
        if (!r.Ok) return false;
        token = tok; graceSec = grace; epoch = ep; welcomeFlags = flags; welcomeAccepted = accepted;
        welcomeResumed = (flags & 1) != 0 && reconnecting;
        ZbWs.StoreResume(roomCode + "." + localId + "." + token);
        return true;
    }

    // "&stale=ID.TOKEN" for a fresh join: the previous incarnation of this tab (crashed / reloaded) may have left a slot in limbo; the relay frees it.
    static string StaleParam(string code)
    {
        string v = ZbWs.LoadResume() ?? "";
        if (v.Length == 0) return "";
        ZbWs.StoreResume("");
        var p = v.Split('.');
        if (p.Length != 3 || !string.Equals(p[0], code, StringComparison.OrdinalIgnoreCase)) return "";
        return "&stale=" + Uri.EscapeDataString(p[1] + "." + p[2]);
    }

    static bool CanResume()
    {
        return ZbCfg.resume && (mode == Mode.Host || mode == Mode.Client) && token.Length > 0 && graceSec > 0 && roomCode.Length > 0;
    }

    static void BeginReconnect()
    {
        reconnecting = true; resumeSent = false; attempt = 0;
        reconnectStart = Time.unscaledTime; nextAttempt = reconnectStart + 0.4f;
        outLen = 0; fastLen = 0; // unflushed bytes: unbuffered ones are lost, buffered ones are in the ring
        Debug.Log("[zbnet] connection lost, trying to resume (grace " + graceSec + " s, seq " + lastSeq + ", sent " + sentBuffered + ")");
    }

    static readonly float[] backoff = { 0.4f, 0.8f, 1.6f, 3f };
    static void TickReconnect(int st)
    {
        float now = Time.unscaledTime;
        if (now > reconnectStart + graceSec + 1f) { FailReconnect(); return; }
        if (resumeSent)
        {
            if (st == 2) { ReadFrames(); return; }   // a resumed WELCOME ends the state; the replay follows in the next frames
            if (st == 3 || st == 0 || now - attemptStart > 6f)
            {
                ZbWs.Close(); resumeSent = false;
                nextAttempt = now + backoff[Math.Min(attempt - 1, backoff.Length - 1)];
            }
            return;
        }
        if (now < nextAttempt) return;
        attempt++; attemptStart = now; resumeSent = true;
        ZbWs.Open("/ws/room/" + Uri.EscapeDataString(roomCode) + "?v=" + PROTO + "&rejoin=" + localId + "." + Uri.EscapeDataString(token) + "&seq=" + lastSeq + (ZbCfg.p2p ? "&rtc=1" : ""), true);
        Debug.Log("[zbnet] resume attempt " + attempt);
    }

    // the resumed WELCOME arrived
    static void OnResumed()
    {
        reconnecting = false; resumeSent = false; hostState = 0;
        int resent = 0;
        if (welcomeAccepted != 0xFFFFFFFFu)
        {
            long missing = (long)sentBuffered - welcomeAccepted;
            if (missing > 0)
            {
                int n = (int)Math.Min(missing, ring.Count);
                for (int i = ring.Count - n; i < ring.Count; i++) QueueRaw(ring[i]);
                resent = n; sentBuffered = welcomeAccepted + (uint)n;
                if (n < missing) Debug.LogWarning("[zbnet] resume: " + (missing - n) + " buffered operations were older than the resend ring and are lost");
            }
            else if (missing < 0) sentBuffered = welcomeAccepted;
        }
        nextPing = 0; nextEcho = 0;
        DirectResumed(); // phase 2b: the relay's skip list again, resends that waited for the socket
        Debug.Log("[zbnet] RESUMED id " + localId + " after " + attempt + " attempts, re-sent " + resent + " buffered operations, seq " + lastSeq);
        attempt = 0;
    }

    static void FailReconnect()
    {
        Debug.Log("[zbnet] could not resume within the grace period");
        RememberSession(0);
        reconnecting = false; resumeSent = false;
        ZbWs.Close(); ZbWs.StoreResume("");
        Network.peerType = NetworkPeerType.Disconnected;
        mode = Mode.Offline; ResetSession();
        lastEndReason = 0;
        Notify("OnDisconnectedFromServer", NetworkDisconnection.LostConnection);
    }

    static void RememberSession(int reason)
    {
        lost = new Lost { code = roomCode, id = localId, token = token, epoch = epoch, wasHost = mode == Mode.Host, restart = restartPending && reason == 4, reason = reason };
    }

    static void OnHostState(int state, int secs)
    {
        hostState = state; hostStateUntil = Time.unscaledTime + secs;
        if (state == 3) restartPending = true;
    }

    // Level B: the next host start claims the room that this player was in (/ws/host?claim=CODE.ID.TOKEN)
    public static void SetClaim(string code, int id, string tok) { claimCode = code + "." + id + "." + tok; }

    // one line for the overlay (drawn over the game), null when everything is fine
    public static string StatusText(out Color c)
    {
        c = new Color(1f, 0.85f, 0.2f);
        if (reconnecting)
        {
            int left = Mathf.Max(0, Mathf.CeilToInt(reconnectStart + graceSec - Time.unscaledTime));
            return "Connection lost - reconnecting... " + left + " s";
        }
        if (!Connected || Network.isServer) return null;
        if (hostState == 1) return "The host lost the connection - waiting for them... " + Mathf.Max(0, Mathf.CeilToInt(hostStateUntil - Time.unscaledTime)) + " s";
        if (hostState == 2) return "The host's browser tab is in the background - the game is paused until they come back";
        return null;
    }
}
