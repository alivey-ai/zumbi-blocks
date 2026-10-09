// Feel layer of the online transport (docs/MULTIPLAYER.md, "Feel"): net stats overlay, host timestamps + clock offset
// per sender, snapshot interpolation for remote players and zombies, debug/test switches from the page URL.
// The gameplay scripts are untouched: this sits between the transport (ZbNet) and the scripts' RPC methods.
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// ---------------------------------------------------------------------------------------------------------------
// Switches from the page URL (query string), read once. Meant for testing / A-B comparisons:
//   ?netsim=120,30   latency test mode (handled in ZbNet.jslib)
//   ?legacy=1        behave like the first version: no interpolation, no compact state messages, no send-rate boost
//   ?interp=0        no interpolation        ?compact=0   state sent as plain RPCs      ?rate=N   state send rate (10 = original)
//   ?delay=MS        minimum interpolation delay (default 100)        ?stats=1   overlay on at start (F3 toggles)
// ---------------------------------------------------------------------------------------------------------------
public static class ZbCfg
{
    public static bool interp = true, compact = true, statsOn;
    public static float rate = 30f;        // value for sc_player.sendrate / sc_zumbi.sendrate (the original used 10 = 8..10 updates/s)
    public static float minDelay = 100f;   // ms, interpolation delay floor
    public static string query = "";
    static bool loaded;

    public static void Load()
    {
        if (loaded) return;
        loaded = true;
        query = ZbWs.Query() ?? "";
        var q = query.TrimStart('?');
        foreach (var kv in q.Split('&'))
        {
            int i = kv.IndexOf('=');
            string k = i < 0 ? kv : kv.Substring(0, i), v = i < 0 ? "" : Uri.UnescapeDataString(kv.Substring(i + 1));
            float f;
            switch (k)
            {
                case "legacy": if (v == "1") { interp = false; compact = false; rate = 0f; } break;
                case "interp": interp = v != "0"; break;
                case "compact": compact = v != "0"; break;
                case "rate": if (float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) rate = f; break;
                case "delay": if (float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) minDelay = f; break;
                case "stats": statsOn = v == "1"; break;
            }
        }
    }
}

// ---------------------------------------------------------------------------------------------------------------
// Round trip statistics of one peer.
// ---------------------------------------------------------------------------------------------------------------
public sealed class ZbRtt
{
    public double last, min = 1e9, avg, jitter;
    public int n;
    public void Add(double v)
    {
        if (n == 0) avg = v; else { jitter += (Math.Abs(v - last) - jitter) / 8.0; avg += (v - avg) / 8.0; }
        last = v; if (v < min) min = v; n++;
    }
}

// ---------------------------------------------------------------------------------------------------------------
// Clock relation to one sender. Every frame a peer sends starts with its own clock reading (C_TIME); the receiver keeps
// the smallest "arrival - stamp" of the last seconds (the path without queueing delay) as the offset between the two
// clocks, and the RFC 3550 style interarrival jitter of that transit time.
// ---------------------------------------------------------------------------------------------------------------
public sealed class ZbClock
{
    public double offset;      // sender stamp + offset = local time of a best-case arrival (ms, performance.now base)
    public double jitter;      // ms
    public double lastQueue;   // ms the last frame was later than the best case
    public int n;
    readonly long[] binSec = new long[10];
    readonly double[] binMin = new double[10];

    public void Sample(double arrival, double stamp)
    {
        double transit = arrival - stamp;
        if (n > 0) jitter += (Math.Abs(transit - lastTransit) - jitter) / 16.0;
        lastTransit = transit; n++;
        long sec = (long)(arrival / 1000.0); int slot = (int)(sec % 10);
        if (binSec[slot] != sec) { binSec[slot] = sec; binMin[slot] = transit; } else if (transit < binMin[slot]) binMin[slot] = transit;
        double m = double.MaxValue;
        for (int i = 0; i < 10; i++) if (binSec[i] > sec - 8 && binSec[i] <= sec && binMin[i] < m) m = binMin[i];
        if (n == 1 || Math.Abs(m - offset) > 250.0) offset = m;
        else offset += Math.Max(-2.0, Math.Min(1.0, m - offset));   // follow the minimum slowly (no time jumps)
        lastQueue = transit - offset;
    }
    double lastTransit;
}

// ---------------------------------------------------------------------------------------------------------------
// Shared state of the feel layer: clocks, RTTs, traffic counters, interpolation counters.
// ---------------------------------------------------------------------------------------------------------------
public static class ZbSync
{
    // frame context while a received WebSocket frame is processed
    public static int ctxSender = -1; public static double ctxStamp; public static bool ctxHasStamp; public static double ctxArrival;

