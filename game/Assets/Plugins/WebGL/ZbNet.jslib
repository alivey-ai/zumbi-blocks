// Browser side of the Zumbi Blocks online transport: one binary WebSocket, polled from C# (see ZbNet.cs).
//
// Latency test mode: add ?netsim=RTT,JITTER to the page URL (e.g. ?netsim=120,30). Every message this browser sends or
// receives is delayed by RTT/2 plus a random +-JITTER/2 (so this browser's round trip to the relay grows by RTT, +-JITTER),
// in order like TCP (a late message holds back the ones behind it). 0 or missing = off.
mergeInto(LibraryManager.library, {
  ZbWsOpen: function (pathPtr) {
    var path = UTF8ToString(pathPtr);
    var s = (window.__zbws = window.__zbws || {});
    if (s.ws) { try { s.ws.onclose = null; s.ws.close(); } catch (e) {} }
    s.queue = []; s.state = 1; s.err = 0; s.lastArrival = 0; s.pendingOut = 0; s.closed = false;
    s.lastSendAt = 0; s.lastRecvAt = 0;
    var ns = (/[?&]netsim=([0-9.]+)(?:,([0-9.]+))?/.exec(location.search) || []);
    s.simD = (parseFloat(ns[1]) || 0) / 2; s.simJ = (parseFloat(ns[2]) || 0) / 2;
    s.sim = s.simD > 0 || s.simJ > 0;
    if (window.zbrtc) { try { window.zbrtc.attach(s); } catch (e) {} }
    var base = (location.protocol === 'https:' ? 'wss://' : 'ws://') + location.host;
    var ws;
    try { ws = new WebSocket(base + path); } catch (e) { s.state = 3; s.ws = null; return; }
    ws.binaryType = 'arraybuffer';
    ws.onopen = function () { if (s.ws === ws) s.state = 2; };
    ws.onmessage = function (e) {
      if (s.ws !== ws) return;
      if (typeof e.data === 'string') { if (window.zbrtc) window.zbrtc.onText(e.data); return; } // WebRTC signaling (zbrtc.js)
      if (!(e.data instanceof ArrayBuffer)) return;
      var now = performance.now();
      var at = now;
      if (s.sim) {
        at = Math.max(s.lastRecvAt, now + Math.max(0, s.simD + (Math.random() * 2 - 1) * s.simJ));
        s.lastRecvAt = at;
      }
      s.queue.push({ t: at, d: new Uint8Array(e.data) });
    };
    ws.onclose = function () { if (s.ws === ws) { s.closed = true; if (s.state !== 2 || !s.queue.length) s.state = 3; } };
    ws.onerror = function () { if (s.ws === ws) s.err = 1; };
    s.ws = ws;
  },
  // 0 = never opened / closed by us, 1 = connecting, 2 = open, 3 = closed or failed.
  // A socket that closed reports "open" until the frames it received before closing (e.g. the reject reason) were read.
  ZbWsState: function () {
    var s = window.__zbws;
    if (!s) return 0;
    if (s.closed && s.state === 2 && !s.queue.length) s.state = 3;
    return s.state | 0;
  },
  ZbWsSend: function (ptr, len) {
    var s = window.__zbws;
    if (!s || !s.ws || s.state !== 2 || s.closed) return 0;
    var data = HEAPU8.slice(ptr, ptr + len);
    if (!s.sim) { s.ws.send(data); return 1; }
    var now = performance.now();
    var at = Math.max(s.lastSendAt, now + Math.max(0, s.simD + (Math.random() * 2 - 1) * s.simJ));
    s.lastSendAt = at;
    s.pendingOut += len;
    var ws = s.ws;
    setTimeout(function () { s.pendingOut -= len; if (s.ws === ws && ws.readyState === 1) { try { ws.send(data); } catch (e) {} } }, Math.max(0, at - now));
    return 1;
  },
  // Copies the next received frame into the buffer. Returns its length, 0 if none (or not yet "arrived" under netsim),
  // -1 if it did not fit (dropped).
  ZbWsRecv: function (ptr, cap) {
    var s = window.__zbws;
    if (!s || !s.queue || !s.queue.length) return 0;
    var head = s.queue[0];
    if (head.t > performance.now()) return 0;
    s.queue.shift();
    s.lastArrival = head.t;
    var m = head.d;
    if (m.length > cap) return -1;
    HEAPU8.set(m, ptr);
    return m.length;
  },
  // performance.now() time (ms) at which the frame returned by the last ZbWsRecv arrived (after netsim delay).
  ZbWsLastArrival: function () {
    var s = window.__zbws;
    return s ? s.lastArrival : 0;
  },
  // performance.now() in ms: the common clock for send stamps and arrival times.
  ZbNow: function () { return performance.now(); },
  // Bytes handed to the browser's WebSocket but not sent yet (plus those held back by netsim).
  ZbWsBuffered: function () {
    var s = window.__zbws;
    return s && s.ws ? ((s.ws.bufferedAmount | 0) + (s.pendingOut | 0)) : 0;
  },
  ZbNetsimMs: function () {
    var s = window.__zbws;
    return s ? ((s.simD || 0) * 2) : 0;
  },
  ZbWsClose: function () {
    var s = window.__zbws;
    if (!s) return;
    if (window.zbrtc) { try { window.zbrtc.detach(); } catch (e) {} }
    if (s.ws) { try { s.ws.onclose = null; s.ws.close(); } catch (e) {} }
    s.ws = null; s.state = 0; s.queue = []; s.closed = false; s.pendingOut = 0;
  },
  // ---- WebRTC fast lane (zbrtc.js in the page template): host->client state and echo probes. All polled, no callbacks. ----
  ZbRtcPeerUp: function (id) { return window.zbrtc ? window.zbrtc.peerUp(id) : 0; },
  ZbRtcAnyUp: function () { return window.zbrtc ? window.zbrtc.anyUp() : 0; },
  ZbRtcSend: function (id, ptr, len) { return window.zbrtc ? window.zbrtc.send(id, HEAPU8.slice(ptr, ptr + len)) : 0; },
  ZbRtcRecv: function (ptr, cap) { return window.zbrtc ? window.zbrtc.recv(ptr, cap, HEAPU8) : 0; },
  ZbRtcLastPeer: function () { return window.zbrtc ? window.zbrtc.lastPeer : -1; },
  ZbRtcLastArrival: function () { return window.zbrtc ? window.zbrtc.lastArrival : 0; },
  ZbRtcClosePeer: function (id) { if (window.zbrtc) window.zbrtc.closePeer(id); },
  ZbRtcStat: function (id, which) { return window.zbrtc ? window.zbrtc.stat(id, which) : 0; },
  ZbOrigin: function () {
    var o = location.origin;
    var n = lengthBytesUTF8(o) + 1;
    var p = _malloc(n);
    stringToUTF8(o, p, n);
    return p;
  },
  // The page's query string ("?netsim=120,30&legacy=1"), for the debug/test switches in ZbNet.cs.
  ZbQuery: function () {
    var o = location.search || '';
    var n = lengthBytesUTF8(o) + 1;
    var p = _malloc(n);
    stringToUTF8(o, p, n);
    return p;
  }
});
