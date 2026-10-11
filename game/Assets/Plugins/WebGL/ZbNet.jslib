// Browser side of the Zumbi Blocks online transport: one binary WebSocket, polled from C# (see ZbNet.cs).
//
// Latency test mode: add ?netsim=RTT,JITTER to the page URL (e.g. ?netsim=120,30). Every message this browser sends or
// receives is delayed by RTT/2 plus a random +-JITTER/2 (so this browser's round trip to the relay grows by RTT, +-JITTER),
// in order like TCP (a late message holds back the ones behind it). 0 or missing = off.
mergeInto(LibraryManager.library, {
  // resume = 1: the same session takes its slot back after a drop (phase 4). The WebRTC lane (zbrtc.js) is left alone then: a data channel
  // does not depend on the WebSocket and usually survives it; the relay's new "hello" lets it re-announce itself.
  ZbWsOpen: function (pathPtr, resume) {
    var path = UTF8ToString(pathPtr);
    var s = (window.__zbws = window.__zbws || {});
    if (s.ws) { try { s.ws.onclose = null; s.ws.close(1000, 'bye'); } catch (e) {} }
    // Tab visibility goes to the relay as a complete frame straight on the socket (C_VIS = 17: u8 visible, u8 fps, u16 rtt), because the game
    // does not run while the tab is hidden. The relay never drops a hidden player for silence, and tells everybody when the HOST is hidden.
    var visFrame = function () { return new Uint8Array([5, 0, 17, document.visibilityState === 'visible' ? 1 : 0, 0, 0, 0]); };
    if (!s.visHooked) {
      s.visHooked = true;
      document.addEventListener('visibilitychange', function () { var w = s.ws; if (w && w.readyState === 1) { try { w.send(visFrame()); } catch (e) {} } });
    }
    // while hidden nobody drains the receive queue: drop frames that only carry lossy data (state, echo, pong, unbuffered rpc); buffered ones are kept
    var lossy = function (d) {
      var o = 0;
      while (o + 2 <= d.length) {
        var len = d[o] | (d[o + 1] << 8); o += 2; var op = d[o];
        if (!(op === 0x8b || op === 0x8d || op === 0x8c || op === 0x88 || op === 0x89 || op === 0x95 || (op === 0x82 && len > 5 && (d[o + 5] === 1 || d[o + 5] === 2)))) return false;
        o += len;
      }
      return true;
    };
    s.queue = []; s.state = 1; s.err = 0; s.lastArrival = 0; s.pendingOut = 0; s.closed = false; s.lastTx = performance.now();
    // Keepalive (free plan budget, docs/MULTIPLAYER.md): when this page has sent nothing for 4.5 s it sends the text "zbka"; the Durable Object
    // runtime answers it by itself (setWebSocketAutoResponse) without waking the object, and the relay's silence check reads that timestamp.
    if (!s.kaTimer) s.kaTimer = setInterval(function () {
      var w = s.ws; if (!w || w.readyState !== 1 || s.state !== 2) return;
      if (performance.now() - (s.lastTx || 0) > 4500) { try { w.send('zbka'); s.lastTx = performance.now(); } catch (e) {} }
    }, 1000);
    s.lastSendAt = 0; s.lastRecvAt = 0;
    var ns = (/[?&]netsim=([0-9.]+)(?:,([0-9.]+))?/.exec(location.search) || []);
    s.simD = (parseFloat(ns[1]) || 0) / 2; s.simJ = (parseFloat(ns[2]) || 0) / 2;
    s.sim = s.simD > 0 || s.simJ > 0;
    if (window.zbrtc && !resume) { try { window.zbrtc.attach(s); } catch (e) {} }
    var base = (location.protocol === 'https:' ? 'wss://' : 'ws://') + location.host;
    var ws;
    try { ws = new WebSocket(base + path); } catch (e) { s.state = 3; s.ws = null; return; }
    ws.binaryType = 'arraybuffer';
    ws.onopen = function () { if (s.ws === ws) { s.state = 2; if (document.visibilityState !== 'visible') { try { ws.send(visFrame()); } catch (e) {} } } };
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
      var arr = new Uint8Array(e.data);
      if (document.visibilityState !== 'visible' && s.queue.length > 600 && lossy(arr)) return;
      s.queue.push({ t: at, d: arr });
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
    s.lastTx = performance.now();
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
  // Local wall clock time ("HH:MM", the browser's time zone) of the next 00:00 UTC: when the free plan's daily Durable Object quota resets.
  ZbQuotaResetLocal: function () {
    var t = '';
    try { var n = new Date(); var r = new Date(Date.UTC(n.getUTCFullYear(), n.getUTCMonth(), n.getUTCDate() + 1)); t = r.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }); } catch (e) { t = '00:00 UTC'; }
    var n2 = lengthBytesUTF8(t) + 1; var p = _malloc(n2); stringToUTF8(t, p, n2); return p;
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
    if (s.ws) { try { s.ws.onclose = null; s.ws.close(1000, 'bye'); } catch (e) {} } // 1000: an intentional leave, the relay frees the slot at once
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
  // Resume token of this tab ("CODE.ID.TOKEN") in sessionStorage: a reloaded tab can free the slot its previous incarnation left in limbo.
  ZbResumeStore: function (ptr) {
    try { var v = UTF8ToString(ptr); if (v) sessionStorage.setItem('zb.resume', v); else sessionStorage.removeItem('zb.resume'); } catch (e) {}
  },
  ZbResumeLoad: function () {
    var v = ''; try { v = sessionStorage.getItem('zb.resume') || ''; } catch (e) {}
    var n = lengthBytesUTF8(v) + 1; var p = _malloc(n); stringToUTF8(v, p, n); return p;
  },
  ZbOrigin: function () {
    var o = location.origin;
    var n = lengthBytesUTF8(o) + 1;
    var p = _malloc(n);
    stringToUTF8(o, p, n);
    return p;
  },
  // Invite link bar of the page (index.html): shown while in a room. hosting: 1 = this tab is the host.
  ZbSetRoomUi: function (codePtr, hosting) {
    var code = UTF8ToString(codePtr);
    if (window.zbSetRoom) { try { window.zbSetRoom(code, hosting | 0); } catch (e) {} }
  },
  // Drop ?room= from the address once the invite was used, so a refresh after leaving does not join again.
  ZbClearInvite: function () {
    try {
      var u = new URL(location.href); u.searchParams.delete('room'); u.searchParams.delete('pw');
      history.replaceState(null, '', u.pathname + (u.search || '') + u.hash);
    } catch (e) {}
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
