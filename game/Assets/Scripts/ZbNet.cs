// Online transport behind the legacy-networking shim (LegacyNetworkShim.cs).
// One player's browser is the authoritative host (like the original RakNet server); a Cloudflare Durable Object
// relays between peers over one binary WebSocket. See docs/MULTIPLAYER.md for the protocol.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// ---------------------------------------------------------------------------------------------------------------
// Browser WebSocket bridge (see Plugins/WebGL/ZbNet.jslib). Outside a WebGL player there is no transport.
// ---------------------------------------------------------------------------------------------------------------
static class ZbWs
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void ZbWsOpen(string path);
    [DllImport("__Internal")] static extern int ZbWsState();
    [DllImport("__Internal")] static extern int ZbWsSend(byte[] data, int len);
    [DllImport("__Internal")] static extern int ZbWsRecv(byte[] buf, int cap);
    [DllImport("__Internal")] static extern int ZbWsBuffered();
    [DllImport("__Internal")] static extern void ZbWsClose();
    [DllImport("__Internal")] static extern IntPtr ZbOrigin();
    [DllImport("__Internal")] static extern IntPtr ZbQuery();
    [DllImport("__Internal")] static extern double ZbNow();
    [DllImport("__Internal")] static extern double ZbWsLastArrival();
    [DllImport("__Internal")] static extern double ZbNetsimMs();
    public static void Open(string path) { ZbWsOpen(path); }
    public static int State() { return ZbWsState(); }
    public static bool Send(byte[] d, int len) { return ZbWsSend(d, len) == 1; }
    public static int Recv(byte[] b) { return ZbWsRecv(b, b.Length); }
    public static int Buffered() { return ZbWsBuffered(); }
    public static void Close() { ZbWsClose(); }
    public static string Origin() { return Marshal.PtrToStringUTF8(ZbOrigin()); }
    public static string Query() { return Marshal.PtrToStringUTF8(ZbQuery()); }
    public static double Now() { return ZbNow(); }
    public static double LastArrival() { return ZbWsLastArrival(); }
    public static double NetsimMs() { return ZbNetsimMs(); }
#else
    public static void Open(string path) { }
    public static int State() { return 3; }
    public static bool Send(byte[] d, int len) { return false; }
    public static int Recv(byte[] b) { return 0; }
    public static int Buffered() { return 0; }
    public static void Close() { }
    public static string Origin() { return "http://localhost:8787"; }
    public static string Query() { return ""; }
    public static double Now() { return UnityEngine.Time.realtimeSinceStartupAsDouble * 1000.0; }
    public static double LastArrival() { return Now(); }
    public static double NetsimMs() { return 0; }
#endif
}

// ---------------------------------------------------------------------------------------------------------------
// Binary helpers (little endian)
// ---------------------------------------------------------------------------------------------------------------
sealed class ZbW
{
    public byte[] b = new byte[4096];
    public int n;
    void Need(int k) { if (n + k > b.Length) Array.Resize(ref b, Math.Max(b.Length * 2, n + k)); }
    public ZbW Begin(byte op) { n = 0; U8(op); return this; }
    public ZbW U8(int v) { Need(1); b[n++] = (byte)v; return this; }
    public ZbW U16(int v) { Need(2); b[n++] = (byte)v; b[n++] = (byte)(v >> 8); return this; }
    public ZbW I32(int v) { Need(4); b[n++] = (byte)v; b[n++] = (byte)(v >> 8); b[n++] = (byte)(v >> 16); b[n++] = (byte)(v >> 24); return this; }
    public ZbW F32(float v) { return I32(BitConverter.SingleToInt32Bits(v)); }
    public ZbW I64(long v) { I32((int)(v & 0xFFFFFFFF)); return I32((int)(v >> 32)); }
    public ZbW Str8(string s, int max)
    {
        var e = Encoding.UTF8.GetBytes(s ?? "");
        int len = Math.Min(e.Length, Math.Min(max, 255));
        U8(len); Need(len); Buffer.BlockCopy(e, 0, b, n, len); n += len; return this;
    }
    public ZbW Str16(string s)
    {
        var e = Encoding.UTF8.GetBytes(s ?? "");
        int len = Math.Min(e.Length, 4000);
        U16(len); Need(len); Buffer.BlockCopy(e, 0, b, n, len); n += len; return this;
    }
    public ZbW Vec3(Vector3 v) { return F32(v.x).F32(v.y).F32(v.z); }
    public ZbW Quat(Quaternion q) { return F32(q.x).F32(q.y).F32(q.z).F32(q.w); }
}

sealed class ZbR
{
    public byte[] b; public int o, end;
    public ZbR(byte[] buf, int start, int len) { b = buf; o = start; end = start + len; }
    public bool Ok { get { return o <= end; } }
    public int U8() { if (o + 1 > end) { o = end + 1; return 0; } return b[o++]; }
    public int U16() { if (o + 2 > end) { o = end + 1; return 0; } int v = b[o] | (b[o + 1] << 8); o += 2; return v; }
    public int I32() { if (o + 4 > end) { o = end + 1; return 0; } int v = b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24); o += 4; return v; }
    public float F32() { return BitConverter.Int32BitsToSingle(I32()); }
    public long I64() { long lo = (uint)I32(); long hi = I32(); return (hi << 32) | lo; }
    public string Str8() { int l = U8(); if (o + l > end) { o = end + 1; return ""; } var s = Encoding.UTF8.GetString(b, o, l); o += l; return s; }
    public string Str16() { int l = U16(); if (o + l > end) { o = end + 1; return ""; } var s = Encoding.UTF8.GetString(b, o, l); o += l; return s; }
    public Vector3 Vec3() { return new Vector3(F32(), F32(), F32()); }
    public Quaternion Quat() { return new Quaternion(F32(), F32(), F32(), F32()); }
}

// Marks the root of a Network.Instantiate'd object (its view id base and owner).
public class NetIdentity : MonoBehaviour
{
    public long baseId;
    public int owner;
    void OnDestroy() { ZbNet.UnregisterRoot(baseId, gameObject); }
}

// ---------------------------------------------------------------------------------------------------------------
public static class ZbNet
{
    // wire opcodes
    const byte C_RPC = 1, C_INST = 2, C_DESTROY = 3, C_REMOVE_RPCS = 4, C_DESTROY_PLAYER = 5, C_KICK = 6, C_PING = 7, C_LISTING = 8, C_TIME = 9, C_ECHO = 10, C_STATE = 11;
    const byte S_WELCOME = 0x81, S_RPC = 0x82, S_INST = 0x83, S_DESTROY = 0x84, S_CONNECTED = 0x85, S_DISCONNECTED = 0x86,
        S_REJECT = 0x87, S_PONG = 0x88, S_PING_INFO = 0x89, S_SYNC_DONE = 0x8a, S_TIME = 0x8b, S_ECHO = 0x8c, S_STATE = 0x8d;
    const int PROTO = 2;
    public const long SCENE_BASE = 1L << 51;
    const long OWNER_UNIT = 1L << 40;

