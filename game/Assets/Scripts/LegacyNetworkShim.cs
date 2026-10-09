// Re-creation of Unity's legacy networking API (UnityEngine.Network / NetworkView / RPC / MasterServer, RakNet based),
// which was removed from Unity long before Unity 6. The original game was written against it everywhere (319
// networkView.RPC calls, 36 Network.Instantiate calls), so this file provides the same surface and routes it to:
//   - offline loopback when playing alone (Network.InitializeServer(1, ...), exactly like the original single player), or
//   - the Cloudflare relay (ZbNet.cs + server/) for online rooms: InitializeServer(n > 1) hosts, Network.Connect joins.
// Differences from RakNet are listed in docs/MULTIPLAYER.md.
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RPCAttribute : Attribute { }

public enum RPCMode { Server = 0, Others = 1, All = 2, OthersBuffered = 5, AllBuffered = 6 }

public enum NetworkPeerType { Disconnected = 0, Server = 1, Client = 2, Connecting = 3 }

public enum NetworkConnectionError
{
    NoError = 0, RSAPublicKeyMismatch = 21, InvalidPassword = 23, ConnectionFailed = 15, TooManyConnectedPlayers = 18,
    ConnectionBanned = 22, AlreadyConnectedToServer = 16, AlreadyConnectedToAnotherServer = -1, CreateSocketOrThreadFailure = -2,
    IncorrectParameters = -3, EmptyConnectTarget = -4, InternalDirectConnectFailed = -5, NATTargetNotConnected = 69,
    NATTargetConnectionLost = 71, NATPunchthroughFailed = 73
}

public enum NetworkDisconnection { LostConnection = 20, Disconnected = 19 }

public enum ConnectionTesterStatus
{
    Error = -2, Undetermined = -1, PrivateIPNoNATPunchthrough = 0, PrivateIPHasNATPunchThrough = 1,
    PublicIPIsConnectable = 2, PublicIPPortBlocked = 3, PublicIPNoServerStarted = 4,
    LimitedNATPunchthroughPortRestricted = 5, LimitedNATPunchthroughSymmetric = 6, NATpunchthroughFullCone = 7, NATpunchthroughAddressRestrictedCone = 8
}

/// <summary>A player in the room. id 0 is the host. ipAddress is an anonymous per-room id of the connection (not a real IP).</summary>
public struct NetworkPlayer
{
    public int id;
    public string ipAddress { get { string s; return ZbNet.playerIp.TryGetValue(id, out s) ? s : (id == 0 ? "127.0.0.1" : "relay-" + id); } }
    public int port { get { return 0; } }
    public string guid { get { return "zb" + id; } }
    public string externalIP { get { return ipAddress; } }
    public int externalPort { get { return 0; } }
    public static bool operator ==(NetworkPlayer a, NetworkPlayer b) { return a.id == b.id; }
    public static bool operator !=(NetworkPlayer a, NetworkPlayer b) { return a.id != b.id; }
    public override bool Equals(object o) { return o is NetworkPlayer && ((NetworkPlayer)o).id == id; }
    public override int GetHashCode() { return id; }
    public override string ToString() { return id.ToString(); }
}

public struct NetworkViewID
{
    public long id;
    public static NetworkViewID unassigned { get { return default(NetworkViewID); } }
    public bool isMine { get { return ZbNet.IsMine(owner.id); } }
    public NetworkPlayer owner { get { return new NetworkPlayer { id = id >= ZbNet.SCENE_BASE ? 0 : (int)(id >> 40) - 1 }; } }
}

public class HostData
{
    public bool useNat;
    public string gameType = "";
    public string gameName = "";
    public int connectedPlayers;
    public int playerLimit;
    public string[] ip = new string[0];   // [0] = room code
    public int port;                      // unused (0)
    public bool passwordProtected;
    public string comment = "";
    public string guid = "";
}

public static class MasterServer
{
    public static string ipAddress = "127.0.0.1";
    public static int port = 23466;
    public static int updateRate = 10;
    public static bool dedicatedServer;
    public static void RequestHostList(string gameTypeName) { ZbNet.RequestHostList(gameTypeName); }
    public static void ClearHostList() { ZbNet.ClearHostList(); }
    public static HostData[] PollHostList() { return ZbNet.hostList; }
    public static void RegisterHost(string gameTypeName, string gameName) { ZbNet.Register(gameTypeName, gameName, ""); }
    public static void RegisterHost(string gameTypeName, string gameName, string comment) { ZbNet.Register(gameTypeName, gameName, comment); }
    public static void UnregisterHost() { ZbNet.Unregister(); }
}

