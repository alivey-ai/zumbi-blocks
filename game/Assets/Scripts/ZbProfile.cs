// Player profiles + cloud save (docs/ACCOUNTS.md). Sits around the original game like ZbMenu / ZbBoard: sc_gamecontrol only gets three one-line
// hooks (save() -> OnSave, the main menu -> MainMenuButton, OnGUI -> Draw).
//   * a profile = unique name (the nickname) + password; the session is an httpOnly cookie (the game never sees it; UnityWebRequest, the relay
//     WebSocket and sendBeacon all carry it because everything is same-origin)
//   * everything sc_gamecontrol.save() writes (money, items, look, nick, sensitivity, volume, auto aim / reload) + this port's own settings
//     (FOV, invert Y, graphics, waves / loot) is uploaded after every save (at most every 10 s, and with sendBeacon when the page goes away)
//     and downloaded on login (the server copy wins). Every upload names the revision it is based on: the server refuses stale writes (an old tab)
//     and hands back its newer copy, which then replaces the local one.
//   * logging in keeps this browser's guest save aside (ZBX_guest_save) and brings it back on log out; the first signup moves the guest save
//     into the new profile. Guest play without a profile is unchanged (PlayerPrefs only).
//   states 50 profile, 51 log in, 52 create, 53 forgot password, 54 recovery code, 55 "keep profile save", 56 delete profile
using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

public static class ZbProfile
{
    public const int ST_PROFILE = 50, ST_LOGIN = 51, ST_SIGNUP = 52, ST_RECOVER = 53, ST_CODE = 54, ST_KEEP = 55, ST_DELETE = 56;
    const float UPLOAD_GAP = 10f;     // seconds between two uploads at least (free plan: one D1 write per upload)

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void ZbAcctStage(string body);
    [DllImport("__Internal")] static extern void ZbAcctCopy(string text);
#else
    static void ZbAcctStage(string body) { }
    static void ZbAcctCopy(string text) { }
#endif

    // ---- data -------------------------------------------------------------------------------------------------------------------------
    [Serializable] public class SaveData
    {
        public int v;                                   // 1; 0 = "no data" (JsonUtility turns a JSON null into a default object)
        public string nick; public int nickok;
        public int beard, beardc, skin, shirt, money, autoaim, autoreload;
        public float sound, sense;
        public int[] inv;
        public int fov, invy, quality, cwaves, cloot;
    }
    [Serializable] public class Stats { public int bestWave, kills, games; }
    [Serializable] class Prof { public bool ok; public string name, error, recovery; public int rev; public SaveData data; public Stats stats; }

    public static bool loggedIn;
    public static string name = "";
    public static Stats stats = new Stats();
    static int rev;                     // server revision the local save is based on
    static bool dirty;                  // local changes not uploaded yet
    static bool applying;               // writing a downloaded save: save() must not trigger an upload
    static bool uploading, booted, busy;
    static int saveGen;                 // bumps on every save(); an upload clears `dirty` only if nothing was saved meanwhile
    static float dueAt = -1f, lastUpload = -999f;
    static string msg = ""; static Color msgColor = Color.white;
    static string fName = "", fPw = "", fPw2 = "", fCode = "", recovery = "";
    static int pending;                 // ST_KEEP: what to do after "Keep profile save" (ST_LOGIN or ST_RECOVER)

    static void LoadLocal()
    {
        name = PlayerPrefs.GetString("ZBX_acct_name", "");
        rev = PlayerPrefs.GetInt("ZBX_acct_rev", 0);
        dirty = PlayerPrefs.GetInt("ZBX_acct_dirty", 0) == 1;
        loggedIn = name.Length > 0;
    }
    static void StoreLocal()
    {
        PlayerPrefs.SetString("ZBX_acct_name", loggedIn ? name : "");
        PlayerPrefs.SetInt("ZBX_acct_rev", loggedIn ? rev : 0);
        PlayerPrefs.SetInt("ZBX_acct_dirty", loggedIn && dirty ? 1 : 0);
        PlayerPrefs.Save();
    }

    static sc_gamecontrol G() { return UnityEngine.Object.FindFirstObjectByType<sc_gamecontrol>(); }

