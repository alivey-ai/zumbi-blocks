// Player profiles (docs/ACCOUNTS.md), browser side. The session itself is an httpOnly cookie the game never sees.
//   ZbAcctStage(body): the latest not yet uploaded cloud save ("{rev, data}" JSON, "" = nothing pending). When the page goes away (tab closed,
//   reload, navigation) it is sent with navigator.sendBeacon, which carries the cookie and survives the unload. The server refuses it if it is
//   stale (revision check), so a beacon can never overwrite newer data.
//   ZbAcctCopy(text): copy to the clipboard (the recovery code screen).
mergeInto(LibraryManager.library, {
  ZbAcctStage: function (bodyPtr) {
    var s = (window.__zbacct = window.__zbacct || {});
    s.body = UTF8ToString(bodyPtr);
    if (!s.hooked) {
      s.hooked = true;
      window.addEventListener('pagehide', function () {
        if (!s.body) return;
        try { navigator.sendBeacon('/api/account/save', new Blob([s.body], { type: 'application/json' })); } catch (e) {}
        s.body = '';
      });
    }
  },
  ZbAcctCopy: function (textPtr) {
    var t = UTF8ToString(textPtr);
    try { navigator.clipboard.writeText(t); } catch (e) {}
  },
});