    public static readonly Dictionary<int, ZbClock> clocks = new Dictionary<int, ZbClock>();
    public static readonly Dictionary<int, ZbRtt> rtt = new Dictionary<int, ZbRtt>();
    public static readonly List<ZbInterp> interps = new List<ZbInterp>();

    // cumulative traffic (WebSocket payload bytes; messages = protocol messages inside the frames)
    public static long rxMsgs, rxBytes, rxFrames, txMsgs, txBytes, txFrames;
    public static readonly Dictionary<byte, long> txByOp = new Dictionary<byte, long>(), rxByOp = new Dictionary<byte, long>();
    // interpolation counters
    public static long snaps, framesSampled, framesExtrap, framesStarved, teleports;
    public static double sumDelay, sumBuf;

    static int nowFrame = -1; static double now;
    public static double Now { get { if (nowFrame != Time.frameCount) { nowFrame = Time.frameCount; now = ZbWs.Now(); } return now; } }

    public static ZbClock Clock(int sender)
    {
        ZbClock c;
        if (!clocks.TryGetValue(sender, out c)) { c = new ZbClock(); clocks[sender] = c; }
        return c;
    }
    public static ZbRtt Rtt(int peer)
    {
        ZbRtt r;
        if (!rtt.TryGetValue(peer, out r)) { r = new ZbRtt(); rtt[peer] = r; }
        return r;
    }

    public static void Reset()
    {
        clocks.Clear(); rtt.Clear();
        ctxSender = -1; ctxHasStamp = false;
    }

    // ---- rolling rates (last ~5 s) -----------------------------------------------------------------
    struct Snap { public double t; public long rxM, rxB, rxF, txM, txB, txF, snaps; }
    static readonly Snap[] ring = new Snap[8];
    static int ringN; static double lastRing;
    public static double rateRxMsgs, rateRxBytes, rateRxFrames, rateTxMsgs, rateTxBytes, rateTxFrames, rateSnaps;
    public static void TickRates()
    {
        double t = Time.realtimeSinceStartupAsDouble;
        if (t - lastRing < 1.0) return;
        lastRing = t;
        var s = new Snap { t = t, rxM = rxMsgs, rxB = rxBytes, rxF = rxFrames, txM = txMsgs, txB = txBytes, txF = txFrames, snaps = snaps };
        ring[ringN % ring.Length] = s; ringN++;
        int count = Math.Min(ringN, ring.Length);
        if (count < 2) return;
        var old = ring[(ringN - count) % ring.Length]; double dt = t - old.t;
        if (dt <= 0) return;
        rateRxMsgs = (s.rxM - old.rxM) / dt; rateRxBytes = (s.rxB - old.rxB) / dt; rateRxFrames = (s.rxF - old.rxF) / dt;
        rateTxMsgs = (s.txM - old.txM) / dt; rateTxBytes = (s.txB - old.txB) / dt; rateTxFrames = (s.txF - old.txF) / dt;
        rateSnaps = (s.snaps - old.snaps) / dt;
    }

    // ---- state feed (called by ZbNet for every delivered RPC / state message) -------------------------
    static float F(object o) { return o is float ? (float)o : Convert.ToSingle(o); }
    public static void TryFeed(NetworkView nv, string name, object[] args)
    {
        if (!ZbCfg.interp || nv == null || nv.isMine || args == null) return;
        float yaw; Vector3 p;
        if (name == "setzumbistuff" && args.Length == 2 && args[1] is Vector3) { yaw = F(args[0]); p = (Vector3)args[1]; }
        else if (name == "setposstuff" && args.Length == 6 && args[2] is Vector3) { yaw = F(args[0]); p = (Vector3)args[2]; }
        else return;
        var it = nv.GetComponent<ZbInterp>();
        if (it == null)
        {
            var oc = nv.GetComponent<sc_onlinecontroller>(); var zc = nv.GetComponent<sc_zumbionlinecontroller>();
            if (oc == null && zc == null) return;
            it = nv.gameObject.AddComponent<ZbInterp>(); it.Init(oc, zc);
        }
        int sender = ctxSender >= 0 ? ctxSender : nv.ownerId;
        var clock = Clock(sender);
        double t = ctxHasStamp && ctxSender == sender ? ctxStamp : (ctxArrival > 0 ? ctxArrival : Now) - clock.offset;
        snaps++;
        it.Feed(t, p, yaw, clock);
    }