    public static SaveData Capture(sc_gamecontrol g)
    {
        ZbSettings.Load(); ZbMenu.TimedWaves(); ZbMenu.MoreLoot(); // (lazily loaded values)
        return new SaveData
        {
            v = 1, nick = g.playernick ?? "", nickok = PlayerPrefs.GetInt("ZBX_nick_ok", 0),
            beard = g.my_beard, beardc = g.my_beardcolor, skin = g.my_skincolor, shirt = g.my_shirtcolor, money = g.money,
            autoaim = g.autoaim ? 1 : 0, autoreload = g.autoreload ? 1 : 0, sound = g.audiovol, sense = g.mousesense,
            inv = (int[])g.invent.Clone(), fov = ZbSettings.fov, invy = ZbSettings.invertY ? 1 : 0, quality = ZbSettings.quality,
            cwaves = ZbMenu.timedWaves ? 0 : 1, cloot = ZbMenu.moreLoot ? 0 : 1,
        };
    }
    static SaveData Defaults(sc_gamecontrol g)
    {
        var inv = new int[g.invent.Length]; for (int i = 0; i < inv.Length; i++) inv[i] = starterInv != null && i < starterInv.Length ? starterInv[i] : -1;
        return new SaveData { v = 1, nick = "player", beard = -1, sound = 1f, sense = 1f, inv = inv, fov = ZbSettings.FOV_DEFAULT, quality = ZbSettings.Q_MEDIUM };
    }
    // the original's own save, overwritten with d (and written to PlayerPrefs through save(), which does not upload while applying)
    static void Apply(sc_gamecontrol g, SaveData d)
    {
        if (d == null || d.v == 0) return;
        applying = true;
        try
        {
            g.playernick = loggedIn ? name : (d.nick ?? "player");
            g.my_beard = d.beard; g.my_beardcolor = d.beardc; g.my_skincolor = d.skin; g.my_shirtcolor = d.shirt;
            g.money = Mathf.Max(0, d.money); g.autoaim = d.autoaim == 1; g.autoreload = d.autoreload == 1;
            g.audiovol = Mathf.Clamp01(d.sound); g.mousesense = Mathf.Clamp(d.sense, 0.2f, 3f);
            for (int i = 0; i < g.invent.Length; i++) g.invent[i] = d.inv != null && i < d.inv.Length ? d.inv[i] : -1;
            PlayerPrefs.SetInt("ZBX_nick_ok", loggedIn || d.nickok == 1 ? 1 : 0);
            ZbSettings.Load();
            ZbSettings.fov = Mathf.Clamp(d.fov == 0 ? ZbSettings.FOV_DEFAULT : d.fov, ZbSettings.FOV_MIN, ZbSettings.FOV_MAX);
            ZbSettings.invertY = d.invy == 1;
            int q = Mathf.Clamp(d.quality, ZbSettings.Q_LOW, ZbSettings.Q_HIGH);
            if (q != ZbSettings.quality) { ZbSettings.quality = q; ZbSettings.ApplyQuality(); }
            ZbSettings.Save();
            ZbMenu.TimedWaves(); ZbMenu.MoreLoot(); // make sure the lazily loaded values are loaded before overriding them
            ZbMenu.timedWaves = d.cwaves != 1; ZbMenu.moreLoot = d.cloot != 1;
            PlayerPrefs.SetInt("ZBX_classic_waves", d.cwaves == 1 ? 1 : 0); PlayerPrefs.SetInt("ZBX_classic_loot", d.cloot == 1 ? 1 : 0);
            g.save();
            g.update_manequim();
        }
        finally { applying = false; }
    }
    // the inventory a new player starts with (the scene's sc_gamecontrol before its Start loads the save; captured by ZbProfileRunner)
    public static int[] starterInv;
    static bool HasGuestData(sc_gamecontrol g)
    {
        if (g.money > 0) return true;
        if (starterInv == null) return false;
        for (int i = 0; i < g.invent.Length; i++) if (g.invent[i] != (i < starterInv.Length ? starterInv[i] : -1)) return true;
        return false;
    }