public static class Network
{
    public static NetworkPeerType peerType = NetworkPeerType.Disconnected;
    public static bool isServer { get { return peerType == NetworkPeerType.Server; } }
    public static bool isClient { get { return peerType == NetworkPeerType.Client; } }
    public static NetworkPlayer player { get { return new NetworkPlayer { id = ZbNet.localId }; } }
    public static string incomingPassword = "";
    public static string connectionTesterIP = "";
    public static int connectionTesterPort = 10737;
    public static NetworkPlayer[] connections
    {
        get
        {
            var l = new System.Collections.Generic.List<NetworkPlayer>();
            if (isServer) foreach (var k in ZbNet.playerIp.Keys) if (k != 0) l.Add(new NetworkPlayer { id = k });
            return l.ToArray();
        }
    }
    public static int maxConnections;

    public static void InitializeSecurity() { }

    /// <summary>connections <= 1: offline single player (the original also "hosted" a local server). Otherwise opens an online room.</summary>
    public static NetworkConnectionError InitializeServer(int connections, int listenPort, bool useNat)
    {
        maxConnections = connections;
        peerType = NetworkPeerType.Server;
        if (connections > 1) ZbNet.StartHost(connections, incomingPassword);
        else ZbNet.Shutdown();
        return NetworkConnectionError.NoError;
    }

    /// <summary>The "ip" is the room code (the game's IP text field / server list entries carry it).</summary>
    public static NetworkConnectionError Connect(string ip, int remotePort, string password)
    {
        string code = (ip ?? "").Trim().ToUpperInvariant().Replace(" ", "");
        if (code.Length == 0)
        {
            ZbNet.Defer(() => ZbNet.Notify("OnFailedToConnect", NetworkConnectionError.EmptyConnectTarget));
            return NetworkConnectionError.EmptyConnectTarget;
        }
        peerType = NetworkPeerType.Connecting;
        ZbNet.StartClient(code, password);
        return NetworkConnectionError.NoError;
    }
    public static NetworkConnectionError Connect(string[] ips, int remotePort, string password)
    {
        return Connect(ips != null && ips.Length > 0 ? ips[0] : "", remotePort, password);
    }

    public static void Disconnect() { Disconnect(0); }
    public static void Disconnect(int timeout)
    {
        var was = peerType;
        peerType = NetworkPeerType.Disconnected;
        ZbNet.Shutdown();
        // RakNet reports the local disconnect (on a server too) a moment later.
        if (was == NetworkPeerType.Server || was == NetworkPeerType.Client)
            ZbNet.Defer(() => ZbNet.Notify("OnDisconnectedFromServer", NetworkDisconnection.Disconnected));
    }
    public static void CloseConnection(NetworkPlayer target, bool sendDisconnectionNotification) { ZbNet.Kick(target.id); }
    public static bool HavePublicAddress() { return true; }
    public static int GetAveragePing(NetworkPlayer player) { return ZbNet.GetPing(player.id); }
    public static ConnectionTesterStatus TestConnection(bool forceTest) { return ConnectionTesterStatus.PublicIPIsConnectable; }
    public static void RemoveRPCs(NetworkPlayer player) { ZbNet.RemoveRpcs(player.id); }
    public static void DestroyPlayerObjects(NetworkPlayer player) { ZbNet.DestroyPlayerObjects(player.id); }

    public static UnityEngine.Object Instantiate(UnityEngine.Object prefab, Vector3 position, Quaternion rotation, int group)
    {
        return ZbNet.Instantiate(prefab, position, rotation, group);
    }
    public static void Destroy(NetworkViewID viewID) { ZbNet.DestroyView(viewID.id); }
    public static void Destroy(GameObject gameObject) { ZbNet.DestroyNetworked(gameObject); }
}

/// <summary>Stand-in for the removed NetworkView component. Added on demand by NetView().</summary>
[DefaultExecutionOrder(-32000)]
public class NetworkView : MonoBehaviour
{
    [NonSerialized] public long id;
    [NonSerialized] public int ownerId;
    [NonSerialized] public bool dead;
    internal static bool creatingDead;