    enum Mode { Offline, HostConnecting, Host, ClientConnecting, Client }
    static Mode mode = Mode.Offline;

    public static int localId;
    public static string roomCode = "";
    public static string roomName = "";
    public static bool Active { get { return mode != Mode.Offline; } }
    public static bool Connected { get { return mode == Mode.Host || mode == Mode.Client; } }
    public static int remoteCount;
    public static readonly Dictionary<int, string> playerIp = new Dictionary<int, string>();
    public static readonly Dictionary<int, int> playerPing = new Dictionary<int, int>();
    public static int myPing;

    static bool synced;
    static string hostName, hostPassword, hostType = "";
    static bool hostListed; static string hostComment = "";
    static long counter;
    static NetRunner runner;

    // views and roots
    static readonly Dictionary<long, NetworkView> views = new Dictionary<long, NetworkView>();
    static readonly Dictionary<long, GameObject> roots = new Dictionary<long, GameObject>();
    static bool hasPending; static long pendingView; static int pendingOwner;

    // ---- prefab table ------------------------------------------------------------------------
    static ZbNetPrefabs table;
    static Dictionary<int, int> prefabIndex;
    static void LoadTable()
    {
        if (prefabIndex != null) return;
        prefabIndex = new Dictionary<int, int>();
        table = Resources.Load<ZbNetPrefabs>("ZbNetPrefabs");
        if (table == null) { Debug.LogError("[zbnet] ZbNetPrefabs table missing"); return; }
        for (int i = 0; i < table.prefabs.Length; i++)
            if (table.prefabs[i] != null) prefabIndex[table.prefabs[i].GetInstanceID()] = i;
    }
    static GameObject ToGo(UnityEngine.Object o)
    {
        var g = o as GameObject; if (g != null) return g;
        var c = o as Component; return c != null ? c.gameObject : null;
    }

    // ---- runner ------------------------------------------------------------------------------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot() { EnsureRunner(); }
    static void EnsureRunner()
    {
        if (runner != null) return;
        var go = new GameObject("ZbNetRunner");
        go.hideFlags = HideFlags.HideInHierarchy;
        UnityEngine.Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<NetRunner>();
    }

    // ---- connection lifecycle (called by Network / MasterServer shims) -----------------------
    public static void StartHost(int maxPlayers, string password)
    {
        ZbCfg.Load(); EnsureRunner(); ResetSession();
        mode = Mode.HostConnecting; localId = 0; synced = true;
        hostPassword = password ?? "";
        playerIp[0] = "127.0.0.1";
        string path = "/ws/host?v=" + PROTO + "&max=" + maxPlayers + "&name=" + Uri.EscapeDataString(roomName.Length > 0 ? roomName : "Zumbi room")
            + "&pw=" + Uri.EscapeDataString(hostPassword) + "&type=" + Uri.EscapeDataString(hostType);
        ZbWs.Open(path);
    }

    public static void StartClient(string code, string password)
    {
        ZbCfg.Load(); EnsureRunner(); ResetSession();
        mode = Mode.ClientConnecting;
        ZbWs.Open("/ws/room/" + Uri.EscapeDataString(code) + "?v=" + PROTO + "&pw=" + Uri.EscapeDataString(password ?? ""));
    }

    public static void Register(string gameType, string name, string comment)
    {
        hostType = gameType ?? ""; roomName = name ?? ""; hostComment = comment ?? ""; hostListed = true;
        if (Active && Network.isServer) SendListing();
    }
    public static void Unregister()
    {
        hostListed = false;
        if (Active && Network.isServer) SendListing();
    }
    static void SendListing()
    {
        w.Begin(C_LISTING).U8(hostListed ? 1 : 0).Str8(roomName, 40).Str8(hostComment, 60).Str8(hostType, 40);
        Queue();
    }

    public static void Shutdown()
    {
        FlushOut();
        ZbWs.Close();
        ResetSession();
    }

    static void ResetSession()
    {
        mode = Mode.Offline; synced = false; remoteCount = 0; roomCode = ""; counter = 0; localId = 0;
        outLen = 0; playerIp.Clear(); playerPing.Clear(); pendingRpcs.Clear(); myPing = 0; ZbSync.Reset(); nextEcho = 0;
    }

    // ---- ids ---------------------------------------------------------------------------------
    public static bool IsMine(int owner) { return owner == localId; }
    public static long NewView() { counter++; return (localId + 1) * OWNER_UNIT + (counter << 12); }
    public static long RootOf(long v) { return v >= SCENE_BASE ? v : v & ~0xFFFL; }

    static uint Fnv(string s)
    {
        uint h = 2166136261;
        for (int i = 0; i < s.Length; i++) { h ^= s[i]; h *= 16777619; }
        return h;
    }
    static string PathOf(Transform t, Transform stop)
    {
        var sb = new StringBuilder();
        for (var c = t; c != null && c != stop; c = c.parent)
        {
            if (sb.Length > 0) sb.Insert(0, '/');
            sb.Insert(0, c.parent == null ? c.name : c.name + "#" + c.GetSiblingIndex());
        }
        return sb.ToString();
    }

    // Called by NetworkView.Awake: work out the view id and owner for a GameObject.
    public static void ResolveView(NetworkView v)
    {
        var ident = v.GetComponentInParent<NetIdentity>(true);
        if (ident == null && hasPending)
        {
            ident = v.transform.root.gameObject.AddComponent<NetIdentity>();
            ident.baseId = pendingView; ident.owner = pendingOwner; roots[pendingView] = ident.gameObject;
        }
        if (ident != null)
        {
            long sub = 0;
            if (ident.transform != v.transform)
                sub = 1 + (Fnv(PathOf(v.transform, ident.transform)) % 4095);
            v.id = ident.baseId + sub; v.ownerId = ident.owner;
        }
        else
        {
            v.id = SCENE_BASE + Fnv(PathOf(v.transform, null)); v.ownerId = 0;
        }
        NetworkView old;
        if (views.TryGetValue(v.id, out old) && old != null && old != v)
            Debug.LogWarning("[zbnet] view id collision: " + v.name + " vs " + old.name);
        views[v.id] = v;
    }
    public static void UnregisterView(NetworkView v)
    {
        NetworkView cur;
        if (views.TryGetValue(v.id, out cur) && cur == v) views.Remove(v.id);
    }
    public static void UnregisterRoot(long id, GameObject go)
    {
        GameObject cur;
        if (roots.TryGetValue(id, out cur) && cur == go) roots.Remove(id);
    }

