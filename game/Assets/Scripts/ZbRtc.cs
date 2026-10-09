// C# side of the WebRTC fast lane (Phase 2, docs/design/PHASE2-WEBRTC.md). The connection logic lives in the page
// (WebGLTemplates/ZumbiBlocks/zbrtc.js); this is a polled wrapper over the jslib shims. Outside a WebGL player there is no lane.
using System.Runtime.InteropServices;

public static class ZbRtc
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern int ZbRtcPeerUp(int id);
    [DllImport("__Internal")] static extern int ZbRtcAnyUp();
    [DllImport("__Internal")] static extern int ZbRtcSend(int id, byte[] data, int len);
    [DllImport("__Internal")] static extern int ZbRtcRecv(byte[] buf, int cap);
    [DllImport("__Internal")] static extern int ZbRtcLastPeer();
    [DllImport("__Internal")] static extern double ZbRtcLastArrival();
    [DllImport("__Internal")] static extern void ZbRtcClosePeer(int id);
    [DllImport("__Internal")] static extern double ZbRtcStat(int id, int which);
    public static bool PeerUp(int id) { return ZbRtcPeerUp(id) == 1; }
    public static bool AnyUp() { return ZbRtcAnyUp() == 1; }
    public static bool Send(int id, byte[] d, int len) { return ZbRtcSend(id, d, len) == 1; }
    public static int Recv(byte[] b) { return ZbRtcRecv(b, b.Length); }
    public static int LastPeer() { return ZbRtcLastPeer(); }
    public static double LastArrival() { return ZbRtcLastArrival(); }
    public static void ClosePeer(int id) { ZbRtcClosePeer(id); }
    // 0 state (0 none, 1 connecting, 2 open not up, 3 up, 4 closed), 1 rtt, 2 avg rtt, 3 probe loss %, 4 rx frames, 5 rx bytes, 6 tx frames, 7 tx bytes,
    // 8 bufferedAmount, 9 skipped sends, 10 lane flips, 11 attempts
    public static double Stat(int id, int which) { return ZbRtcStat(id, which); }
#else
    public static bool PeerUp(int id) { return false; }
    public static bool AnyUp() { return false; }
    public static bool Send(int id, byte[] d, int len) { return false; }
    public static int Recv(byte[] b) { return 0; }
    public static int LastPeer() { return -1; }
    public static double LastArrival() { return 0; }
    public static void ClosePeer(int id) { }
    public static double Stat(int id, int which) { return 0; }
#endif
}