    // ---- hooks ------------------------------------------------------------------------------------------------------------------------
    // end of sc_gamecontrol.save()
    public static void OnSave(sc_gamecontrol g)
    {
        if (applying || !loggedIn) return;
        if (g.playernick != name)
        {   // the profile name is the nickname (the Appearance name field cannot change it while logged in)
            g.playernick = name; PlayerPrefs.SetString("ZB_" + g.saveversion + "_prefname", name);
        }
        dirty = true; saveGen++;
        StoreLocal();
        Stage(g);
        float now = Time.unscaledTime;
        float due = Mathf.Max(now + 1f, lastUpload + UPLOAD_GAP);
        if (dueAt < 0f || due < dueAt) dueAt = due;
    }
    static string Body(sc_gamecontrol g) { return "{\"rev\":" + rev + ",\"data\":" + JsonUtility.ToJson(Capture(g)) + "}"; }
    static void Stage(sc_gamecontrol g) { ZbAcctStage(loggedIn && dirty ? Body(g) : ""); }

    // every frame (ZbProfileRunner.Update)
    public static void Tick()
    {
        var g = G();
        if (g == null || Time.frameCount < 10) return;
        if (!booted) { booted = true; LoadLocal(); ZbNet.Run(Boot(g)); return; }
        if (loggedIn && dirty && !uploading && !busy && dueAt >= 0f && Time.unscaledTime >= dueAt) ZbNet.Run(Upload(g));
    }

    // main menu (state 0): a row below the original's Quit
    public static void MainMenuButton(sc_gamecontrol g, int cx)
    {
        GUI.color = Color.red;
        GUI.DrawTexture(new Rect(cx - g.newhud[0].width / 2, Screen.height / 2 + 150, g.newhud[0].width, g.newhud[0].height), g.newhud[0], ScaleMode.StretchToFill, true, 0f);
        GUI.color = Color.white;
        if (GUI.Button(new Rect(cx - 200, Screen.height / 2 + 155, 400f, 32f), loggedIn ? "Profile: " + name : "Profile (log in / create)")) Open(g);
    }

    public static void Open(sc_gamecontrol g)
    {
        msg = ""; g.gamestate = ST_PROFILE;
        if (loggedIn && !busy) ZbNet.Run(Refresh(g)); // fresh stats
    }

