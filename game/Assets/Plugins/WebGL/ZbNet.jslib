// Browser side of the Zumbi Blocks online transport: one binary WebSocket, polled from C# (see ZbNet.cs).
mergeInto(LibraryManager.library, {
  ZbWsOpen: function (pathPtr) {
    var path = UTF8ToString(pathPtr);
    var s = (window.__zbws = window.__zbws || {});
    if (s.ws) { try { s.ws.onclose = null; s.ws.close(); } catch (e) {} }
    s.queue = []; s.state = 1; s.err = 0;
    var base = (location.protocol === 'https:' ? 'wss://' : 'ws://') + location.host;
    var ws;
    try { ws = new WebSocket(base + path); } catch (e) { s.state = 3; s.ws = null; return; }
    ws.binaryType = 'arraybuffer';
    ws.onopen = function () { if (s.ws === ws) s.state = 2; };
    ws.onmessage = function (e) { if (s.ws === ws && e.data instanceof ArrayBuffer) s.queue.push(new Uint8Array(e.data)); };
    ws.onclose = function () { if (s.ws === ws) s.state = 3; };
    ws.onerror = function () { if (s.ws === ws) s.err = 1; };
    s.ws = ws;
  },
  // 0 = never opened / closed by us, 1 = connecting, 2 = open, 3 = closed or failed
  ZbWsState: function () {
    var s = window.__zbws;
    return s ? (s.state | 0) : 0;
  },
  ZbWsSend: function (ptr, len) {
    var s = window.__zbws;
    if (!s || !s.ws || s.state !== 2) return 0;
    s.ws.send(HEAPU8.slice(ptr, ptr + len));
    return 1;
  },
  // Copies the next received frame into the buffer. Returns its length, 0 if none, -1 if it did not fit (dropped).
  ZbWsRecv: function (ptr, cap) {
    var s = window.__zbws;
    if (!s || !s.queue || !s.queue.length) return 0;
    var m = s.queue.shift();
    if (m.length > cap) return -1;
    HEAPU8.set(m, ptr);
    return m.length;
  },
  ZbWsBuffered: function () {
    var s = window.__zbws;
    return s && s.ws ? s.ws.bufferedAmount | 0 : 0;
  },
  ZbWsClose: function () {
    var s = window.__zbws;
    if (!s) return;
    if (s.ws) { try { s.ws.onclose = null; s.ws.close(); } catch (e) {} }
    s.ws = null; s.state = 0; s.queue = [];
  },
  ZbOrigin: function () {
    var o = location.origin;
    var n = lengthBytesUTF8(o) + 1;
    var p = _malloc(n);
    stringToUTF8(o, p, n);
    return p;
  }
});
