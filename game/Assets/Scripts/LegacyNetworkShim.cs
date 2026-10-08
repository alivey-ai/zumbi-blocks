// TODO(multiplayer phase): Unity's legacy networking (UnityEngine.Network / NetworkView / RPC /
// MasterServer, RakNet based) was removed from Unity long before Unity 6. The original game was
// written against it everywhere (319 networkView.RPC calls, 36 Network.Instantiate calls).
//
// This file is an OFFLINE LOOPBACK SHIM that re-creates just enough of that API for single player:
//   - the game "hosts" a one-player server on itself (Network.InitializeServer(1, ...)), exactly like the original single player mode did
//   - Network.Instantiate == Object.Instantiate
//   - networkView.RPC(name, mode, args): Server/All/AllBuffered are invoked locally by reflection,
//     Others/OthersBuffered go nowhere (there are no other peers)
//   - MasterServer returns an empty host list
// Real online play must replace this with a real transport (see docs/RECOVERY.md).
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

public struct NetworkPlayer
{
    public string ipAddress { get { return "127.0.0.1"; } }
    public int port { get { return 0; } }
    public string guid { get { return "local"; } }
    public string externalIP { get { return "127.0.0.1"; } }
    public int externalPort { get { return 0; } }
    public static bool operator ==(NetworkPlayer a, NetworkPlayer b) { return true; }
    public static bool operator !=(NetworkPlayer a, NetworkPlayer b) { return false; }
    public override bool Equals(object o) { return o is NetworkPlayer; }
    public override int GetHashCode() { return 0; }
    public override string ToString() { return "0"; }
}

public struct NetworkViewID
{
    public GameObject target;
    public static NetworkViewID unassigned { get { return default(NetworkViewID); } }
    public bool isMine { get { return true; } }
    public NetworkPlayer owner { get { return default(NetworkPlayer); } }
}

public class HostData
{
    public bool useNat;
    public string gameType = "";
    public string gameName = "";
    public int connectedPlayers;
    public int playerLimit;
    public string[] ip = new string[0];
    public int port;
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
    public static void RequestHostList(string gameTypeName) { }
    public static void ClearHostList() { }
    public static HostData[] PollHostList() { return new HostData[0]; }
    public static void RegisterHost(string gameTypeName, string gameName) { }
    public static void RegisterHost(string gameTypeName, string gameName, string comment) { }
    public static void UnregisterHost() { }
}

public static class Network
{
    public static NetworkPeerType peerType = NetworkPeerType.Disconnected;
    public static bool isServer { get { return peerType == NetworkPeerType.Server; } }
    public static bool isClient { get { return peerType == NetworkPeerType.Client; } }
    public static NetworkPlayer player { get { return default(NetworkPlayer); } }
    public static string incomingPassword = "";
    public static string connectionTesterIP = "";
    public static int connectionTesterPort = 10737;
    public static NetworkPlayer[] connections { get { return new NetworkPlayer[0]; } }
    public static int maxConnections;

    public static void InitializeSecurity() { }
    public static NetworkConnectionError InitializeServer(int connections, int listenPort, bool useNat)
    {
        maxConnections = connections;
        peerType = NetworkPeerType.Server;
        return NetworkConnectionError.NoError;
    }
    public static NetworkConnectionError Connect(string ip, int remotePort, string password)
    {
        // TODO(multiplayer): no transport yet.
        return NetworkConnectionError.ConnectionFailed;
    }
    public static NetworkConnectionError Connect(string[] ips, int remotePort, string password)
    {
        return NetworkConnectionError.ConnectionFailed;
    }
    public static void Disconnect() { peerType = NetworkPeerType.Disconnected; }
    public static void Disconnect(int timeout) { peerType = NetworkPeerType.Disconnected; }
    public static void CloseConnection(NetworkPlayer target, bool sendDisconnectionNotification) { }
    public static bool HavePublicAddress() { return true; }
    public static int GetAveragePing(NetworkPlayer player) { return 0; }
    public static ConnectionTesterStatus TestConnection(bool forceTest) { return ConnectionTesterStatus.PublicIPIsConnectable; }
    public static void RemoveRPCs(NetworkPlayer player) { }
    public static void DestroyPlayerObjects(NetworkPlayer player) { }

    public static UnityEngine.Object Instantiate(UnityEngine.Object prefab, Vector3 position, Quaternion rotation, int group)
    {
        return UnityEngine.Object.Instantiate(prefab, position, rotation);
    }
    public static void Destroy(NetworkViewID viewID)
    {
        if (viewID.target != null) UnityEngine.Object.Destroy(viewID.target);
    }
    public static void Destroy(GameObject gameObject)
    {
        if (gameObject != null) UnityEngine.Object.Destroy(gameObject);
    }
}

/// <summary>Stand-in for the removed NetworkView component. Added on demand by NetView().</summary>
public class NetworkView : MonoBehaviour
{
    public bool isMine { get { return true; } }
    public NetworkPlayer owner { get { return default(NetworkPlayer); } }
    public NetworkViewID viewID { get { return new NetworkViewID { target = gameObject }; } }

    public void RPC(string name, RPCMode mode, params object[] args)
    {
        if (mode == RPCMode.Others || mode == RPCMode.OthersBuffered) return; // nobody else to tell
        if (mode == RPCMode.Server && !Network.isServer) return;
        Dispatch(name, args);
    }

    // Reflection lookups are cached per (type, name, argc); null results are cached too.
    static readonly System.Collections.Generic.Dictionary<(Type, string, int), MethodInfo> methodCache =
        new System.Collections.Generic.Dictionary<(Type, string, int), MethodInfo>();

    static MethodInfo FindMethod(Type type, string name, int argc)
    {
        var key = (type, name, argc);
        MethodInfo found;
        if (methodCache.TryGetValue(key, out found)) return found;
        for (var t = type; t != null && t != typeof(MonoBehaviour) && found == null; t = t.BaseType)
        {
            foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (m.Name == name && m.GetParameters().Length == argc) { found = m; break; }
            }
        }
        methodCache[key] = found;
        return found;
    }

    void Dispatch(string name, object[] args)
    {
        if (args == null) args = new object[0];
        foreach (var mb in GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;
            var m = FindMethod(mb.GetType(), name, args.Length);
            if (m == null) continue;
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
            object r = m.Invoke(mb, conv);
            var e = r as IEnumerator;
            if (e != null) mb.StartCoroutine(e);
            return;
        }
        Debug.LogWarning("[shim] RPC target not found: " + name + " on " + gameObject.name);
    }
}

public static class NetworkShimExtensions
{
    public static NetworkView NetView(this Component c)
    {
        var nv = c.GetComponent<NetworkView>();
        if (nv == null) nv = c.gameObject.AddComponent<NetworkView>();
        return nv;
    }
    public static NetworkView NetView(this GameObject g)
    {
        var nv = g.GetComponent<NetworkView>();
        if (nv == null) nv = g.AddComponent<NetworkView>();
        return nv;
    }
}