    // Host side: raise the state send rate of freshly created players / zombies (the gameplay scripts keep their own counters).
    public static void Tune(GameObject go)
    {
        if (ZbCfg.rate <= 0f) return;
        var pl = go.GetComponent<sc_player>(); if (pl != null && pl.sendrate < ZbCfg.rate) pl.sendrate = ZbCfg.rate;
        var zb = go.GetComponent<sc_zumbi>(); if (zb != null && zb.sendrate < ZbCfg.rate) zb.sendrate = ZbCfg.rate;
    }
}

// ---------------------------------------------------------------------------------------------------------------
// Snapshot interpolation for one remote player / zombie. Runs in LateUpdate before the game's own online controller
// (sc_onlinecontroller / sc_zumbionlinecontroller, which chase the last received position) and writes the interpolated
// pose into transform and into the controller's target fields, so the chase has nothing left to do.
// ---------------------------------------------------------------------------------------------------------------
[DefaultExecutionOrder(-50)]
public class ZbInterp : MonoBehaviour
{
    struct S { public double t; public Vector3 p; public float yaw; }
    const int CAP = 24;
    const float TELEPORT = 6f;       // m between two snapshots = teleport / respawn: snap instead of gliding
    const double EXTRAP_MAX = 100.0; // ms of extrapolation after the newest snapshot, then hold
    const double MAX_DELAY = 350.0;

    readonly S[] buf = new S[CAP];
    int cnt;
    ZbClock clock;
    sc_onlinecontroller oc; sc_zumbionlinecontroller zc;
    double interval = 60.0;          // ms between snapshots (smoothed)
    public double delay = 100.0;     // ms we render behind (adaptive)
    bool snapNext;

    public int Buffered { get { return cnt; } }

    public void Init(sc_onlinecontroller o, sc_zumbionlinecontroller z) { oc = o; zc = z; delay = Math.Max(ZbCfg.minDelay, 100.0); }
    void OnEnable() { ZbSync.interps.Add(this); }
    void OnDisable() { ZbSync.interps.Remove(this); }

    public void Feed(double t, Vector3 p, float yaw, ZbClock c)
    {
        clock = c;
        if (cnt > 0)
        {
            S last = buf[cnt - 1];
            if (t <= last.t)
            {
                if (t < last.t - 1000.0) cnt = 0; // clock realigned: start over
                else return;                       // duplicate / older than what we have
            }
            else
            {
                double dt = t - last.t;
                interval += (Math.Min(500.0, Math.Max(5.0, dt)) - interval) * 0.1;
                if ((p - last.p).sqrMagnitude > TELEPORT * TELEPORT) { cnt = 0; snapNext = true; ZbSync.teleports++; }
            }
        }
        if (cnt == CAP) { Array.Copy(buf, 1, buf, 0, CAP - 1); cnt--; }
        buf[cnt++] = new S { t = t, p = p, yaw = yaw };
    }

    void LateUpdate()
    {
        if (cnt == 0 || clock == null) return;
        double dtMs = Math.Max(0.1, Time.unscaledDeltaTime * 1000.0);
        // adaptive delay: enough to always hold the next snapshot (interval) plus the measured jitter; moves slowly
        double target = Math.Min(MAX_DELAY, Math.Max(ZbCfg.minDelay, interval * 1.1 + 3.0 * clock.jitter + 8.0));
        double step = (target - delay) * (target > delay ? Math.Min(1.0, dtMs * 0.002) : Math.Min(1.0, dtMs * 0.0003));
        double lim = dtMs * 0.2; // never change render speed by more than 20 %
        delay += Math.Max(-lim, Math.Min(lim, step));

        double tr = ZbSync.Now - clock.offset - delay;
        int n = cnt;
        Vector3 p; float yaw;
        ZbSync.framesSampled++; ZbSync.sumDelay += delay; ZbSync.sumBuf += n;
        if (snapNext) { snapNext = false; p = buf[n - 1].p; yaw = buf[n - 1].yaw; cnt = 1; buf[0] = buf[n - 1]; n = 1; }
        else if (n == 1 || tr <= buf[0].t) { p = buf[0].p; yaw = buf[0].yaw; if (n > 1 || tr < buf[0].t - 1) ZbSync.framesStarved++; }
        else if (tr >= buf[n - 1].t)
        {
            double over = tr - buf[n - 1].t;
            p = buf[n - 1].p; yaw = buf[n - 1].yaw;
            if (n >= 2 && over > 0)
            {
                double span = buf[n - 1].t - buf[n - 2].t;
                if (span > 0.5) p += (buf[n - 1].p - buf[n - 2].p) * (float)(Math.Min(over, EXTRAP_MAX) / span);
            }
            ZbSync.framesExtrap++;
            if (n > 2) { buf[0] = buf[n - 2]; buf[1] = buf[n - 1]; cnt = 2; }
        }
        else
        {
            int i = 0; while (i + 1 < n - 1 && buf[i + 1].t <= tr) i++;
            double span = buf[i + 1].t - buf[i].t;
            float a = span > 0.001 ? (float)((tr - buf[i].t) / span) : 1f;
            p = Vector3.LerpUnclamped(buf[i].p, buf[i + 1].p, a);
            yaw = Mathf.LerpAngle(buf[i].yaw, buf[i + 1].yaw, a);
            // drop snapshots we have fully passed (keep the one at i, the segment start)
            if (i > 0) { Array.Copy(buf, i, buf, 0, cnt - i); cnt -= i; }
        }

        transform.position = p;
        var e = transform.eulerAngles; e.y = yaw; transform.eulerAngles = e;
        if (oc != null) { oc.posissao = p; oc.yang = Mathf.RoundToInt(yaw); }
        if (zc != null) { zc.posissao = p; zc.yang = Mathf.RoundToInt(yaw); }
    }
}