    static readonly Dictionary<Type, bool> hasRpc = new Dictionary<Type, bool>();
    public static bool HasRpc(Type t)
    {
        bool r;
        if (hasRpc.TryGetValue(t, out r)) return r;
        for (var c = t; c != null && c != typeof(MonoBehaviour) && !r; c = c.BaseType)
            foreach (var m in c.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                if (m.IsDefined(typeof(RPCAttribute), false)) { r = true; break; }
        hasRpc[t] = r;
        return r;
    }
    // Make sure every GameObject that can receive an RPC has a registered NetworkView.
    public static void EnsureViews(GameObject root)
    {
        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            if (mb != null && !(mb is NetworkView) && HasRpc(mb.GetType())) mb.NetView();
    }
    static float lastScan = -10;
    public static void ScanScene()
    {
        lastScan = Time.unscaledTime;
        foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (mb != null && !(mb is NetworkView) && HasRpc(mb.GetType())) mb.NetView();
    }

    // ---- instantiate / destroy ---------------------------------------------------------------
    public static UnityEngine.Object Instantiate(UnityEngine.Object prefab, Vector3 pos, Quaternion rot, int group)
    {
        if (!Active) return UnityEngine.Object.Instantiate(prefab, pos, rot);
        LoadTable();
        var pgo = ToGo(prefab);
        int idx; if (pgo == null || !prefabIndex.TryGetValue(pgo.GetInstanceID(), out idx)) idx = -1;
        long view = NewView();
        var clone = InstantiateNet(prefab, pos, rot, view, localId);
        if (idx < 0) { Debug.LogError("[zbnet] prefab not in table, not replicated: " + (prefab != null ? prefab.name : "null")); return clone; }
        w.Begin(C_INST).U8(group).I64(view).U16(idx).Vec3(pos).Quat(rot);
        Queue();
        return clone;
    }

    static UnityEngine.Object InstantiateNet(UnityEngine.Object prefab, Vector3 pos, Quaternion rot, long view, int owner)
    {
        hasPending = true; pendingView = view; pendingOwner = owner;
        UnityEngine.Object clone;
        try { clone = UnityEngine.Object.Instantiate(prefab, pos, rot); }
        finally { hasPending = false; }
        var go = ToGo(clone);
        if (go != null)
        {
            var id = go.GetComponent<NetIdentity>();
            if (id == null) id = go.AddComponent<NetIdentity>();
            id.baseId = view; id.owner = owner; roots[view] = go;
            EnsureViews(go);
            ZbSync.Tune(go);
        }
        return clone;
    }

    public static void DestroyNetworked(GameObject go)
    {
        if (go == null) return;
        var id = go.GetComponentInParent<NetIdentity>(true);
        if (id != null && Active)
        {
            w.Begin(C_DESTROY).I64(id.baseId); Queue();
            UnityEngine.Object.Destroy(id.gameObject);
        }
        else UnityEngine.Object.Destroy(go);
    }
    public static void DestroyView(long viewId)
    {
        GameObject go;
        if (roots.TryGetValue(RootOf(viewId), out go)) DestroyNetworked(go);
        else
        {
            // Offline (single player) Network.Instantiate registers no root, but the view itself is
            // still known. Network.Destroy(viewID) must still destroy the object, otherwise scripts
            // that rely on it (sc_suplly airdrop crate) keep running and respawn every frame.
            NetworkView v;
            if (views.TryGetValue(viewId, out v) && v != null) DestroyNetworked(v.gameObject);
        }
    }

    public static void RemoveRpcs(int player) { if (Active) { w.Begin(C_REMOVE_RPCS).I32(player); Queue(); } }
    public static void DestroyPlayerObjects(int player) { if (Active) { w.Begin(C_DESTROY_PLAYER).I32(player); Queue(); } }
    public static void Kick(int player) { if (Active && Network.isServer && player != 0) { w.Begin(C_KICK).I32(player); Queue(); } }

    // ---- rpc ---------------------------------------------------------------------------------
    public static void SendRpc(NetworkView v, string name, RPCMode m, object[] args)
    {
        bool isServer = Network.isServer;
        if (m == RPCMode.All || m == RPCMode.AllBuffered || (m == RPCMode.Server && isServer)) v.Dispatch(name, args);
        if (!Active) return;
        if (m == RPCMode.Server && isServer) return;
        bool buffered = m == RPCMode.OthersBuffered || m == RPCMode.AllBuffered;
        if (!buffered && isServer && remoteCount == 0) return; // nobody to tell
        if (m == RPCMode.Others && ZbCfg.compact && TrySendState(v, name, args)) return;
        w.Begin(C_RPC).U8((int)m).I64(v.id).Str8(name, 64);
        WriteArgs(args);
        if (w.n > 8000) { Debug.LogWarning("[zbnet] rpc too large, dropped: " + name); return; }
        Queue();
    }

    // Compact binary form of the three high-rate position RPCs (see docs/MULTIPLAYER.md, "State messages").
    static bool TrySendState(NetworkView v, string name, object[] args)
    {
        if (args == null) return false;
        if (name == "setzumbistuff")
        {
            if (args.Length != 2 || !(args[0] is int) || !(args[1] is Vector3)) return false;
            int y = (int)args[0]; if (y < short.MinValue || y > short.MaxValue) return false;
            w.Begin(C_STATE).U8(1).I64(v.id).U16(y & 0xFFFF).Vec3((Vector3)args[1]);
        }
        else if (name == "setposstuff")
        {
            if (args.Length != 6 || !(args[0] is int) || !(args[1] is int) || !(args[2] is Vector3) || !(args[3] is float) || !(args[4] is float) || !(args[5] is float)) return false;
            int y = (int)args[0], h = (int)args[1];
            if (y < short.MinValue || y > short.MaxValue || h < short.MinValue || h > short.MaxValue) return false;
            w.Begin(C_STATE).U8(2).I64(v.id).U16(y & 0xFFFF).U16(h & 0xFFFF).Vec3((Vector3)args[2]).F32((float)args[3]).F32((float)args[4]).F32((float)args[5]);
        }
        else if (name == "updateposition")
        {
            if (args.Length != 1 || !(args[0] is Vector3)) return false;
            w.Begin(C_STATE).U8(3).I64(v.id).Vec3((Vector3)args[0]);
        }
        else return false;
        Queue();
        return true;
    }

    static bool ReadState(ZbR r, out long view, out string name, out object[] args)
    {
        int kind = r.U8(); view = r.I64(); name = null; args = null;
        if (kind == 1) { int y = (short)r.U16(); var p = r.Vec3(); name = "setzumbistuff"; args = new object[] { y, p }; }
        else if (kind == 2) { int y = (short)r.U16(); int h = (short)r.U16(); var p = r.Vec3(); float a = r.F32(), b = r.F32(), c = r.F32(); name = "setposstuff"; args = new object[] { y, h, p, a, b, c }; }
        else if (kind == 3) { var p = r.Vec3(); name = "updateposition"; args = new object[] { p }; }
        return name != null && r.Ok;
    }

    static void WriteArgs(object[] args)
    {
        if (args == null) { w.U8(0); return; }
        w.U8(args.Length);
        foreach (var a in args)
        {
            if (a == null) w.U8(0);
            else if (a is int) w.U8(1).I32((int)a);
            else if (a is float) w.U8(2).F32((float)a);
            else if (a is bool) w.U8(3).U8((bool)a ? 1 : 0);
            else if (a is string) w.U8(4).Str16((string)a);
            else if (a is Vector3) w.U8(5).Vec3((Vector3)a);
            else if (a is Quaternion) w.U8(6).Quat((Quaternion)a);
            else if (a is int[]) { var arr = (int[])a; w.U8(7).U16(arr.Length); foreach (var x in arr) w.I32(x); }
            else if (a is double) w.U8(2).F32((float)(double)a);
            else if (a is long) w.U8(1).I32((int)(long)a);
            else if (a is short || a is byte || a is sbyte || a is ushort || a is uint) w.U8(1).I32(Convert.ToInt32(a));
            else if (a is Enum) w.U8(1).I32(Convert.ToInt32(a));
            else { Debug.LogWarning("[zbnet] unsupported rpc arg type " + a.GetType()); w.U8(0); }
        }
    }
    static object[] ReadArgs(ZbR r)
    {
        int n = r.U8();
        var args = new object[n];
        for (int i = 0; i < n && r.Ok; i++)
        {
            switch (r.U8())
            {
                case 1: args[i] = r.I32(); break;
                case 2: args[i] = r.F32(); break;
                case 3: args[i] = r.U8() != 0; break;
                case 4: args[i] = r.Str16(); break;
                case 5: args[i] = r.Vec3(); break;
                case 6: args[i] = r.Quat(); break;
                case 7: { int c = r.U16(); var arr = new int[c]; for (int k = 0; k < c && r.Ok; k++) arr[k] = r.I32(); args[i] = arr; break; }
                default: args[i] = null; break;
            }
        }
        return args;
    }

    // ---- outgoing queue ----------------------------------------------------------------------
    static readonly ZbW w = new ZbW();
    static byte[] outBuf = new byte[65536];
    static int outLen;
    const int OUT_SOFT = 48000, OUT_CAP = 2 * 1024 * 1024;

    static void Queue()
    {
        int need = 2 + w.n;
        if (outLen + need > OUT_SOFT && ZbWs.State() == 2) FlushOut();
        if (outLen == 0) StampFrame(); // every frame starts with this peer's clock reading
        if (outLen + need > OUT_CAP) { Debug.LogWarning("[zbnet] out queue full, dropping"); return; }
        if (outLen + need > outBuf.Length) Array.Resize(ref outBuf, Math.Max(outBuf.Length * 2, outLen + need));
        outBuf[outLen++] = (byte)w.n; outBuf[outLen++] = (byte)(w.n >> 8);
        Buffer.BlockCopy(w.b, 0, outBuf, outLen, w.n); outLen += w.n;
        ZbSync.txMsgs++;
        byte op = outBuf[outLen - w.n]; long c; ZbSync.txByOp.TryGetValue(op, out c); ZbSync.txByOp[op] = c + 1;
    }

    // C_TIME: u32 sender clock (ms, performance.now) of the moment this frame was started. The relay passes it on to the
    // recipients (S_TIME) so they can interpolate on the sender's timeline instead of on jittery arrival times.
    static void StampFrame()
    {
        outBuf[outLen++] = 5; outBuf[outLen++] = 0; outBuf[outLen++] = C_TIME;
        uint ms = (uint)(long)ZbWs.Now();
        outBuf[outLen++] = (byte)ms; outBuf[outLen++] = (byte)(ms >> 8); outBuf[outLen++] = (byte)(ms >> 16); outBuf[outLen++] = (byte)(ms >> 24);
    }

    static void FlushOut()
    {
        if (outLen == 0 || ZbWs.State() != 2) return;
        // chunk on message boundaries so one frame stays well under the relay's 64 KB limit
        int o = 0;
        while (o < outLen)
        {
            int start = o;
            while (o + 2 <= outLen)
            {
                int len = outBuf[o] | (outBuf[o + 1] << 8);
                if (o - start + 2 + len > 60000 && o > start) break;
                o += 2 + len;
            }
            var chunk = new byte[o - start];
            Buffer.BlockCopy(outBuf, start, chunk, 0, chunk.Length);
            if (ZbWs.Send(chunk, chunk.Length)) { ZbSync.txFrames++; ZbSync.txBytes += chunk.Length + 2; }
        }
        outLen = 0;
    }

    // ---- per frame ---------------------------------------------------------------------------
    static readonly byte[] rx = new byte[1 << 19];
    static float nextPing, nextEcho;
    struct PendingRpc { public float t; public int sender; public long view; public string name; public object[] args; }
    static readonly List<PendingRpc> pendingRpcs = new List<PendingRpc>();
    static readonly Queue<Action> deferred = new Queue<Action>();

    public static void Defer(Action a) { deferred.Enqueue(a); }

    public static void Tick()
    {
        while (deferred.Count > 0) deferred.Dequeue()();
        if (mode == Mode.Offline) return;

        int st = ZbWs.State();
        if (st == 3) { OnSocketClosed(); return; }
        if (st == 2)
        {
            if (mode == Mode.HostConnecting) { /* wait for WELCOME */ }
            for (int guard = 0; guard < 256; guard++)
            {
                int n = ZbWs.Recv(rx);
                if (n == 0) break;
                if (n < 0) { Debug.LogWarning("[zbnet] oversized frame dropped"); continue; }
                ZbSync.rxFrames++; ZbSync.rxBytes += n + 2;
                ZbSync.ctxArrival = ZbWs.LastArrival();
                if (!ProcessFrame(n)) return; // yield to the game for a frame (e.g. after WELCOME) or session ended
            }
            if (Connected && Time.unscaledTime >= nextPing)
            {
                nextPing = Time.unscaledTime + 2f;
                w.Begin(C_PING).I32((int)(Time.realtimeSinceStartup * 1000f)).U16(Mathf.Clamp(myPing, 0, 65535));
                Queue();
            }
            if (Connected && Time.unscaledTime >= nextEcho)
            {
                // round trip to the host through the relay (host side: to every client), measured by the game processes themselves
                nextEcho = Time.unscaledTime + 1f;
                if (Network.isServer) { var ids = new List<int>(playerIp.Keys); foreach (var id in ids) if (id != 0) SendEcho(0, id, (long)ZbWs.Now()); }
                else SendEcho(0, 0, (long)ZbWs.Now());
            }
        }
        RetryPending();
    }

    public static void LateTick()
    {
        if (mode != Mode.Offline) FlushOut();
    }

    static void SendEcho(int type, int target, long t)
    {
        w.Begin(C_ECHO).U8(type).I32(target).I32((int)(uint)t);
        Queue();
    }

    // Returns false when the caller should stop reading for this frame (after WELCOME / session end).
    static bool ProcessFrame(int n)
    {
        int o = 0; bool yield = false;
        ZbSync.ctxSender = -1; ZbSync.ctxHasStamp = false;
        while (o + 2 <= n)
        {
            int len = rx[o] | (rx[o + 1] << 8); o += 2;
            if (len < 1 || o + len > n) break;
            ZbSync.rxMsgs++;
            { byte op = rx[o]; long c; ZbSync.rxByOp.TryGetValue(op, out c); ZbSync.rxByOp[op] = c + 1; }
            if (!ProcessMessage(o, len)) yield = true;
            o += len;
        }
        ZbSync.ctxSender = -1; ZbSync.ctxHasStamp = false;
        return !yield && mode != Mode.Offline;
    }

    static bool ProcessMessage(int o, int len)
    {
        var r = new ZbR(rx, o, len);
        int op = r.U8();
        switch (op)
        {
            case S_WELCOME:
            {
                localId = r.I32(); bool isHost = r.U8() == 1; int max = r.U8(); roomCode = r.Str8();
                if (isHost) { mode = Mode.Host; if (hostListed) SendListing(); Notify("OnServerInitialized"); return true; }
                mode = Mode.Client; Network.peerType = NetworkPeerType.Client;
                Notify("OnConnectedToServer");
                return false; // buffered state follows on the next frame, like RakNet
            }
            case S_RPC:
            {
                int sender = r.I32(); r.U8(); long view = r.I64(); string name = r.Str8(); var args = ReadArgs(r);
                if (!DeliverRpc(view, name, args)) pendingRpcs.Add(new PendingRpc { t = Time.unscaledTime, sender = sender, view = view, name = name, args = args });
                break;
            }
            case S_INST:
            {
                int sender = r.I32(); r.U8(); long view = r.I64(); int idx = r.U16(); var pos = r.Vec3(); var rot = r.Quat();
                LoadTable();
                if (table == null || idx >= table.prefabs.Length || table.prefabs[idx] == null) { Debug.LogError("[zbnet] unknown prefab index " + idx); break; }
                InstantiateNet(table.prefabs[idx], pos, rot, view, sender);
                break;
            }
            case S_DESTROY:
            {
                long view = r.I64(); GameObject go;
                if (roots.TryGetValue(RootOf(view), out go) && go != null) UnityEngine.Object.Destroy(go);
                break;
            }
            case S_CONNECTED:
            {
                int id = r.I32(); string ip = r.Str8();
                playerIp[id] = ip; remoteCount++;
                Notify("OnPlayerConnected", new NetworkPlayer { id = id });
                break;
            }
            case S_DISCONNECTED:
            {
                int id = r.I32();
                remoteCount = Math.Max(0, remoteCount - 1); playerPing.Remove(id);
                Notify("OnPlayerDisconnected", new NetworkPlayer { id = id });
                playerIp.Remove(id);
                break;
            }
            case S_REJECT:
                OnReject(r.U8());
                return false;
            case S_PONG:
            {
                int t = r.I32();
                myPing = Mathf.Max(0, (int)(Time.realtimeSinceStartup * 1000f) - t);
                playerPing[localId] = myPing;
                break;
            }
            case S_PING_INFO:
            {
                int id = r.I32(); playerPing[id] = r.U16();
                break;
            }
            case S_SYNC_DONE:
                synced = true;
                break;
            case S_TIME:
            {
                int sender = r.I32(); double stamp = (uint)r.I32();
                ZbSync.ctxSender = sender; ZbSync.ctxStamp = stamp; ZbSync.ctxHasStamp = true;
                if (ZbSync.ctxArrival > 0) ZbSync.Clock(sender).Sample(ZbSync.ctxArrival, stamp);
                break;
            }
            case S_ECHO:
            {
                int sender = r.I32(); int type = r.U8(); long t = (uint)r.I32();
                if (type == 0) SendEcho(1, sender, t); // answer at once; the sender measures the round trip
                else
                {
                    double rt = (double)(uint)(long)(ZbSync.ctxArrival > 0 ? ZbSync.ctxArrival : ZbWs.Now()) - t;
                    if (rt < 0) rt += 4294967296.0;
                    if (rt < 60000) ZbSync.Rtt(sender).Add(rt);
                }
                break;
            }
            case S_STATE:
            {
                long view; string name; object[] args;
                if (ReadState(r, out view, out name, out args) && !DeliverRpc(view, name, args))
                    pendingRpcs.Add(new PendingRpc { t = Time.unscaledTime, sender = ZbSync.ctxSender, view = view, name = name, args = args });
                break;
            }
        }
        return true;
    }

    static bool DeliverRpc(long view, string name, object[] args)
    {
        NetworkView nv;
        if (views.TryGetValue(view, out nv) && nv != null) { ZbSync.TryFeed(nv, name, args); nv.Dispatch(name, args); return true; }
        // Instantiated objects that are gone are expected (late RPCs); scene objects may not have a view yet.
        if (view < SCENE_BASE && !roots.ContainsKey(RootOf(view))) return true;
        return false;
    }

    static void RetryPending()
    {
        if (pendingRpcs.Count == 0) return;
        if (Time.unscaledTime - lastScan > 0.25f) ScanScene();
        var list = new List<PendingRpc>(pendingRpcs);
        pendingRpcs.Clear();
        foreach (var p in list)
        {
            if (DeliverRpc(p.view, p.name, p.args)) continue;
            if (Time.unscaledTime - p.t < 10f) pendingRpcs.Add(p);
            else Debug.LogWarning("[zbnet] dropped rpc " + p.name + " for unknown view " + p.view);
        }
    }

    // ---- failures ----------------------------------------------------------------------------
    static void OnReject(int reason)
    {
        var m = mode;
        ZbWs.Close(); // we got the reason; the socket close that follows is expected
        bool wasHandshake = m == Mode.ClientConnecting || m == Mode.HostConnecting;
        Network.peerType = NetworkPeerType.Disconnected;
        mode = Mode.Offline; ResetSession();
        if (wasHandshake)
        {
            var err = NetworkConnectionError.ConnectionFailed;
            if (reason == 2) err = NetworkConnectionError.InvalidPassword;
            else if (reason == 3) err = NetworkConnectionError.TooManyConnectedPlayers;
            Notify("OnFailedToConnect", err);
        }
        else Notify("OnDisconnectedFromServer", reason == 5 ? NetworkDisconnection.Disconnected : NetworkDisconnection.LostConnection);
    }

    static void OnSocketClosed()
    {
        var m = mode;
        ZbWs.Close();
        Network.peerType = NetworkPeerType.Disconnected;
        ResetSession();
        if (m == Mode.ClientConnecting) Notify("OnFailedToConnect", NetworkConnectionError.ConnectionFailed);
        else Notify("OnDisconnectedFromServer", NetworkDisconnection.LostConnection);
    }

    // ---- Unity-style network messages (SendMessage to every script) --------------------------
    static readonly Dictionary<(Type, string, int), MethodInfo> evCache = new Dictionary<(Type, string, int), MethodInfo>();
    public static void Notify(string name, params object[] args)
    {
        foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            var t = mb.GetType();
            MethodInfo m;
            var key = (t, name, args.Length);
            if (!evCache.TryGetValue(key, out m))
            {
                for (var c = t; c != null && c != typeof(MonoBehaviour) && m == null; c = c.BaseType)
                    foreach (var mi in c.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                        if (mi.Name == name && mi.GetParameters().Length == args.Length) { m = mi; break; }
                evCache[key] = m;
            }
            if (m == null) continue;
            try { m.Invoke(mb, args); }
            catch (Exception e) { Debug.LogException(e is TargetInvocationException && e.InnerException != null ? e.InnerException : e); }
        }
    }

    public static int Backlog { get { return outLen + ZbWs.Buffered(); } }

    public static int GetPing(int player)
    {
        int p; return playerPing.TryGetValue(player, out p) ? p : 0;
    }

    // ---- room list ---------------------------------------------------------------------------
    [Serializable] class RoomRec { public string code, name, type, comment; public int players, max; public bool pw; }
    [Serializable] class RoomList { public RoomRec[] rooms; }
    public static HostData[] hostList = new HostData[0];
    static int listRequest;

    public static void RequestHostList(string gameType)
    {
        EnsureRunner();
        runner.StartCoroutine(FetchRooms(gameType, ++listRequest));
    }
    static IEnumerator FetchRooms(string gameType, int req)
    {
        using (var wr = UnityWebRequest.Get(ZbWs.Origin() + "/api/rooms"))
        {
            yield return wr.SendWebRequest();
            if (req != listRequest) yield break; // a newer request (or ClearHostList) superseded this one
            if (wr.result != UnityWebRequest.Result.Success) { Debug.LogWarning("[zbnet] room list failed: " + wr.error); yield break; }
            var list = JsonUtility.FromJson<RoomList>("{\"rooms\":" + wr.downloadHandler.text + "}");
            var res = new List<HostData>();
            if (list != null && list.rooms != null)
                foreach (var r in list.rooms)
                {
                    if (!string.IsNullOrEmpty(gameType) && r.type != gameType) continue;
                    res.Add(new HostData
                    {
                        gameType = r.type, gameName = r.name, comment = r.comment ?? "", connectedPlayers = r.players, playerLimit = r.max,
                        passwordProtected = r.pw, ip = new[] { r.code }, port = 0, guid = r.code
                    });
                }
            hostList = res.ToArray();
        }
    }
    public static void ClearHostList() { listRequest++; hostList = new HostData[0]; }
}

// ---------------------------------------------------------------------------------------------------------------
[DefaultExecutionOrder(-10000)]
public class NetRunner : MonoBehaviour
{
    void OnEnable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnLoaded; ZbCfg.Load(); showStats = ZbCfg.statsOn; }
    void OnDisable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnLoaded; }
    void OnLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m)
    {
        if (ZbNet.Active) ZbNet.ScanScene();
    }
    bool showStats;
    float fpsDt = 0.016f;
    void Update()
    {
        ZbNet.Tick();
        ZbSync.TickRates();
        fpsDt += (Time.unscaledDeltaTime - fpsDt) * 0.05f;
        if (Input.GetKeyDown(KeyCode.F3)) showStats = !showStats;
    }
    void LateUpdate() { ZbNet.LateTick(); }
    void OnApplicationQuit() { ZbNet.Shutdown(); }

    // Test/debug hook: unityInstance.SendMessage("ZbNetRunner", "DebugDump", "") prints one "ZBDUMP {json}" line to the console.
    public void DebugDump(string unused)
    {
        var sb = new StringBuilder();
        sb.Append("{\"mode\":\"").Append(ZbNet.Connected ? (Network.isServer ? "host" : "client") : "offline").Append("\",\"id\":").Append(ZbNet.localId)
          .Append(",\"remote\":").Append(ZbNet.remoteCount).Append(",\"code\":\"").Append(ZbNet.roomCode).Append("\"");
        var players = FindObjectsByType<sc_player>(FindObjectsSortMode.None);
        sb.Append(",\"players\":[");
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i]; var nv = p.GetComponent<NetworkView>();
            var pos = p.transform.position;
            sb.Append(i > 0 ? "," : "").Append("{\"name\":\"").Append(p.playername).Append("\",\"owner\":").Append(nv != null ? nv.ownerId : -1)
              .Append(",\"x\":").Append(Mathf.Round(pos.x * 10) / 10f).Append(",\"z\":").Append(Mathf.Round(pos.z * 10) / 10f).Append("}");
        }
        sb.Append("]");
        var zs = FindObjectsByType<sc_zumbi>(FindObjectsSortMode.None);
        sb.Append(",\"zombies\":").Append(zs.Length).Append(",\"zpos\":[");
        var zl = new System.Collections.Generic.List<string>();
        foreach (var z in zs) zl.Add(Mathf.Round(z.transform.position.x) + "," + Mathf.Round(z.transform.position.z));
        zl.Sort(); for (int i = 0; i < zl.Count && i < 12; i++) sb.Append(i > 0 ? "," : "").Append("\"").Append(zl[i]).Append("\"");
        sb.Append("]");
        var mc = FindFirstObjectByType<sc_matchcontroller>();
        sb.Append(",\"wave\":").Append(mc != null ? mc.wave : -1).Append(",\"spawners\":").Append(mc != null ? mc.spawners : -1);
        sb.Append(",\"supply\":").Append(FindObjectsByType<sc_suplly>(FindObjectsSortMode.None).Length)
          .Append(",\"crates\":").Append(FindObjectsByType<sc_ammocrate>(FindObjectsSortMode.None).Length);
        { // supply bags: clones of the bag prefab the airdrop crate lands as
            int bags = 0; var mcp = FindFirstObjectByType<sc_matchcontroller>();
            var sp = mcp != null && mcp.supllyprefab != null ? mcp.supllyprefab.GetComponent<sc_suplly>() : null;
            if (sp != null && sp.bagprefab != null)
                foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (t.parent == null && t.name == sp.bagprefab.name + "(Clone)") bags++;
            sb.Append(",\"bags\":").Append(bags);
        }
        sb.Append(",\"shots\":").Append(FindObjectsByType<sc_shot>(FindObjectsSortMode.None).Length);
        float minLife = float.MaxValue, maxLife = 0f;
        foreach (var z in zs) { var lf = z.GetComponent<sc_life>(); if (lf != null && z.state != -1) { minLife = Mathf.Min(minLife, lf.quantia); maxLife = Mathf.Max(maxLife, lf.quantia); } }
        foreach (var p in players)
        {
            var pnv = p.GetComponent<NetworkView>();
            if (pnv == null || !pnv.isMine) continue;
            float nd = float.MaxValue;
            foreach (var z in zs) if (z.state != -1) nd = Mathf.Min(nd, Vector3.Distance(z.transform.position, p.transform.position));
            if (nd < float.MaxValue) sb.Append(",\"zdist\":").Append(Mathf.Round(nd * 10f) / 10f);
        }
        if (minLife < float.MaxValue) sb.Append(",\"zlifeMin\":").Append(Mathf.Round(minLife)).Append(",\"zlifeMax\":").Append(Mathf.Round(maxLife));
        var md = FindFirstObjectByType<sc_matchdata>();
        sb.Append(",\"killed\":").Append(md != null ? md.zombieskilled : -1);
        foreach (var p in players) { var nv = p.GetComponent<NetworkView>(); var lf = p.GetComponent<sc_life>(); if (nv != null && nv.isMine && lf != null) sb.Append(",\"hp\":").Append(Mathf.Round(lf.quantia)); }
        sb.Append("}");
        Debug.Log("ZBDUMP " + sb);
    }

    // Test hooks for headless rendering checks: DebugPose("x,y,z,yaw") teleports the local player; DebugFreeze("0"|"1") sets Time.timeScale.
    public void DebugPose(string a)
    {
        var f = a.Split(',');
        foreach (var p in FindObjectsByType<sc_player>(FindObjectsSortMode.None)) { var nv = p.GetComponent<NetworkView>(); if (nv == null || !nv.isMine) continue;
            var cc = p.GetComponent<CharacterController>(); if (cc != null) cc.enabled = false;
            p.transform.position = new Vector3(float.Parse(f[0]), float.Parse(f[1]), float.Parse(f[2]));
            p.transform.rotation = Quaternion.Euler(0f, float.Parse(f[3]), 0f); p.headang = 0f;
            if (cc != null) cc.enabled = true; }
    }
    public void DebugFreeze(string a) { Time.timeScale = float.Parse(a); }
    // Test hook: the radio's "call supply" path (sc_player armstate 13 -> sc_spawner.asksuplly), without needing the item in hand.
    public void DebugSupply(string unused)
    {
        var sp = FindFirstObjectByType<sc_spawner>();
        if (sp == null) { Debug.Log("ZBSUPPLY nospawner"); return; }
        if (Network.isClient) sp.NetView().RPC("asksuplly", RPCMode.Server);
        else sp.asksuplly();
        Debug.Log("ZBSUPPLY called");
    }

    // Test hook: top up the local player's life (idle test players get eaten by zombies).
    public void DebugHeal(string unused)
    {
        foreach (var p in FindObjectsByType<sc_player>(FindObjectsSortMode.None)) { var nv = p.GetComponent<NetworkView>(); var lf = p.GetComponent<sc_life>(); if (nv != null && nv.isMine && lf != null) lf.quantia = 100f; }
    }

    // Test hook: turn the local player towards the nearest living zombie and level the view (lets a headless test aim).
    public void DebugAim(string unused)
    {
        sc_player me = null;
        foreach (var p in FindObjectsByType<sc_player>(FindObjectsSortMode.None)) { var nv = p.GetComponent<NetworkView>(); if (nv != null && nv.isMine) me = p; }
        if (me == null) return;
        sc_zumbi best = null; float bd = float.MaxValue;
        foreach (var z in FindObjectsByType<sc_zumbi>(FindObjectsSortMode.None))
        {
            if (z.state == -1) continue;
            float d = (z.transform.position - me.transform.position).sqrMagnitude;
            if (d < bd) { bd = d; best = z; }
        }
        if (best == null) return;
        var dir = best.transform.position - me.transform.position;
        me.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 0f);
        me.headang = 0f;
    }

    // unityInstance.SendMessage("ZbNetRunner", "StatsDump", "") prints one "ZBSTATS {json}" line: cumulative traffic counters
    // (tests subtract two dumps to get rates), round trips, jitter, interpolation counters.
    public void StatsDump(string unused)
    {
        var sb = new StringBuilder("{");
        sb.Append("\"t\":").Append((long)ZbWs.Now()).Append(",\"rx\":{\"msgs\":").Append(ZbSync.rxMsgs).Append(",\"bytes\":").Append(ZbSync.rxBytes).Append(",\"frames\":").Append(ZbSync.rxFrames)
          .Append("},\"tx\":{\"msgs\":").Append(ZbSync.txMsgs).Append(",\"bytes\":").Append(ZbSync.txBytes).Append(",\"frames\":").Append(ZbSync.txFrames).Append("}");
        sb.Append(",\"rtt\":{");
        bool first = true;
        foreach (var kv in ZbSync.rtt)
        {
            sb.Append(first ? "" : ",").Append("\"").Append(kv.Key).Append("\":{\"last\":").Append(Math.Round(kv.Value.last)).Append(",\"avg\":").Append(Math.Round(kv.Value.avg, 1))
              .Append(",\"min\":").Append(Math.Round(kv.Value.min)).Append(",\"jitter\":").Append(Math.Round(kv.Value.jitter, 1)).Append(",\"n\":").Append(kv.Value.n).Append("}");
            first = false;
        }
        sb.Append("},\"clockJitter\":{");
        first = true;
        foreach (var kv in ZbSync.clocks)
        {
            sb.Append(first ? "" : ",").Append("\"").Append(kv.Key).Append("\":").Append(Math.Round(kv.Value.jitter, 1));
            first = false;
        }
        sb.Append("},\"relayPing\":").Append(ZbNet.myPing);
        sb.Append(",\"interp\":{\"on\":").Append(ZbCfg.interp ? 1 : 0).Append(",\"entities\":").Append(ZbSync.interps.Count).Append(",\"snaps\":").Append(ZbSync.snaps)
          .Append(",\"frames\":").Append(ZbSync.framesSampled).Append(",\"extrap\":").Append(ZbSync.framesExtrap).Append(",\"starved\":").Append(ZbSync.framesStarved)
          .Append(",\"teleports\":").Append(ZbSync.teleports)
          .Append(",\"avgDelay\":").Append(ZbSync.framesSampled > 0 ? Math.Round(ZbSync.sumDelay / ZbSync.framesSampled, 1) : 0)
          .Append(",\"avgBuf\":").Append(ZbSync.framesSampled > 0 ? Math.Round(ZbSync.sumBuf / ZbSync.framesSampled, 2) : 0).Append("}");
        sb.Append(",\"backlog\":").Append(ZbNet.Backlog).Append(",\"fps\":").Append(Math.Round(1f / Mathf.Max(0.0001f, fpsDt), 1));
        sb.Append(",\"overlay\":").Append(showStats ? 1 : 0).Append(",\"netsimMs\":").Append(ZbWs.NetsimMs());
        sb.Append(",\"cfg\":\"").Append(ZbCfg.interp ? "interp " : "nointerp ").Append(ZbCfg.compact ? "compact " : "rpc ").Append("rate=").Append(ZbCfg.rate).Append("\"");
        sb.Append("}");
        Debug.Log("ZBSTATS " + sb);
    }

    // Visual smoothness probe: SampleStart records every frame, SampleStop prints "ZBSAMPLE {json}".
    ZbSampler sampler;
    public void SampleStart(string unused)
    {
        if (sampler != null) Destroy(sampler);
        sampler = gameObject.AddComponent<ZbSampler>();
    }
    public void SampleStop(string unused)
    {
        if (sampler == null) { Debug.Log("ZBSAMPLE {}"); return; }
        sampler.Dump(); // prints one ZBTRACK line per entity, then "ZBSAMPLE {}"
        Destroy(sampler); sampler = null;
    }

    GUIStyle style, boxStyle;
    void OnGUI()
    {
        if (!ZbNet.Connected || ZbNet.roomCode.Length == 0) return;
        if (style == null) { style = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.UpperRight }; style.normal.textColor = new Color(1f, 1f, 1f, 0.75f); }
        GUI.depth = -1000;
        string txt = "Room code: " + ZbNet.roomCode + (Network.isServer ? "  (you host, " + (ZbNet.remoteCount + 1) + " players)" : "  (" + ZbNet.myPing + " ms)");
        GUI.Label(new Rect(Screen.width - 420, 4, 410, 22), txt, style);
        if (showStats) DrawStats();
    }

    void DrawStats()
    {
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.UpperLeft, richText = false };
            boxStyle.normal.textColor = new Color(0.75f, 1f, 0.75f, 1f);
        }
        var sb = new StringBuilder();
        sb.Append("NET STATS  (F3 hides)\n");
        if (Network.isServer)
        {
            foreach (var kv in ZbSync.rtt) sb.Append("RTT player ").Append(kv.Key).Append(": ").Append(Math.Round(kv.Value.last)).Append(" ms  (avg ").Append(Math.Round(kv.Value.avg)).Append(", min ").Append(Math.Round(kv.Value.min)).Append(", jitter ").Append(Math.Round(kv.Value.jitter, 1)).Append(")\n");
            if (ZbSync.rtt.Count == 0) sb.Append("RTT: no clients\n");
        }
        else
        {
            ZbRtt h; ZbClock c;
            if (ZbSync.rtt.TryGetValue(0, out h)) sb.Append("RTT host: ").Append(Math.Round(h.last)).Append(" ms  (avg ").Append(Math.Round(h.avg)).Append(", min ").Append(Math.Round(h.min)).Append(", jitter ").Append(Math.Round(h.jitter, 1)).Append(")\n");
            else sb.Append("RTT host: ...\n");
            sb.Append("Relay RTT: ").Append(ZbNet.myPing).Append(" ms");
            if (ZbSync.clocks.TryGetValue(0, out c)) sb.Append("   host->me jitter ").Append(Math.Round(c.jitter, 1)).Append(" ms, queue ").Append(Math.Round(c.lastQueue)).Append(" ms");
            sb.Append("\n");
        }
        sb.Append("In : ").Append((ZbSync.rateRxBytes / 1024.0).ToString("0.0")).Append(" KB/s  ").Append(Math.Round(ZbSync.rateRxMsgs)).Append(" msg/s  ").Append(Math.Round(ZbSync.rateRxFrames)).Append(" frames/s\n");
        sb.Append("Out: ").Append((ZbSync.rateTxBytes / 1024.0).ToString("0.0")).Append(" KB/s  ").Append(Math.Round(ZbSync.rateTxMsgs)).Append(" msg/s  ").Append(Math.Round(ZbSync.rateTxFrames)).Append(" frames/s\n");
        sb.Append("Send backlog: ").Append(ZbNet.Backlog).Append(" B");
        double sim = ZbWs.NetsimMs(); if (sim > 0) sb.Append("    NETSIM +").Append(Math.Round(sim)).Append(" ms");
        sb.Append("\n");
        if (ZbCfg.interp)
        {
            double d = 0, b = 0; foreach (var it in ZbSync.interps) { d += it.delay; b += it.Buffered; }
            int n = ZbSync.interps.Count;
            sb.Append("Interp: ").Append(n).Append(" entities, ").Append(Math.Round(ZbSync.rateSnaps)).Append(" snapshots/s, delay ").Append(n > 0 ? Math.Round(d / n) : 0).Append(" ms, buffer ").Append(n > 0 ? (b / n).ToString("0.0") : "0");
            if (ZbSync.framesSampled > 0) sb.Append(", extrap ").Append((100.0 * ZbSync.framesExtrap / ZbSync.framesSampled).ToString("0.0")).Append("%, starved ").Append((100.0 * ZbSync.framesStarved / ZbSync.framesSampled).ToString("0.0")).Append("%");
            sb.Append("\n");
        }
        else sb.Append("Interp: off (original chase)\n");
        sb.Append("State: ").Append(ZbCfg.compact ? "compact" : "rpc").Append(", rate ").Append(ZbCfg.rate > 0 ? ZbCfg.rate.ToString("0") : "orig").Append("   ").Append(Math.Round(1f / Mathf.Max(0.0001f, fpsDt))).Append(" fps");
        var r = new Rect(8, 8, 520, 130);
        var old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f); GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
        GUI.Label(new Rect(r.x + 6, r.y + 4, r.width - 12, r.height - 8), sb.ToString(), boxStyle);
    }
}