    // ---- network ----------------------------------------------------------------------------------------------------------------------
    static IEnumerator Req(string method, string path, string json, Action<long, string> done)
    {
        UnityWebRequest wr = method == "GET" ? UnityWebRequest.Get(ZbWs.Origin() + path) : UnityWebRequest.Post(ZbWs.Origin() + path, json ?? "{}", "application/json");
        using (wr)
        {
            wr.timeout = 15;
            yield return wr.SendWebRequest();
            long code = wr.result == UnityWebRequest.Result.ConnectionError ? -1 : wr.responseCode;
            done(code, wr.downloadHandler != null ? wr.downloadHandler.text : "");
        }
    }
    static Prof Parse(string body) { try { return JsonUtility.FromJson<Prof>(string.IsNullOrEmpty(body) ? "{}" : body); } catch { return new Prof(); } }
    static string Esc(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\""); }

    // page load: is the cookie still a session? (also picks up a session this browser has while its PlayerPrefs were cleared)
    static IEnumerator Boot(sc_gamecontrol g)
    {
        long code = 0; string body = "";
        yield return Req("GET", "/api/account/me", null, (c, b) => { code = c; body = b; });
        if (code == 200)
        {
            var p = Parse(body);
            bool wasIn = loggedIn && name.ToLowerInvariant() == (p.name ?? "").ToLowerInvariant();
            if (!wasIn) { if (!loggedIn) PlayerPrefs.SetString("ZBX_guest_save", JsonUtility.ToJson(Capture(g))); dirty = false; }
            loggedIn = true; name = p.name; stats = p.stats ?? new Stats();
            if (dirty && rev == p.rev) { Debug.Log("[zbprofile] boot: uploading the local save (rev " + rev + ")"); StoreLocal(); dueAt = 0f; }
            else { Debug.Log("[zbprofile] boot: server save rev " + p.rev + " (local " + rev + (dirty ? ", stale local changes dropped" : "") + ")"); rev = p.rev; dirty = false; StoreLocal(); Apply(g, p.data); }
        }
        else if (code == 401 && loggedIn)
        {
            Debug.Log("[zbprofile] boot: the session ended");
            LocalLogout(g); msg = "Your session ended. Log in again."; msgColor = Color.yellow;
        }
        else if (loggedIn) { Debug.Log("[zbprofile] boot: offline (" + code + "), keeping the local save"); if (dirty) dueAt = Time.unscaledTime + 30f; }
        Stage(g);
    }

    static IEnumerator Upload(sc_gamecontrol g)
    {
        uploading = true; dueAt = -1f; lastUpload = Time.unscaledTime;
        int gen = saveGen; long code = 0; string body = "";
        yield return Req("POST", "/api/account/save", Body(g), (c, b) => { code = c; body = b; });
        uploading = false;
        if (!loggedIn) yield break;
        var p = Parse(body);
        if (code == 200 && p.ok)
        {
            rev = p.rev;
            if (gen == saveGen) dirty = false; else dueAt = Mathf.Max(Time.unscaledTime + 1f, lastUpload + UPLOAD_GAP);
            Debug.Log("[zbprofile] uploaded, rev " + rev);
        }
        else if (code == 409)
        {   // another tab / browser wrote first: its newer save wins
            Debug.Log("[zbprofile] stale save refused, loading the server copy (rev " + p.rev + ")");
            rev = p.rev; dirty = false; StoreLocal(); Apply(g, p.data);
            msg = "This profile was saved from another tab or browser; its newer save was loaded."; msgColor = Color.yellow;
        }
        else if (code == 401) { Debug.Log("[zbprofile] upload: session ended"); LocalLogout(g); msg = "Your session ended. Log in again."; msgColor = Color.yellow; }
        else { Debug.Log("[zbprofile] upload failed (" + code + "), retrying"); dueAt = Time.unscaledTime + 30f; }
        StoreLocal(); Stage(g);
    }

    static IEnumerator Refresh(sc_gamecontrol g)
    {
        long code = 0; string body = "";
        yield return Req("GET", "/api/account/me", null, (c, b) => { code = c; body = b; });
        if (code == 200) { var p = Parse(body); stats = p.stats ?? stats; }
        else if (code == 401 && loggedIn) { LocalLogout(g); msg = "Your session ended. Log in again."; msgColor = Color.yellow; }
    }

    static void Busy(string t) { busy = true; msg = t; msgColor = Color.white; }
    static void Err(string t) { busy = false; msg = t; msgColor = Color.red; }
    static string ErrText(long code, Prof p, string fallback)
    {
        if (code == -1 || code == 0) return "Could not reach the server, try again.";
        if (code == 429) return "Too many tries, wait a few minutes.";
        if (p != null && p.error == "taken") return "This name is taken.";
        if (p != null && p.error == "name") return "Name: 2 to 10 letters, digits, space, _ - .   (not \"player\")";
        if (p != null && p.error == "password") return "Password: 6 to 72 characters.";
        return fallback;
    }

    static IEnumerator SignupRoutine(sc_gamecontrol g, string n, string pw)
    {
        Busy("Creating the profile...");
        var d = Capture(g); d.nick = n; d.nickok = 1;  // the first signup takes this browser's save along (guest -> profile)
        long code = 0; string body = "";
        yield return Req("POST", "/api/account/signup", "{\"name\":\"" + Esc(n) + "\",\"password\":\"" + Esc(pw) + "\",\"data\":" + JsonUtility.ToJson(d) + "}", (c, b) => { code = c; body = b; });
        var p = Parse(body);
        if (code != 200 || !p.ok) { Err(ErrText(code, p, "Could not create the profile (" + code + ").")); yield break; }
        busy = false;
        PlayerPrefs.DeleteKey("ZBX_guest_save"); // moved into the profile: logging out later starts a fresh guest
        loggedIn = true; name = p.name; rev = p.rev; dirty = false; stats = p.stats ?? new Stats(); StoreLocal();
        Apply(g, p.data);
        recovery = p.recovery ?? ""; fPw = fPw2 = "";
        msg = ""; g.gamestate = ST_CODE;
        Debug.Log("[zbprofile] signed up as " + name);
        Stage(g);
    }

    // login and recovery both end here: the profile's save replaces the local one, the guest save is kept aside
    static void LoggedIn(sc_gamecontrol g, Prof p)
    {
        busy = false;
        if (!loggedIn) PlayerPrefs.SetString("ZBX_guest_save", JsonUtility.ToJson(Capture(g)));
        loggedIn = true; name = p.name; rev = p.rev; dirty = false; stats = p.stats ?? new Stats(); StoreLocal();
        Apply(g, p.data);
        fPw = fPw2 = fCode = "";
        Stage(g);
        Debug.Log("[zbprofile] logged in as " + name + " (rev " + rev + ")");
    }

    static IEnumerator LoginRoutine(sc_gamecontrol g, string n, string pw)
    {
        Busy("Logging in...");
        long code = 0; string body = "";
        yield return Req("POST", "/api/account/login", "{\"name\":\"" + Esc(n) + "\",\"password\":\"" + Esc(pw) + "\"}", (c, b) => { code = c; body = b; });
        var p = Parse(body);
        if (code != 200 || !p.ok) { Err(code == 401 ? "Wrong name or password." : ErrText(code, p, "Could not log in (" + code + ").")); g.gamestate = ST_LOGIN; yield break; }
        LoggedIn(g, p);
        msg = "Logged in. Your profile's save is loaded."; msgColor = Color.green; g.gamestate = ST_PROFILE;
    }

    static IEnumerator RecoverRoutine(sc_gamecontrol g, string n, string rc, string pw)
    {
        Busy("Resetting the password...");
        long code = 0; string body = "";
        yield return Req("POST", "/api/account/recover", "{\"name\":\"" + Esc(n) + "\",\"code\":\"" + Esc(rc) + "\",\"password\":\"" + Esc(pw) + "\"}", (c, b) => { code = c; body = b; });
        var p = Parse(body);
        if (code != 200 || !p.ok) { Err(code == 401 ? "Wrong name or recovery code." : ErrText(code, p, "Could not reset the password (" + code + ").")); g.gamestate = ST_RECOVER; yield break; }
        LoggedIn(g, p);
        recovery = p.recovery ?? ""; msg = ""; g.gamestate = ST_CODE;
    }

    // back to this browser's guest save (or a fresh one)
    static void LocalLogout(sc_gamecontrol g)
    {
        loggedIn = false; dirty = false; rev = 0; name = ""; stats = new Stats(); dueAt = -1f;
        StoreLocal(); ZbAcctStage("");
        SaveData guest = null;
        var s = PlayerPrefs.GetString("ZBX_guest_save", "");
        if (s.Length > 0) { try { guest = JsonUtility.FromJson<SaveData>(s); } catch { guest = null; } }
        Apply(g, guest != null && guest.v == 1 ? guest : Defaults(g));
        PlayerPrefs.DeleteKey("ZBX_guest_save"); PlayerPrefs.Save();
    }

    static IEnumerator LogoutRoutine(sc_gamecontrol g)
    {
        Busy("Logging out...");
        if (dirty) { long uc = 0; yield return Req("POST", "/api/account/save", Body(g), (c, b) => { uc = c; }); } // the last changes first
        long code = 0;
        yield return Req("POST", "/api/account/logout", "{}", (c, b) => { code = c; });
        busy = false;
        LocalLogout(g);
        msg = "Logged out. Playing as a guest."; msgColor = Color.white; g.gamestate = ST_PROFILE;
    }

    static IEnumerator DeleteRoutine(sc_gamecontrol g, string pw)
    {
        Busy("Deleting the profile...");
        long code = 0; string body = "";
        yield return Req("POST", "/api/account/delete", "{\"password\":\"" + Esc(pw) + "\"}", (c, b) => { code = c; body = b; });
        if (code != 200) { Err(code == 401 ? "Wrong password." : ErrText(code, Parse(body), "Could not delete the profile (" + code + ").")); yield break; }
        busy = false; fPw = "";
        LocalLogout(g);
        msg = "The profile was deleted."; msgColor = Color.white; g.gamestate = ST_PROFILE;
    }

    // ---- screens ----------------------------------------------------------------------------------------------------------------------
    static void Slot(sc_gamecontrol g, int cx, int dy)
    {
        GUI.color = Color.red;
        GUI.DrawTexture(new Rect(cx - g.newhud[0].width / 2, Screen.height / 2 + dy, g.newhud[0].width, g.newhud[0].height), g.newhud[0], ScaleMode.StretchToFill, true, 0f);
        GUI.color = Color.white;
    }
    static bool Btn(sc_gamecontrol g, int cx, int dy, string text) { Slot(g, cx, dy); return GUI.Button(new Rect(cx - 200, Screen.height / 2 + dy + 5, 400f, 32f), text) && !busy; }
    static void Label(int cx, int dy, string text, Color c, int h = 40, int size = 0)
    {
        var old = GUI.color; GUI.color = c;
        var st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
        if (size > 0) st.fontSize = size;
        GUI.Label(new Rect(cx - 300, Screen.height / 2 + dy, 600f, h), text, st);
        GUI.color = old;
    }
    // a labelled input row in the original's frame: label on the left half, the field on the right half
    static string Field(sc_gamecontrol g, int cx, int dy, string label, string value, bool password, int max)
    {
        Slot(g, cx, dy);
        GUI.Label(new Rect(cx - 190, Screen.height / 2 + dy + 5, 140f, 32f), label);
        var r = new Rect(cx - 45, Screen.height / 2 + dy + 6, 235f, 30f);
        return password ? GUI.PasswordField(r, value ?? "", '*', max) : GUI.TextField(r, value ?? "", max);
    }
    static bool Enter() { return Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter); }

    // after the original states (sc_gamecontrol.OnGUI, next to ZbMenu.Draw)
    public static void Draw(sc_gamecontrol g, int cx)
    {
        int s = g.gamestate;
        if (s < ST_PROFILE || s > ST_DELETE) return;
        g.sharedhud = 1; GUI.skin = g.newgui_square; GUI.color = Color.white;
        switch (s)
        {
            case ST_PROFILE: DrawProfile(g, cx); break;
            case ST_LOGIN: DrawLogin(g, cx); break;
            case ST_SIGNUP: DrawSignup(g, cx); break;
            case ST_RECOVER: DrawRecover(g, cx); break;
            case ST_CODE: DrawCode(g, cx); break;
            case ST_KEEP: DrawKeep(g, cx); break;
            case ST_DELETE: DrawDelete(g, cx); break;
        }
        if (msg.Length > 0 && s != ST_CODE) Label(cx, 200, msg, msgColor, 50);
    }

    static void DrawProfile(sc_gamecontrol g, int cx)
    {
        if (loggedIn)
        {
            Label(cx, -230, "Logged in as " + name, Color.yellow, 40, 22);
            Label(cx, -190, "Best wave " + stats.bestWave + "     Total kills " + stats.kills + "     Games played " + stats.games, Color.white);
            Label(cx, -160, "Your money, items, look and settings are saved online: log in on any browser to get them back.", Color.gray, 40, 13);
            if (Btn(g, cx, -100, "Log out")) ZbNet.Run(LogoutRoutine(g));
            if (Btn(g, cx, -50, "Delete profile")) { fPw = ""; msg = ""; g.gamestate = ST_DELETE; }
            if (Btn(g, cx, 0, "Go back")) { msg = ""; g.gamestate = 0; }
        }
        else
        {
            Label(cx, -230, "Playing as a guest", Color.yellow, 40, 22);
            Label(cx, -190, "Your progress is saved only in this browser. A profile keeps it online: log in on any browser or computer to get it back.", Color.white, 46);
            if (Btn(g, cx, -130, "Log in")) { Clear(); g.gamestate = ST_LOGIN; }
            if (Btn(g, cx, -80, "Create profile")) { Clear(); g.gamestate = ST_SIGNUP; }
            if (Btn(g, cx, -30, "Forgot password")) { Clear(); g.gamestate = ST_RECOVER; }
            if (Btn(g, cx, 20, "Go back")) { msg = ""; g.gamestate = 0; }
        }
        Label(cx, 70, "Privacy: a profile stores its name, a salted hash of the password (never the password) and your game save (money, items, look, settings) plus best wave / kills / games played. Delete it any time here (Delete profile).", Color.gray, 70, 12);
    }
    static void Clear() { fPw = fPw2 = fCode = ""; msg = ""; if (fName.Length == 0) fName = loggedIn ? name : ""; }

    static void DrawLogin(sc_gamecontrol g, int cx)
    {
        Label(cx, -200, "Log in to your profile", Color.yellow, 40, 20);
        fName = ZbMenu.FilterNick(Field(g, cx, -150, "Profile name", fName, false, 10));
        fPw = Field(g, cx, -100, "Password", fPw, true, 72);
        bool go = Btn(g, cx, -30, "Log in") || (Enter() && !busy);
        if (Btn(g, cx, 20, "Forgot password")) { Clear(); g.gamestate = ST_RECOVER; }
        if (Btn(g, cx, 70, "Go back")) { msg = ""; g.gamestate = ST_PROFILE; }
        if (go) Submit(g, ST_LOGIN);
    }

    static void DrawSignup(sc_gamecontrol g, int cx)
    {
        Label(cx, -230, "Create a profile", Color.yellow, 40, 20);
        Label(cx, -200, "The profile name is your nickname (unique). This browser's money, items and look move into the profile.", Color.gray, 34, 13);
        fName = ZbMenu.FilterNick(Field(g, cx, -150, "Profile name", fName, false, 10));
        fPw = Field(g, cx, -100, "Password", fPw, true, 72);
        fPw2 = Field(g, cx, -50, "Repeat", fPw2, true, 72);
        bool go = Btn(g, cx, 20, "Create profile") || (Enter() && !busy);
        if (Btn(g, cx, 70, "Go back")) { msg = ""; g.gamestate = ST_PROFILE; }
        if (!go) return;
        string n = ZbMenu.CleanNick(fName);
        if (n == null) Err("Name: 2 to 10 letters, digits, space, _ - .   (not \"player\")");
        else if (fPw.Length < 6) Err("Password: at least 6 characters.");
        else if (fPw != fPw2) Err("The passwords do not match.");
        else ZbNet.Run(SignupRoutine(g, n, fPw));
    }

    static void DrawRecover(sc_gamecontrol g, int cx)
    {
        Label(cx, -230, "Forgot password", Color.yellow, 40, 20);
        Label(cx, -200, "Use the recovery code you got when you created the profile. You get a new one.", Color.gray, 34, 13);
        fName = ZbMenu.FilterNick(Field(g, cx, -150, "Profile name", fName, false, 10));
        fCode = Field(g, cx, -100, "Recovery code", fCode, false, 24);
        fPw = Field(g, cx, -50, "New password", fPw, true, 72);
        bool go = Btn(g, cx, 20, "Reset password") || (Enter() && !busy);
        if (Btn(g, cx, 70, "Go back")) { msg = ""; g.gamestate = ST_PROFILE; }
        if (go) Submit(g, ST_RECOVER);
    }

    // login / recovery: a browser that holds a guest save asks first (the profile's save is loaded, never merged)
    static void Submit(sc_gamecontrol g, int what)
    {
        string n = ZbMenu.CleanNick(fName);
        if (n == null) { Err("Enter your profile name."); return; }
        if (what == ST_RECOVER && fPw.Length < 6) { Err("New password: at least 6 characters."); return; }
        if (what == ST_LOGIN && fPw.Length == 0) { Err("Enter your password."); return; }
        if (!loggedIn && HasGuestData(g) && g.gamestate != ST_KEEP) { pending = what; msg = ""; g.gamestate = ST_KEEP; return; }
        Go(g, what);
    }
    static void Go(sc_gamecontrol g, int what)
    {
        string n = ZbMenu.CleanNick(fName);
        if (what == ST_LOGIN) ZbNet.Run(LoginRoutine(g, n, fPw)); else ZbNet.Run(RecoverRoutine(g, n, fCode, fPw));
    }

    static void DrawKeep(sc_gamecontrol g, int cx)
    {
        Label(cx, -220, "This browser has a guest save ($" + g.money + ")", Color.yellow, 40, 20);
        Label(cx, -185, "Logging in loads your profile's save instead (nothing is merged). The guest save stays in this browser and comes back when you log out.", Color.white, 70);
        if (Btn(g, cx, -100, "Keep profile save")) Go(g, pending);
        if (Btn(g, cx, -50, "Cancel")) { msg = ""; g.gamestate = pending; }
        if (Enter() && !busy) Go(g, pending);
    }

    static void DrawCode(sc_gamecontrol g, int cx)
    {
        Label(cx, -230, "Your recovery code", Color.yellow, 40, 22);
        Label(cx, -195, "Write it down. It is the only way to reset a forgotten password (there is no e-mail) and it is shown only this once.", Color.white, 46);
        Slot(g, cx, -130);
        var big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 24, fontStyle = FontStyle.Bold };
        GUI.color = Color.green; GUI.Label(new Rect(cx - 200, Screen.height / 2 - 128, 400f, 38f), recovery, big); GUI.color = Color.white;
        if (Btn(g, cx, -70, "Copy to clipboard")) { ZbAcctCopy(recovery); msg = "Copied."; msgColor = Color.green; }
        if (Btn(g, cx, -20, "I wrote it down")) { recovery = ""; msg = "Logged in as " + name + "."; msgColor = Color.green; g.gamestate = ST_PROFILE; }
        if (msg.Length > 0) Label(cx, 40, msg, msgColor);
    }

    static void DrawDelete(sc_gamecontrol g, int cx)
    {
        Label(cx, -230, "Delete the profile " + name + "?", Color.yellow, 40, 20);
        Label(cx, -195, "This removes the profile, its online save and its stats for good. This browser goes back to its guest save. Leaderboard results stay under the nickname.", Color.white, 46);
        fPw = Field(g, cx, -130, "Password", fPw, true, 72);
        if (Btn(g, cx, -60, "Delete profile")) { if (fPw.Length == 0) Err("Enter your password."); else ZbNet.Run(DeleteRoutine(g, fPw)); }
        if (Btn(g, cx, -10, "Go back")) { msg = ""; fPw = ""; g.gamestate = ST_PROFILE; }
    }

    // ---- test hooks (ZbProfileRunner) ---------------------------------------------------------------------------------------------------
    public static string DebugJson()
    {
        var g = G();
        return "{\"loggedIn\":" + (loggedIn ? 1 : 0) + ",\"name\":\"" + Esc(name) + "\",\"rev\":" + rev + ",\"dirty\":" + (dirty ? 1 : 0) + ",\"busy\":" + (busy ? 1 : 0)
            + ",\"uploading\":" + (uploading ? 1 : 0) + ",\"msg\":\"" + Esc(msg) + "\",\"recovery\":\"" + Esc(recovery) + "\",\"gamestate\":" + (g != null ? g.gamestate : -99)
            + ",\"stats\":{\"bestWave\":" + stats.bestWave + ",\"kills\":" + stats.kills + ",\"games\":" + stats.games + "}"
            + ",\"guestSave\":" + (PlayerPrefs.GetString("ZBX_guest_save", "").Length > 0 ? 1 : 0) + "}";
    }
    // "name\tpassword\trepeat\tcode": fill the form fields (typing into IMGUI fields from Playwright is unreliable)
    public static void DebugFields(string a)
    {
        var f = (a ?? "").Split('\t');
        fName = f.Length > 0 ? f[0] : ""; fPw = f.Length > 1 ? f[1] : ""; fPw2 = f.Length > 2 ? f[2] : fPw; fCode = f.Length > 3 ? f[3] : "";
    }
    public static void DebugFlush() { if (loggedIn && dirty) dueAt = 0f; lastUpload = -999f; }
}

// own runner object (unityInstance.SendMessage("ZbProfile", ...)): keeps the profile code out of ZbNet's runner
public class ZbProfileRunner : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        var go = new GameObject("ZbProfile");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        go.AddComponent<ZbProfileRunner>();
    }
    void OnEnable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnLoaded; }
    void OnDisable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnLoaded; }
    // first scene: sc_gamecontrol has run Awake but not Start (which loads the save), so its inventory is still the new-player default
    void OnLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m)
    {
        if (ZbProfile.starterInv != null) return;
        var g = FindFirstObjectByType<sc_gamecontrol>();
        if (g != null && g.invent != null) ZbProfile.starterInv = (int[])g.invent.Clone();
    }
    void Update() { ZbProfile.Tick(); }
    public void DebugProfile(string unused) { Debug.Log("ZBPROF " + ZbProfile.DebugJson()); }
    public void DebugFields(string a) { ZbProfile.DebugFields(a); }
    public void DebugFlush(string unused) { ZbProfile.DebugFlush(); }
}