    static NetworkView deadView;
    /// <summary>Returned when a view cannot be created (e.g. while the GameObject is being destroyed); every call is a no-op.</summary>
    public static NetworkView Dead
    {
        get
        {
            if (deadView == null)
            {
                creatingDead = true;
                var go = new GameObject("zb-dead-view") { hideFlags = HideFlags.HideAndDontSave };
                deadView = go.AddComponent<NetworkView>();
                creatingDead = false;
                deadView.dead = true;
            }
            return deadView;
        }
    }

    void Awake() { if (!creatingDead) ZbNet.ResolveView(this); }
    void OnDestroy() { if (!dead) ZbNet.UnregisterView(this); }

    public bool isMine { get { return dead || ownerId == ZbNet.localId; } }
    public NetworkPlayer owner { get { return new NetworkPlayer { id = ownerId }; } }
    public NetworkViewID viewID { get { return new NetworkViewID { id = id }; } }

    public void RPC(string name, RPCMode mode, params object[] args)
    {
        if (dead) return;
        ZbNet.SendRpc(this, name, mode, args);
    }

    // Reflection lookups are cached per (type, name, argc); null results are cached too.
    static readonly System.Collections.Generic.Dictionary<(Type, string, int), MethodInfo[]> methodCache =
        new System.Collections.Generic.Dictionary<(Type, string, int), MethodInfo[]>();

    // [RPC]-attributed methods, like Unity; if the decompiled code lost the attribute fall back to the first plain match.
    static MethodInfo[] FindMethods(Type type, string name, int argc)
    {
        var key = (type, name, argc);
        MethodInfo[] found;
        if (methodCache.TryGetValue(key, out found)) return found;
        MethodInfo rpc = null, plain = null;
        for (var t = type; t != null && t != typeof(MonoBehaviour) && rpc == null; t = t.BaseType)
        {
            foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (m.Name != name || m.GetParameters().Length != argc) continue;
                if (m.IsDefined(typeof(RPCAttribute), false)) { rpc = m; break; }
                if (plain == null) plain = m;
            }
        }
        found = rpc != null ? new[] { rpc } : plain != null ? new[] { plain } : new MethodInfo[0];
        methodCache[key] = found;
        return found;
    }

    internal void Dispatch(string name, object[] args)
    {
        if (args == null) args = new object[0];
        MonoBehaviour firstPlain = null; MethodInfo firstPlainM = null;
        bool called = false;
        foreach (var mb in GetComponents<MonoBehaviour>())
        {
            if (mb == null || mb == this) continue;
            var ms = FindMethods(mb.GetType(), name, args.Length);
            if (ms.Length == 0) continue;
            if (!ms[0].IsDefined(typeof(RPCAttribute), false)) { if (firstPlain == null) { firstPlain = mb; firstPlainM = ms[0]; } continue; }
            Invoke(mb, ms[0], args); called = true;
        }
        if (!called && firstPlain != null) { Invoke(firstPlain, firstPlainM, args); called = true; }
        if (!called) Debug.LogWarning("[shim] RPC target not found: " + name + " on " + gameObject.name);
    }

    static void Invoke(MonoBehaviour mb, MethodInfo m, object[] args)
    {
        var ps = m.GetParameters();
        object[] conv = new object[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            object a = args[i];
            Type pt = ps[i].ParameterType;
            if (a != null && !pt.IsInstanceOfType(a) && a is IConvertible && typeof(IConvertible).IsAssignableFrom(pt))
                a = Convert.ChangeType(a, pt);
            conv[i] = a;
        }
        try
        {
            object r = m.Invoke(mb, conv);
            var e = r as IEnumerator;
            if (e != null) mb.StartCoroutine(e);
        }
        catch (TargetInvocationException ex) { Debug.LogException(ex.InnerException ?? ex); }
    }
}

public static class NetworkShimExtensions
{
    public static NetworkView NetView(this Component c)
    {
        var nv = c.GetComponent<NetworkView>();
        if (nv != null) return nv;
        try { nv = c.gameObject.AddComponent<NetworkView>(); } catch (Exception) { nv = null; }
        return nv != null ? nv : NetworkView.Dead;
    }
    public static NetworkView NetView(this GameObject g)
    {
        var nv = g.GetComponent<NetworkView>();
        if (nv != null) return nv;
        try { nv = g.AddComponent<NetworkView>(); } catch (Exception) { nv = null; }
        return nv != null ? nv : NetworkView.Dead;
    }
}
