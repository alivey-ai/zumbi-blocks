// Zumbi Blocks WebRTC fast lane (Phase 2, see docs/design/PHASE2-WEBRTC.md and docs/MULTIPLAYER.md "Data channel lane").
//
// One unordered, maxRetransmits-0 RTCDataChannel ("zb-fast", negotiated id 1) between the host and each client carries
// ONLY host->client state frames and echo probes (same bytes the relay would send). Everything else (RPC, INST, DESTROY...)
// stays on the WebSocket relay. Signaling is JSON text frames on that same WebSocket. The WebSocket path stays the reference
// and the fallback: if the channel is not healthy, state simply keeps flowing over the relay.
//
// Cost rule: STUN only (free public servers), no TURN. Peers that cannot connect directly stay on the WebSocket relay.
//
// Page switches: ?p2p=0 kill switch, ?p2p=drop (test: this client page kills its channel 12 s after it opened, once),
// ?p2p=block (test: ICE can never connect), ?life=MS (maxPacketLifeTime instead of maxRetransmits 0),
// ?netsim=RTT,JITTER,LOSS (LOSS % is applied to data frames received over the channel), ?netsimp2p=RTT,JITTER,LOSS (channel only).
//
// Unity polls this object through the shims in Plugins/WebGL/ZbNet.jslib (no JS -> C# callbacks).
(function () {
  'use strict';
  var Q = location.search || '';
  function qp(k) { var m = new RegExp('[?&]' + k + '=([^&]*)').exec(Q); return m ? decodeURIComponent(m[1]) : null; }
  var MODE = qp('p2p');
  var ENABLED = MODE !== '0' && typeof RTCPeerConnection === 'function';
  var ICE = [{ urls: ['stun:stun.cloudflare.com:3478'] }, { urls: ['stun:stun.l.google.com:19302'] }];

  var PROBE_MS = 500;       // probe every direction
  var DEAD_MS = 1500;       // no frame for this long = lane down
  var NEED_GOOD = 3;        // consecutive good probes before the lane counts as up (hysteresis)
  var CONNECT_MS = 8000;    // connect timeout
  var RETRY_AT = [0, 10000, 30000]; // client attempt start times after hello; a lane that had been up and dropped restarts sooner
  var MAX_ATTEMPTS = 6;
  var BUF_HI = 65536, BUF_DEMOTE_MS = 2000;
  var MAX_PEERS = 8;        // MAX_DC_PEERS: further peers stay on the WebSocket relay
  var RXQ_MAX = 4000;
  var DROP_AFTER_MS = 12000;

  function now() { return performance.now(); }
  function log() { try { console.log.apply(console, ['[zbrtc]'].concat([].slice.call(arguments))); } catch (e) {} }

  // --- netsim for the channel ---------------------------------------------------------------
  function parseSim(re) {
    var m = re.exec(Q) || [];
    return { d: (parseFloat(m[1]) || 0) / 2, j: (parseFloat(m[2]) || 0) / 2, loss: parseFloat(m[3]) || 0 };
  }
  var sim = parseSim(/[?&]netsimp2p=([0-9.]+)(?:,([0-9.]+))?(?:,([0-9.]+))?/);
  if (!sim.d && !sim.j && !sim.loss) sim = parseSim(/[?&]netsim=([0-9.]+)(?:,([0-9.]+))?(?:,([0-9.]+))?/);
  var simOn = sim.d > 0 || sim.j > 0;
  function simDelay() { return Math.max(0, sim.d + (Math.random() * 2 - 1) * sim.j); }

  var Z = {
    enabled: ENABLED, mode: MODE, s: null, myId: -1, isHost: false,
    peers: {}, rx: [], lastPeer: -1, lastArrival: 0,
    genCounter: 0, attempts: 0, helloAt: 0, dropped: false, retryTimer: 0, gaveUp: false, everUp: false
  };

  // --- signaling over the existing socket ---------------------------------------------------
  function sendWs(obj) {
    try { var ws = Z.s && Z.s.ws; if (ws && ws.readyState === 1) ws.send(JSON.stringify(obj)); } catch (e) {}
  }
  function sendSig(to, gen, k, extra) {
    var o = { t: 'sig', to: to, gen: gen, k: k };
    if (extra) for (var key in extra) o[key] = extra[key];
    sendWs(o);
  }

  Z.attach = function (s) {
    Z.detach();
    Z.s = s; Z.myId = -1; Z.isHost = false; Z.attempts = 0; Z.dropped = false; Z.gaveUp = false; Z.everUp = false; Z.rx = [];
  };
  Z.detach = function () {
    var ids = Object.keys(Z.peers);
    for (var i = 0; i < ids.length; i++) teardown(Z.peers[ids[i]], true);
    if (Z.retryTimer) { clearTimeout(Z.retryTimer); Z.retryTimer = 0; }
    Z.peers = {}; Z.s = null; Z.rx = []; Z.myId = -1;
  };

  // text frame from the relay (called by the jslib WebSocket handler)
  Z.onText = function (text) {
    if (!ENABLED) return;
    var m; try { m = JSON.parse(text); } catch (e) { return; }
    if (!m) return;
    try {
      if (m.t === 'hello') onHello(m);
      else if (m.t === 'sig') onSig(m);
    } catch (e) { log('signal error', String(e)); }
  };

  function onHello(m) {
    Z.myId = m.id | 0; Z.isHost = !!m.host; Z.helloAt = now();
    log('hello', 'id=' + Z.myId, Z.isHost ? 'host' : 'client', 'mode=' + (MODE || 'auto'));
    if (!Z.isHost) startAttempt();
  }

  // --- peer ---------------------------------------------------------------------------------
  function newPeer(id, gen) {
    return {
      id: id, gen: gen, pc: null, dc: null, up: false, everUp: false, opened: false,
      t0: now(), lastRx: 0, good: 0, seq: 0, pend: {}, rtt: 0, rttAvg: 0, rttMin: 1e9, got: 0, lost: 0,
      rxFrames: 0, rxBytes: 0, txFrames: 0, txBytes: 0, skipped: 0, flips: 0, overSince: 0, lastSendAt: 0, lastRxAt: 0,
      probeTimer: 0, connTimer: 0, watchTimer: 0, remoteSet: false, ice: [], cand: '', torn: false, downSince: 0
    };
  }

  function makePc(P) {
    var cfg = { iceServers: ICE };
    if (MODE === 'block' && !Z.isHost) cfg.iceTransportPolicy = 'relay'; // test: no relay servers exist, so nothing can connect
    var pc = new RTCPeerConnection(cfg);
    P.pc = pc;
    var life = parseInt(qp('life'), 10);
    var opts = { negotiated: true, id: 1, ordered: false };
    if (life > 0) opts.maxPacketLifeTime = life; else opts.maxRetransmits = 0;
    var dc = pc.createDataChannel('zb-fast', opts);
    dc.binaryType = 'arraybuffer';
    P.dc = dc;
    pc.onicecandidate = function (e) {
      if (P.torn || !e.candidate) return;
      sendSig(P.id, P.gen, 'ice', { c: e.candidate.toJSON ? e.candidate.toJSON() : e.candidate });
    };
    pc.onconnectionstatechange = function () {
      if (P.torn) return;
      if (pc.connectionState === 'failed') fail(P, 'ice-failed');
    };
    dc.onopen = function () { if (!P.torn) onOpen(P); };
    dc.onclose = function () { if (!P.torn) fail(P, 'dc-closed'); };
    dc.onerror = function () {};
    dc.onmessage = function (e) { if (!P.torn) onData(P, e); };
    P.connTimer = setTimeout(function () { if (!P.torn && !P.opened) fail(P, 'timeout'); }, Z.isHost ? CONNECT_MS + 4000 : CONNECT_MS);
    return pc;
  }

  function startAttempt() {
    if (!ENABLED || Z.isHost || Z.s === null || Z.gaveUp) return;
    if (Z.peers[0] && !Z.peers[0].torn) return;
    if (Z.attempts >= MAX_ATTEMPTS) { Z.gaveUp = true; log('giving up, staying on the WebSocket relay'); return; }
    Z.attempts++;
    var P = newPeer(0, ++Z.genCounter);
    Z.peers[0] = P;
    var pc;
    try { pc = makePc(P); } catch (e) { log('RTCPeerConnection failed', String(e)); fail(P, 'no-rtc'); return; }
    pc.createOffer().then(function (o) { return pc.setLocalDescription(o); }).then(function () {
      if (P.torn) return;
      sendSig(0, P.gen, 'offer', { sdp: pc.localDescription.sdp });
    }).catch(function (e) { log('offer failed', String(e)); fail(P, 'offer'); });
    log('attempt', Z.attempts, 'gen', P.gen);
  }

  function scheduleRetry(wasUp) {
    if (Z.isHost || Z.gaveUp || Z.retryTimer || Z.s === null) return;
    var when;
    if (wasUp) { when = 2000; Z.attempts = Math.min(Z.attempts, 1); }
    else {
      var next = RETRY_AT[Z.attempts];
      if (next === undefined) { Z.gaveUp = true; log('no more retries, staying on the WebSocket relay'); return; }
      when = Math.max(500, Z.helloAt + next - now());
    }
    Z.retryTimer = setTimeout(function () { Z.retryTimer = 0; startAttempt(); }, when);
  }

  function onSig(m) {
    var from = m.from | 0, gen = m.gen | 0, k = m.k;
    var P = Z.peers[from];
    if (Z.isHost) {
      if (k === 'offer') {
        if (P && !P.torn) { if (gen <= P.gen) return; teardown(P, false); }
        var live = 0; for (var id in Z.peers) if (!Z.peers[id].torn) live++;
        if (live >= MAX_PEERS) { sendSig(from, gen, 'bye'); log('peer cap reached, ' + from + ' stays on the WebSocket'); return; }
        P = newPeer(from, gen); Z.peers[from] = P;
        var pc; try { pc = makePc(P); } catch (e) { fail(P, 'no-rtc'); return; }
        pc.setRemoteDescription({ type: 'offer', sdp: m.sdp }).then(function () {
          P.remoteSet = true; flushIce(P);
          return pc.createAnswer();
        }).then(function (a) { return pc.setLocalDescription(a); }).then(function () {
          if (!P.torn) sendSig(from, P.gen, 'answer', { sdp: pc.localDescription.sdp });
        }).catch(function (e) { log('answer failed', String(e)); fail(P, 'answer'); });
      } else if (k === 'ice') {
        if (P && !P.torn && P.gen === gen) addIce(P, m.c);
      } else if (k === 'bye') { if (P && P.gen === gen) teardown(P, true); }
    } else {
      if (!P || P.torn || P.gen !== gen) return;
      if (k === 'answer') {
        P.pc.setRemoteDescription({ type: 'answer', sdp: m.sdp }).then(function () { P.remoteSet = true; flushIce(P); })
          .catch(function (e) { log('setRemote failed', String(e)); fail(P, 'answer'); });
      } else if (k === 'ice') addIce(P, m.c);
      else if (k === 'bye') { log('host declined the data channel'); teardown(P, true); Z.gaveUp = true; }
    }
  }
  function addIce(P, c) {
    if (!c) return;
    if (!P.remoteSet) { P.ice.push(c); return; }
    P.pc.addIceCandidate(c).catch(function () {});
  }
  function flushIce(P) { var l = P.ice; P.ice = []; for (var i = 0; i < l.length; i++) addIce(P, l[i]); }

  // --- lifecycle ----------------------------------------------------------------------------
  function onOpen(P) {
    P.opened = true; P.lastRx = now();
    clearTimeout(P.connTimer); P.connTimer = 0;
    log('channel open', 'peer ' + P.id, 'after ' + Math.round(now() - P.t0) + ' ms');
    P.probeTimer = setInterval(function () { probe(P); }, PROBE_MS);
    P.watchTimer = setInterval(function () { watch(P); }, 250);
    probe(P);
    pickedCandidate(P);
    if (MODE === 'drop' && !Z.dropped && !Z.isHost) {
      Z.dropped = true;
      setTimeout(function () {
        if (P.torn) return;
        log('TEST: dropping the data channel now');
        try { P.pc.close(); } catch (e) {}
        fail(P, 'test-drop');
      }, DROP_AFTER_MS);
    }
  }

  function pickedCandidate(P) {
    try {
      P.pc.getStats().then(function (rep) {
        var pairId = null, cands = {};
        rep.forEach(function (r) {
          if (r.type === 'transport' && r.selectedCandidatePairId) pairId = r.selectedCandidatePairId;
          if (r.type === 'local-candidate' || r.type === 'remote-candidate') cands[r.id] = r;
        });
        var type = '';
        rep.forEach(function (r) {
          if (r.type === 'candidate-pair' && (r.id === pairId || (!pairId && r.nominated && r.state === 'succeeded'))) {
            var l = cands[r.localCandidateId]; if (l) type = l.candidateType || '';
          }
        });
        P.cand = type;
        if (!Z.isHost) sendWs({ t: 'stat', r: 'ok', c: type, ms: Math.round(now() - P.t0) });
        log('selected candidate type', type || '?');
      }).catch(function () {});
    } catch (e) {}
  }

  function fail(P, why) {
    if (P.torn) return;
    var wasUp = P.everUp;
    log('lane failed', 'peer ' + P.id, why);
    if (!Z.isHost && !P.opened) sendWs({ t: 'stat', r: why.slice(0, 16), c: '', ms: Math.round(now() - P.t0) });
    teardown(P, false);
    if (!Z.isHost) scheduleRetry(wasUp);
  }

  // silent = the other side must not be told (we are leaving, or it told us)
  function teardown(P, silent) {
    if (!P || P.torn) return;
    if (P.up) setUp(P, false, 'teardown', silent);
    P.torn = true;
    clearTimeout(P.connTimer); clearInterval(P.probeTimer); clearInterval(P.watchTimer);
    try { if (P.dc) { P.dc.onmessage = P.dc.onclose = P.dc.onopen = null; P.dc.close(); } } catch (e) {}
    try { if (P.pc) { P.pc.onicecandidate = P.pc.onconnectionstatechange = null; P.pc.close(); } } catch (e) {}
    if (!silent) sendSig(Z.isHost ? P.id : 0, P.gen, 'bye');
  }

  // lane up / down. Up needs NEED_GOOD consecutive good probes; down is the silence watchdog (or a send backlog).
  function setUp(P, up, why, silent) {
    if (P.up === up) return;
    P.up = up; P.flips++;
    if (up) { P.everUp = true; Z.everUp = true; P.downSince = 0; }
    else { P.good = 0; P.downSince = now(); }
    log('lane ' + (up ? 'UP' : 'DOWN'), 'peer ' + P.id, why || '');
    if (silent) return;
    if (Z.isHost) { if (!up) sendWs({ t: 'dc', to: P.id, up: false }); } // the host can only clear the relay's flag
    else sendWs({ t: 'dc', up: up });
  }

  function watch(P) {
    if (P.torn) return;
    var t = now();
    if (P.up && t - P.lastRx > DEAD_MS) setUp(P, false, 'silent ' + Math.round(t - P.lastRx) + ' ms');
    else if (!P.up && P.opened && P.downSince && t - P.downSince > 12000 && t - P.lastRx > 6000) fail(P, 'down-too-long');
    for (var k in P.pend) if (t - P.pend[k] > 1500) { delete P.pend[k]; P.lost++; } // probes that never came back
  }

  // --- probes: 00 00 type seq(u32) t(f64). A frame with length 0 can never be a data frame, so no ambiguity. -------------
  function probe(P) {
    if (P.torn || !P.dc || P.dc.readyState !== 'open') return;
    var b = new ArrayBuffer(15), dv = new DataView(b);
    dv.setUint8(2, 1); dv.setUint32(3, P.seq, true); dv.setFloat64(7, now(), true);
    P.pend[P.seq] = now(); P.seq++;
    rawSend(P, b, false);
  }
  function onProbe(P, dv) {
    var type = dv.getUint8(2);
    if (type === 1) { // ping: answer at once, from JS (works while the game loop is throttled)
      var b = new ArrayBuffer(15), o = new DataView(b);
      o.setUint8(2, 2); o.setUint32(3, dv.getUint32(3, true), true); o.setFloat64(7, dv.getFloat64(7, true), true);
      rawSend(P, b, false);
    } else if (type === 2) {
      var seq = dv.getUint32(3, true);
      if (!(seq in P.pend)) return;
      delete P.pend[seq]; P.got++;
      var rtt = now() - dv.getFloat64(7, true) + (simOn ? simDelay() : 0);
      P.rtt = rtt; P.rttAvg = P.rttAvg ? P.rttAvg + (rtt - P.rttAvg) / 8 : rtt; if (rtt < P.rttMin) P.rttMin = rtt;
      if (!P.up && ++P.good >= NEED_GOOD) setUp(P, true, 'probes ok, rtt ' + Math.round(rtt) + ' ms');
    }
  }

  function rawSend(P, buf, countData) {
    var dc = P.dc;
    if (!dc || dc.readyState !== 'open') return false;
    if (Z.stalled) return true;
    var doSend = function () { try { if (!P.torn && dc.readyState === 'open') dc.send(buf); } catch (e) {} };
    if (!simOn) doSend();
    else { var t = now(), at = Math.max(P.lastSendAt, t + simDelay()); P.lastSendAt = at; setTimeout(doSend, Math.max(0, at - t)); }
    if (countData) { P.txFrames++; P.txBytes += buf.byteLength; }
    return true;
  }

  function onData(P, e) {
    if (Z.stalled) return;
    var t = now();
    P.lastRx = t;
    if (!(e.data instanceof ArrayBuffer) || e.data.byteLength < 3) return;
    var dv = new DataView(e.data);
    if (dv.getUint8(0) === 0 && dv.getUint8(1) === 0) { if (e.data.byteLength === 15) onProbe(P, dv); return; }
    if (sim.loss > 0 && Math.random() * 100 < sim.loss) return;
    var at = t;
    if (simOn) { at = Math.max(P.lastRxAt, t + simDelay()); P.lastRxAt = at; }
    P.rxFrames++; P.rxBytes += e.data.byteLength;
    if (Z.rx.length >= RXQ_MAX) Z.rx.shift();
    Z.rx.push({ t: at, d: new Uint8Array(e.data), peer: P.id });
  }

  // test hook: window.zbrtc.stall(true) makes this page's channel silently swallow everything (both directions), like a dead network path
  Z.stalled = false;
  Z.stall = function (on) { Z.stalled = !!on; log('TEST: channel ' + (on ? 'stalled' : 'released')); };
  Z.closePeer = function (id) { var P = Z.peers[id]; if (P) { teardown(P, true); delete Z.peers[id]; } };

  // --- API used by the jslib shims ------------------------------------------------------------
  Z.peerUp = function (id) { var P = Z.peers[id]; return P && !P.torn && P.up ? 1 : 0; };
  Z.anyUp = function () { for (var id in Z.peers) { var P = Z.peers[id]; if (!P.torn && P.up) return 1; } return 0; };
  Z.send = function (id, u8) {
    var P = Z.peers[id];
    if (!P || P.torn || !P.up) return 0;
    var dc = P.dc;
    if (!dc || dc.readyState !== 'open') return 0;
    if (dc.bufferedAmount > BUF_HI) {
      P.skipped++;
      var t = now();
      if (!P.overSince) P.overSince = t;
      else if (t - P.overSince > BUF_DEMOTE_MS) setUp(P, false, 'send backlog');
      return 0;
    }
    P.overSince = 0;
    return rawSend(P, u8.buffer.slice(u8.byteOffset, u8.byteOffset + u8.byteLength), true) ? 1 : 0;
  };
  Z.recv = function (ptr, cap, heap) {
    var q = Z.rx;
    if (!q.length) return 0;
    var head = q[0];
    if (head.t > now()) return 0;
    q.shift();
    Z.lastArrival = head.t; Z.lastPeer = head.peer;
    if (head.d.length > cap) return -1;
    heap.set(head.d, ptr);
    return head.d.length;
  };
  // numeric stats for the overlay: 0 state (0 none, 1 connecting, 2 open but not up, 3 up, 4 closed/failed), 1 rtt, 2 avg rtt, 3 probe loss %,
  // 4 rx frames, 5 rx bytes, 6 tx frames, 7 tx bytes, 8 bufferedAmount, 9 skipped sends, 10 lane flips, 11 attempts
  Z.stat = function (id, which) {
    var P = Z.peers[id];
    if (!P) return which === 0 ? (Z.gaveUp ? 4 : 0) : 0;
    switch (which) {
      case 0: return P.torn ? 4 : P.up ? 3 : P.opened ? 2 : 1;
      case 1: return P.rtt; case 2: return P.rttAvg;
      case 3: { var n = P.got + P.lost; return n > 0 ? 100 * P.lost / n : 0; }
      case 4: return P.rxFrames; case 5: return P.rxBytes; case 6: return P.txFrames; case 7: return P.txBytes;
      case 8: return P.dc ? P.dc.bufferedAmount : 0; case 9: return P.skipped; case 10: return P.flips; case 11: return Z.attempts;
    }
    return 0;
  };
  // for tests / debugging: window.zbrtc.info()
  Z.info = function () {
    var o = { enabled: ENABLED, mode: MODE || 'auto', id: Z.myId, host: Z.isHost, attempts: Z.attempts, gaveUp: Z.gaveUp, everUp: Z.everUp, peers: {} };
    for (var id in Z.peers) {
      var P = Z.peers[id];
      o.peers[id] = { state: P.torn ? 'closed' : P.up ? 'up' : P.opened ? 'open' : 'connecting', up: P.up, rtt: Math.round(P.rtt), rttAvg: Math.round(P.rttAvg), lossPct: Z.stat(id, 3),
        rxFrames: P.rxFrames, rxBytes: P.rxBytes, txFrames: P.txFrames, txBytes: P.txBytes, buffered: Z.stat(id, 8), skipped: P.skipped, flips: P.flips, cand: P.cand };
    }
    return o;
  };

  window.zbrtc = Z;
})();