// ---------------------------------------------------------------------------------------------------------------
// Test hook: records the per-frame (wall clock) position of every networked player / zombie on this peer, owned and
// remote, so a test can compare what the owner did with what a remote peer rendered (see server/test/e2e/measure.mjs).
// Started/stopped through unityInstance.SendMessage("ZbNetRunner", "SampleStart" / "SampleStop", "").
// ---------------------------------------------------------------------------------------------------------------
[DefaultExecutionOrder(10000)]
public class ZbSampler : MonoBehaviour
{
    sealed class Track { public bool mine, zombie; public double t0; public List<int> dt = new List<int>(); public List<float> x = new List<float>(), z = new List<float>(); }
    readonly Dictionary<long, Track> tracks = new Dictionary<long, Track>();

    void LateUpdate()
    {
        double t = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        foreach (var z in FindObjectsByType<sc_zumbi>(FindObjectsSortMode.None)) Add(z.gameObject, t, true);
        foreach (var o in FindObjectsByType<sc_player>(FindObjectsSortMode.None)) Add(o.gameObject, t, false);
    }
    void Add(GameObject go, double t, bool zombie)
    {
        var nv = go.GetComponent<NetworkView>();
        if (nv == null) return;
        Track tr;
        if (!tracks.TryGetValue(nv.id, out tr)) { tr = new Track { t0 = t, mine = nv.isMine, zombie = zombie }; tracks[nv.id] = tr; }
        if (tr.x.Count >= 3000) return;
        var p = go.transform.position;
        tr.dt.Add((int)(t - tr.t0)); tr.x.Add(p.x); tr.z.Add(p.z);
    }

    public void Dump()
    {
        foreach (var kv in tracks)
        {
            var tr = kv.Value;
            if (tr.x.Count < 20) continue;
            float minx = float.MaxValue, maxx = float.MinValue, minz = float.MaxValue, maxz = float.MinValue;
            for (int i = 0; i < tr.x.Count; i++) { minx = Mathf.Min(minx, tr.x[i]); maxx = Mathf.Max(maxx, tr.x[i]); minz = Mathf.Min(minz, tr.z[i]); maxz = Mathf.Max(maxz, tr.z[i]); }
            if (Mathf.Max(maxx - minx, maxz - minz) < 2f) continue; // did not move: nothing to compare
            // Unity truncates long log lines: emit "ZBT id mine zombie t0 | dt,x,z,dt,x,z,..." in chunks of ~100 samples
            string head = "ZBT " + kv.Key + " " + (tr.mine ? 1 : 0) + " " + (tr.zombie ? 1 : 0) + " " + (long)tr.t0 + " | ";
            for (int c = 0; c < tr.x.Count; c += 100)
            {
                var sb = new StringBuilder(head);
                for (int i = c; i < Mathf.Min(tr.x.Count, c + 100); i++)
                    sb.Append(i > c ? "," : "").Append(tr.dt[i]).Append(',').Append(tr.x[i].ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(tr.z[i].ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                Debug.Log(sb.ToString());
            }
        }
        Debug.Log("ZBSAMPLE {}");
    }
}
