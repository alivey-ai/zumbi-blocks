// Phase 3 (docs/design/PHASE3-4-MATCHMAKING-RESILIENCE.md): getting into a game. Everything here sits AROUND the original game:
// the original menu code (sc_gamecontrol.OnGUI) only gets a few one-line hooks into this file.
//   state 30  nickname prompt (first multiplayer use)        state 31  Quick Play "finding a game"
//   state 32  invite link (?room=CODE)                       state 33  all settings (FOV, quality, invert Y, sensitivity, volume)
//   state 34  host left, the group continues in a new match (phase 4, level B)
//   state 35  leaderboard (phase 5, ZbBoard.cs)                state 36  start single player (after the nickname prompt)
// Gameplay is not touched: Quick Play rooms may auto start (a countdown instead of ENTER), settings only change camera / quality / input sign.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// ---------------------------------------------------------------------------------------------------------------
// Settings that the original does not have (sensitivity, volume, autoaim, autoreload already live in sc_gamecontrol.save()).
// Own PlayerPrefs keys, independent of the game's save version, flushed with PlayerPrefs.Save() on every change (WebGL only writes
// IndexedDB on Save).
// ---------------------------------------------------------------------------------------------------------------
public static class ZbSettings
{
    public const int FOV_DEFAULT = 70, FOV_MIN = 60, FOV_MAX = 110;
    public const int Q_LOW = 0, Q_MEDIUM = 1, Q_HIGH = 2;
    // existing QualitySettings levels of the project: 1 "Fast", 3 "Good" (what the WebGL build uses today), 4 "Beautiful"
    static readonly int[] levels = { 1, 3, 4 };
    public static readonly string[] qualityNames = { "Low", "Medium", "High" };
    public static int fov = FOV_DEFAULT, quality = Q_MEDIUM;
    public static bool invertY;
    static bool loaded;

    public static void Load()
    {
        if (loaded) return;
        loaded = true;
        fov = Mathf.Clamp(PlayerPrefs.GetInt("ZBX_fov", FOV_DEFAULT), FOV_MIN, FOV_MAX);
        invertY = PlayerPrefs.GetInt("ZBX_invy", 0) == 1;
        quality = Mathf.Clamp(PlayerPrefs.GetInt("ZBX_quality", Q_MEDIUM), Q_LOW, Q_HIGH);
        if (quality != Q_MEDIUM) ApplyQuality(); // an untouched profile never calls QualitySettings: identical to v0.7
    }

    public static void Save()
    {
        PlayerPrefs.SetInt("ZBX_fov", fov);
        PlayerPrefs.SetInt("ZBX_invy", invertY ? 1 : 0);
        PlayerPrefs.SetInt("ZBX_quality", quality);
        PlayerPrefs.Save();
    }

    public static void ApplyQuality() { QualitySettings.SetQualityLevel(levels[Mathf.Clamp(quality, 0, 2)], true); }

    // the one place the aiming code reads the mouse's vertical axis (sc_player: headang += ZbSettings.MouseY() * mouse_sense)
    public static float MouseY() { float y = Input.GetAxis("Mouse Y"); return invertY ? -y : y; }

    // 2 Hz: push the field of view into the local player (the original copies def_fov = 70 in its Start and never changes it)
    static float nextTick;
    public static void Tick()
    {
        if (Time.unscaledTime < nextTick) return;
        nextTick = Time.unscaledTime + 0.5f;
        foreach (var p in UnityEngine.Object.FindObjectsByType<sc_player>(FindObjectsSortMode.None))
        {
            var nv = p.GetComponent<NetworkView>();
            if (nv == null || !nv.isMine) continue;
            if (p.def_fov != fov)
            {
                var cc = p.cam != null ? p.cam.GetComponent<sc_camcript>() : null;
                bool atDefault = cc != null && cc.to_fov == p.def_fov;
                p.def_fov = fov;
                if (cc != null && atDefault && !p.zooming) cc.to_fov = fov; // zoom keeps working: it uses def_fov - gun.add_fov
            }
        }
    }
}

public static class ZbMenu
{
    public const int ST_NICK = 30, ST_QUICK = 31, ST_INVITE = 32, ST_SETTINGS = 33, ST_HOSTLEFT = 34, ST_SOLO = 36;
    const string QUICK_COMMENT = "Quick Play";

    // ---- small helpers -----------------------------------------------------------------------------------------
    static string Type(sc_gamecontrol g) { return "Zumbiblocks " + g.buildversion; }
    static string Origin() { return ZbWs.Origin(); }

    // The original menu look: red outlined frame textures (newhud[0]) with white button text, centred in the right area.
    static void Slot(sc_gamecontrol g, int cx, int dy)
    {
        GUI.color = Color.red;
        GUI.DrawTexture(new Rect(cx - g.newhud[0].width / 2, Screen.height / 2 + dy, g.newhud[0].width, g.newhud[0].height), g.newhud[0], ScaleMode.StretchToFill, true, 0f);
        GUI.color = Color.white;
    }
    static bool Btn(sc_gamecontrol g, int cx, int dy, string text)
    {
        Slot(g, cx, dy);
        return GUI.Button(new Rect(cx - 200, Screen.height / 2 + dy + 5, 400f, 32f), text);
    }
    static void Msg(sc_gamecontrol g, int cx, int dy, string text, Color c)
    {
        var old = GUI.color; GUI.color = c;
        var st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
        GUI.Label(new Rect(cx - 300, Screen.height / 2 + dy, 600f, 60f), text, st);
        GUI.color = old;
    }

    // ---- nickname ----------------------------------------------------------------------------------------------
    static bool NickOk() { return PlayerPrefs.GetInt("ZBX_nick_ok", 0) == 1; }
    public static bool NeedNick(sc_gamecontrol g)
    {
        string n = g.playernick ?? "";
        return (n.Length == 0 || n.Trim().ToLowerInvariant() == "player") && !NickOk();
    }
    // letters, digits, space, _ - . ; strips control characters and bidi overrides. Live filter for text fields (no trimming while typing).
    public static string FilterNick(string s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? "";
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (char c in s) if (char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-' || c == '.') sb.Append(c);
        return sb.Length == s.Length ? s : sb.ToString();
    }
    // final form: trimmed, spaces collapsed, 2..10 chars, not "player"; null = not acceptable
    public static string CleanNick(string s)
    {
        s = FilterNick(s ?? "").Trim();
        while (s.Contains("  ")) s = s.Replace("  ", " ");
        if (s.Length > 10) s = s.Substring(0, 10).Trim();
        if (s.Length < 2 || s.ToLowerInvariant() == "player") return null;
        return s;
    }

    static int nickReturn = 0;
    static string nickText = "";
    static string nickErr = "";

    // ---- hooks called from sc_gamecontrol ----------------------------------------------------------------------
    // first OnGUI frame (gamestate == -1)
    public static int EntryState(sc_gamecontrol g)
    {
        ZbSettings.Load();
        var room = QueryParam("room");
        if (room != null)
        {
            room = room.Trim().ToUpperInvariant();
            if (System.Text.RegularExpressions.Regex.IsMatch(room, "^[A-Z0-9]{3,8}$")) { inviteCode = room; inviteStage = 0; return ST_INVITE; }
        }
        return 0;
    }

    // every OnGUI frame, before any state is drawn: the multiplayer entry screens need a nickname first
    static bool entryDone;
    public static void Gate(sc_gamecontrol g)
    {
        // the menu scene starts in gamestate 0 (not -1 as the decompiled -1 check suggests): decide the first screen once
        if (!entryDone) { entryDone = true; if (g.gamestate <= 0) g.gamestate = EntryState(g); }
        int s = g.gamestate;
        if ((s == 25 || s == 26 || s == 27 || s == ST_QUICK || s == ST_INVITE) && NeedNick(g))
        {
            nickReturn = s; nickText = ""; nickErr = ""; g.gamestate = ST_NICK; quickStage = 0; quickToken++; if (inviteStage == 1) inviteStage = 0;
        }
    }

    // after every save() the nickname is stored in its final shape
    public static void BeforeSave(sc_gamecontrol g) { g.playernick = FilterNick(g.playernick); }

    // drawn from OnGUI after the original states; returns true when it handled the current state
    public static void Draw(sc_gamecontrol g, int cx)
    {
        Gate(g); // a click earlier in this same frame may just have entered a state that needs the nickname first
        switch (g.gamestate)
        {
            case ST_NICK: DrawNick(g, cx); break;
            case ST_QUICK: DrawQuick(g, cx); break;
            case ST_INVITE: DrawInvite(g, cx); break;
            case ST_SETTINGS: DrawSettings(g, cx); break;
            case ST_HOSTLEFT: DrawHostLeft(g, cx); break;
            case ZbBoard.ST_BOARD: ZbBoard.Draw(g, cx); break;
            case ST_SOLO: StartSolo(g); break;
        }
    }

    static void Frame(sc_gamecontrol g) { g.sharedhud = 1; GUI.skin = g.newgui_square; GUI.color = Color.white; }

    static void DrawNick(sc_gamecontrol g, int cx)
    {
        Frame(g);
        Msg(g, cx, -150, "Choose a nickname (shown to other players and on the leaderboard)", Color.yellow);
        Slot(g, cx, -100);
        nickText = FilterNick(GUI.TextField(new Rect(cx - 190, Screen.height / 2 - 95, 380f, 30f), nickText, 10));
        if (nickErr.Length > 0) Msg(g, cx, -50, nickErr, Color.red);
        bool enter = Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
        if (Btn(g, cx, 0, "Continue") || enter)
        {
            string n = CleanNick(nickText);
            if (n == null) nickErr = "2 to 10 letters, digits, space, _ - .   (not \"player\")";
            else
            {
                g.playernick = n; PlayerPrefs.SetInt("ZBX_nick_ok", 1); g.save();
                g.gamestate = nickReturn;
            }
        }
        if (Btn(g, cx, 50, "Go back")) g.gamestate = 0;
    }

    // ---- single player (phase 5): "Play alone" asks for the nickname first too (it is shown on the leaderboard) ----------------
    // sc_gamecontrol state 24: true = the prompt was opened instead of starting the match
    public static bool SoloNick(sc_gamecontrol g)
    {
        if (!NeedNick(g)) return false;
        nickReturn = ST_SOLO; nickText = ""; nickErr = ""; g.gamestate = ST_NICK;
        return true;
    }
    // what the original "Play alone" button does
    static void StartSolo(sc_gamecontrol g)
    {
        g.iamserver = true;
        Network.InitializeSecurity();
        Network.InitializeServer(1, 50123, false);
        g.gamestate = 3;
        SceneManager.LoadScene(1);
    }

    // ---- main menu row -----------------------------------------------------------------------------------------
    public static void MainMenuQuick(sc_gamecontrol g, int cx)
    {
        if (Btn(g, cx, -150, "Quick Play")) StartQuick(g);
        GUI.color = Color.white;
    }

    // ---- Quick Play --------------------------------------------------------------------------------------------
    static int quickToken;
    static int quickAttempts;
    static bool quickJoining;       // we are connecting to a room that Quick Play picked (so a failure may retry)
    static bool quickHosting;       // we host a room that Quick Play created: it starts by itself after a countdown
    static string quickStatus = "";
    public static bool needClick;   // joined/created without the player's own click: show "Click to play" once

    public static void StartQuick(sc_gamecontrol g)
    {
        quickAttempts = 0; g.gamestate = ST_QUICK; quickStatus = "Looking for a game..."; quickStage = 0; quickToken++;
    }
    static int quickStage;          // 0 = not started (state 31 just entered; the nickname gate may still send us to state 30 first)

    [Serializable] class QRoom { public string code, name, type; public int players, max; public bool pw; }
    [Serializable] class QList { public QRoom[] rooms; }
    [Serializable] class RestartInfo { public int successor; public bool claimOpen; public int wave; }
    [Serializable] class RoomInfo { public bool exists, pw, started, ended, full; public string name, type, hostState; public int players, max, wave, epoch; public RestartInfo restart; }

    static IEnumerator Get(string path, Action<long, string> done)
    {
        using (var wr = UnityWebRequest.Get(Origin() + path))
        {
            wr.timeout = 8;
            yield return wr.SendWebRequest();
            done(wr.result == UnityWebRequest.Result.Success || wr.responseCode == 404 ? wr.responseCode : -1, wr.downloadHandler != null ? wr.downloadHandler.text : "");
        }
    }

    static IEnumerator QuickRoutine(sc_gamecontrol g, int token)
    {
        string type = Type(g), pick = null;
        long code = 0; string body = "";
        Debug.Log("[zbmenu] quick play: searching (token " + token + ")");
        yield return Get("/api/quickplay?type=" + Uri.EscapeDataString(type), (c, b) => { code = c; body = b; });
        Debug.Log("[zbmenu] quickplay answer " + code + " token " + token + "/" + quickToken + " state " + g.gamestate);
        if (token != quickToken || g.gamestate != ST_QUICK) yield break;
        var cands = new List<QRoom>();
        if (code == 200) { var l = JsonUtility.FromJson<QList>("{\"rooms\":" + body + "}"); if (l != null && l.rooms != null) cands.AddRange(l.rooms); }
        foreach (var cand in cands)
        {
            // re-check right before connecting: closes the race between the list and the join
            long ic = 0; string ib = "";
            yield return Get("/api/room/" + Uri.EscapeDataString(cand.code), (cc, b) => { ic = cc; ib = b; });
            if (token != quickToken || g.gamestate != ST_QUICK) yield break;
            if (ic != 200) continue;
            var info = JsonUtility.FromJson<RoomInfo>(ib);
            if (info == null || !info.exists || info.full || info.pw || info.ended || info.type != type) continue;
            pick = cand.code; break;
        }
        if (token != quickToken || g.gamestate != ST_QUICK) yield break;
        if (pick != null) { quickStatus = "Joining " + pick + "..."; JoinRoom(g, pick, true); }
        else { quickStatus = "No open game, creating one..."; CreateQuick(g); }
    }

    static void JoinRoom(sc_gamecontrol g, string code, bool viaQuick, bool keepPassword = false)
    {
        g.ipstoconec = new[] { code }; g.trynow = 0; g.ipstotry = 1;
        if (g.porttoconec == 0) g.porttoconec = 25678;
        g.porttostring = "" + g.porttoconec;
        if (!keepPassword) g.the_password = "";
        g.iamserver = false; quickJoining = viaQuick; needClick = true;
        g.gamestate = 5;
        SceneManager.LoadScene(1);
    }

    static void CreateQuick(sc_gamecontrol g)
    {
        string nick = CleanNick(g.playernick) ?? (g.playernick ?? "player");
        g.iamserver = true; quickJoining = false; quickHosting = true; needClick = true; countdownEnd = -1f; hostCd = -1f; hostShrunk = false;
        Network.InitializeSecurity();
        Network.incomingPassword = "";
        Network.InitializeServer(16, g.porttoconec == 0 ? 25678 : g.porttoconec, false);
        MasterServer.RegisterHost(Type(g), (nick + "'s game"), QUICK_COMMENT);
        g.gamestate = 3;
        SceneManager.LoadScene(1);
    }

    // OnFailedToConnect, first line: a Quick Play join that failed (the room filled up or closed in between) searches again, then creates.
    public static bool QuickFail(sc_gamecontrol g, NetworkConnectionError error)
    {
        if (claiming && g.gamestate == 3)
        {
            // somebody else claimed the room first (or it closed): go back to the "host left" screen and join the new match instead
            claiming = false; claimLost = true;
            g.cam.parent = null; g.cam.localPosition = Vector3.zero; g.cam.localEulerAngles = Vector3.zero;
            SceneManager.LoadScene(2);
            g.gamestate = ST_HOSTLEFT; leftStatus = "Another player started the new match, joining...";
            ZbNet.Run(RestartRoutine(g, ++restartToken));
            return true;
        }
        if (!quickJoining || g.gamestate != 5) return false;
        quickJoining = false;
        g.cam.parent = null; g.cam.localPosition = Vector3.zero; g.cam.localEulerAngles = Vector3.zero;
        SceneManager.LoadScene(2);
        if (++quickAttempts <= 2) { g.gamestate = ST_QUICK; quickStatus = "That game is gone, looking again..."; quickStage = 1; ZbNet.Run(QuickRoutine(g, ++quickToken)); }
        else { g.gamestate = ST_QUICK; CreateQuick(g); }
        return true;
    }

    static void DrawQuick(sc_gamecontrol g, int cx)
    {
        Frame(g);
        if (quickStage == 0) { quickStage = 1; ZbNet.Run(QuickRoutine(g, quickToken)); }
        Msg(g, cx, -100, quickStatus, Color.yellow);
        if (Btn(g, cx, 0, "Cancel")) { quickToken++; quickJoining = false; g.gamestate = 0; }
    }

    // ---- invite links ------------------------------------------------------------------------------------------
    static string inviteCode = "";
    static int inviteStage;          // 0 start, 1 checking, 2 error shown
    static string inviteErr = "";

    static string QueryParam(string key)
    {
        string q = ZbWs.Query() ?? "";
        foreach (var kv in q.TrimStart('?').Split('&'))
        {
            int i = kv.IndexOf('=');
            if (i > 0 && kv.Substring(0, i) == key) return Uri.UnescapeDataString(kv.Substring(i + 1));
        }
        return null;
    }

    static IEnumerator InviteRoutine(sc_gamecontrol g)
    {
        long code = 0; string body = "";
        RoomInfo info = null;
        // the host just left and a remaining player is taking the room over (phase 4 claim restart, a few seconds): a join now would be refused
        // ("host away") and land on the Direct IP screen, so wait for the new host first (up to 20 s)
        for (int tries = 0; ; tries++)
        {
            yield return Get("/api/room/" + Uri.EscapeDataString(inviteCode), (c, b) => { code = c; body = b; });
            if (g.gamestate != ST_INVITE) yield break;
            info = code == 200 ? JsonUtility.FromJson<RoomInfo>(body) : null;
            if (info == null || !info.exists || info.hostState == "ok" || info.hostState == "hidden" || tries >= 40) break;
            yield return new WaitForSecondsRealtime(0.5f);
            if (g.gamestate != ST_INVITE) yield break;
        }
        if (code == 404) { Fail("Room " + inviteCode + " was not found or is closed."); yield break; }
        if (info == null || !info.exists) { Fail(code == 200 ? "Room " + inviteCode + " was not found or is closed." : "Could not reach the server, try again."); yield break; }
        if (info.type != Type(g)) { Fail("This room runs another version of the game."); yield break; }
        if (info.ended) { Fail("This match has already ended."); yield break; }
        if (info.full) { Fail("The room is full."); yield break; }
        ZbWs.ClearInviteParam();
        if (info.pw)
        {
            // never a password in the URL: ask for it with the original password screen (state 28)
            g.ipstoconec = new[] { inviteCode }; g.trynow = 0; g.ipstotry = 1; if (g.porttoconec == 0) g.porttoconec = 25678;
            g.the_password = ""; g.gamestate = 28; needClick = true;
            yield break;
        }
        JoinRoom(g, inviteCode, false);
    }
    static void Fail(string msg) { inviteErr = msg; inviteStage = 2; }

    static void DrawInvite(sc_gamecontrol g, int cx)
    {
        Frame(g);
        if (inviteStage == 0) { inviteStage = 1; ZbNet.Run(InviteRoutine(g)); }
        if (inviteStage == 1) { Msg(g, cx, -100, "Joining room " + inviteCode + "...", Color.yellow); if (Btn(g, cx, 0, "Cancel")) { ZbWs.ClearInviteParam(); g.gamestate = 0; } return; }
        Msg(g, cx, -150, inviteErr, Color.red);
        if (Btn(g, cx, -80, "Quick Play")) { ZbWs.ClearInviteParam(); StartQuick(g); }
        if (Btn(g, cx, -30, "Main menu")) { ZbWs.ClearInviteParam(); g.gamestate = 0; }
    }

    // ---- waves: Timed (default) / Classic (playtest feature) ----------------------------------------------------------------------------
    // Timed: the next wave starts when (almost) all zombies are dead OR when 120 s have passed, whichever comes first (the original has the 120 s timer
    // too, but arms it only if the previous wave was cleared). Classic: exactly the original. Quick Play rooms are always Timed.
    public static bool timedWaves = true;
    static bool wavesLoaded;
    public static bool TimedWaves()
    {
        if (!wavesLoaded) { wavesLoaded = true; timedWaves = PlayerPrefs.GetInt("ZBX_classic_waves", 0) != 1; }
        int carried = ZbCarry.WavesOverride(); if (carried >= 0 && Network.isServer) return carried == 1; // level B+: a continued match keeps the old room's rule
        return quickHosting || timedWaves || !Network.isServer; // only the host's value matters; Quick Play rooms are Timed
    }
    // the create-server screen (state 25), where the original had the NAT toggle (ignored in this port)
    public static void WavesToggle(sc_gamecontrol g, int cx)
    {
        TimedWaves();
        string t = timedWaves ? "Waves: Timed (next wave after 2 min at the latest)" : "Waves: Classic (original rules)";
        if (GUI.Button(new Rect(cx - 200, Screen.height / 2 + 5, 400f, 32f), t))
        {
            timedWaves = !timedWaves; PlayerPrefs.SetInt("ZBX_classic_waves", timedWaves ? 0 : 1); PlayerPrefs.Save();
        }
    }

    // Zombies per wave in Timed waves: (zminforwave + wave - 1) * (1 + 0.5 * (players - 1)), i.e. 1 player x1, 2 x1.5, 3 x2, 4 x2.5. Per-zombie stats
    // stay the original's. Nothing is dropped: zombies that cannot spawn now (cap on zombies alive, or the frame rate guard) wait in the host's
    // backlog (sc_matchcontroller.zbacklog, with their wave number) and spawn ZBACKLOG_RATE per second as soon as there is room. The cap on zombies
    // alive is only a crash guard: zombieCeiling (ZOMBIE_CEILING, measured with server/test/e2e/fps.mjs, see docs/MULTIPLAYER.md). Classic: the
    // original's 50 alive, the rest of a wave is dropped, no backlog.
    public const int ZOMBIE_CEILING = 300;
    public static int zombieCeiling = ZOMBIE_CEILING;     // DebugZmax overrides it (tests / measurements)
    public const float ZBACKLOG_RATE = 4f;                // backlog zombies per second
    // human players connected to this match (the host's own `spawners` counter is one too high: the host's spawner is added twice)
    public static int Players() { return ZbNet.Connected ? ZbNet.remoteCount + 1 : 1; }
    public static float WaveFactor(int players) { return 1f + 0.5f * (Mathf.Max(1, players) - 1); }
    public static int WaveZombies(int basecount, int players) { return TimedWaves() ? Mathf.RoundToInt(basecount * WaveFactor(players)) : basecount; }
    public static int ZombieCap(int orig, int players) { return TimedWaves() ? Mathf.Max(1, zombieCeiling) : orig; }

    // Frame rate guard (Timed waves, host): when the host's frame rate stays below GUARD_LOW_FPS for GUARD_LOW_S seconds, no more zombies spawn
    // (wave starts queue their zombies, the backlog waits) until it is back at GUARD_RESUME_FPS or more for GUARD_RESUME_S seconds. Nothing is lost.
    public const float GUARD_LOW_FPS = 25f, GUARD_LOW_S = 3f, GUARD_RESUME_FPS = 30f, GUARD_RESUME_S = 2f;
    public static bool guardPaused;
    // the guard never holds back zombies while fewer than GUARD_MIN_ALIVE are alive (the original's 50): a slow PC still gets the original game
    public const int GUARD_MIN_ALIVE = 50;
    public static bool SpawnBlocked(int alive) { return guardPaused && alive >= GUARD_MIN_ALIVE; }
    public static void GuardReset() { guardPaused = false; guardFps = 60f; guardLow = guardHigh = 0f; }
    public static int guardForce = -1;                    // test hook DebugZGuard: -1 automatic, 0 never pause, 1 always pause
    public static float guardFps = 60f;
    static float guardLow, guardHigh;
    public static void GuardTick(float dt)
    {
        if (dt > 0f)
        {
            guardFps += (Mathf.Min(1f / dt, 240f) - guardFps) * Mathf.Min(1f, dt * 2f); // about half a second of smoothing
            if (!guardPaused) { guardLow = guardFps < GUARD_LOW_FPS ? guardLow + dt : 0f; if (guardLow >= GUARD_LOW_S) { guardPaused = true; guardHigh = 0f; Debug.Log("[zbguard] host fps " + Mathf.Round(guardFps) + ": zombie spawning paused"); } }
            else { guardHigh = guardFps >= GUARD_RESUME_FPS ? guardHigh + dt : 0f; if (guardHigh >= GUARD_RESUME_S) { guardPaused = false; guardLow = 0f; Debug.Log("[zbguard] host fps " + Mathf.Round(guardFps) + ": zombie spawning resumed"); } }
        }
        if (guardForce >= 0) guardPaused = guardForce == 1;
    }

    // ---- loot: More (default) / Classic (playtest feature) ---------------------------------------------------------------------------------
    // Classic: the original matchcontroller values, never touched (itemspawnrate 0.05 = one item / 20 s, ismax 20, 30 % real items, 2 % zombie backpacks).
    // More: scaled with the number of players in the match, recomputed every frame on the host: item interval 10 / 7 / 5 / 4 s for 1 / 2 / 3 / 4+ players,
    // item cap 40 + 10 per extra player (max 80), 50 % real items (rest ammo crates), 10 % zombie backpacks, and grenades (grenadeground,
    // stickgrenadeground) join the random item list. Only the host spawns loot; the prefab values are not edited (runtime override).
    public static bool moreLoot = true;
    static bool lootLoaded, lootInit; static sc_matchcontroller lootFor;
    public static bool MoreLoot()
    {
        if (!lootLoaded) { lootLoaded = true; moreLoot = PlayerPrefs.GetInt("ZBX_classic_loot", 0) != 1; }
        int carried = ZbCarry.LootOverride(); if (carried >= 0) return carried == 1; // level B+: a continued match keeps the old room's rule
        return quickHosting || moreLoot;
    }
    public static void LootToggle(sc_gamecontrol g, int cx)
    {
        MoreLoot();
        GUI.color = Color.red;
        GUI.DrawTexture(new Rect(cx - g.newhud[0].width / 2, Screen.height / 2 + 50, g.newhud[0].width, g.newhud[0].height), g.newhud[0], ScaleMode.StretchToFill, true, 0f);
        GUI.color = Color.white;
        string t = moreLoot ? "Loot: More (scales with the players, grenades)" : "Loot: Classic (original rules)";
        if (GUI.Button(new Rect(cx - 200, Screen.height / 2 + 55, 400f, 32f), t)) { moreLoot = !moreLoot; PlayerPrefs.SetInt("ZBX_classic_loot", moreLoot ? 0 : 1); PlayerPrefs.Save(); }
    }
    public static void LootFor(int players, out float interval, out int cap)
    {
        interval = players <= 1 ? 10f : players == 2 ? 7f : players == 3 ? 5f : 4f;
        cap = Mathf.Min(80, 40 + 10 * (Mathf.Max(1, players) - 1));
    }
    // sc_matchcontroller.Update (host), before the item spawner runs
    public static void ApplyLoot(sc_matchcontroller mc)
    {
        if (!MoreLoot()) return;
        if (!lootInit || lootFor != mc)
        {
            lootInit = true; lootFor = mc;
            var add = new List<Transform>(); var w = new List<int>(mc.itensprob);
            foreach (var n in new[] { "grenadeground", "stickgrenadeground" })
            {
                var pf = ZbNet.FindPrefab(n);
                if (pf != null) { add.Add(pf.transform); w.Add(20); } else Debug.LogWarning("[zbmenu] loot prefab missing: " + n);
            }
            var l = new List<Transform>(mc.itemsrand); l.AddRange(add);
            mc.itemsrand = l.ToArray(); mc.itensprob = w.ToArray();
        }
        float interval; int cap; LootFor(Players(), out interval, out cap);
        mc.itemspawnrate = 1f / interval; mc.ismax = cap; mc.probmin = 50; mc.backpackprobmin = 5;
    }

    // ---- host left (phase 4) ------------------------------------------------------------------------------------
    // Level A (approved): when the host leaves, the players keep the money of the match (banked like the ESC Disconnect does).
    // Level B: the relay picks a remaining player as the new host ("claim restart"); everybody else polls /api/room/CODE and joins the new match
    // of the same room code, without going through the menu. The old match is over for everybody (wave 1 again, avatars are new, money is kept).
    static int restartToken; static bool claiming, claimLost, banked;
    static string leftStatus = ""; static int leftMoney; static float leftAt;
    static ZbNet.Lost leftSession;

    public static void HostLeftBank(sc_gamecontrol g)
    {
        banked = false;
        if (ZbNet.lastEndReason != 4 || g.gamestate != 6) return;
        var sp = g.instedspawner != null ? g.instedspawner.GetComponent<sc_spawner>() : null;
        int m = sp != null ? sp.money : 0;
        g.lastaccmoney = m; g.money += m; g.save();
        ZbCarry.Freeze(); // level B+: wave, match time, room rules and this player's own items for the continued match
        g.errormsg = "The host left the game. Your money was saved.";
        banked = true; leftMoney = m;
        Debug.Log("[zbmenu] host left: banked " + m + " (total " + g.money + ")");
    }

    public static void HostLeftScreen(sc_gamecontrol g)
    {
        // The original unparents the camera (cam.parent = null) before leaving a match: that puts it into the city scene, which is destroyed by the
        // scene change, and the next match (host start: cam.parent = observator) then throws. Keep it alive.
        if (g.cam != null) UnityEngine.Object.DontDestroyOnLoad(g.cam.gameObject);
        if (!banked) return;
        banked = false;
        leftSession = ZbNet.lost;
        if (!leftSession.restart || g.gamestate != 8) return; // nobody to continue with (relay closed the room): the plain error screen
        g.gamestate = ST_HOSTLEFT; leftAt = Time.unscaledTime; claimLost = false; claiming = false;
        leftStatus = "Looking for the new host...";
        ZbNet.Run(RestartRoutine(g, ++restartToken));
    }

    static IEnumerator RestartRoutine(sc_gamecontrol g, int token)
    {
        var L = leftSession;
        int gone = 0; float t0 = Time.unscaledTime;
        while (token == restartToken && g.gamestate == ST_HOSTLEFT)
        {
            long code = 0; string body = "";
            yield return Get("/api/room/" + Uri.EscapeDataString(L.code), (c, b) => { code = c; body = b; });
            if (token != restartToken || g.gamestate != ST_HOSTLEFT) yield break;
            var info = code == 200 ? JsonUtility.FromJson<RoomInfo>(body) : null;
            if (info != null && info.restart != null) ZbCarry.RelayInfo(info.restart.wave, info.restart.wave > 0);
            if (code == 404 || (info != null && !info.exists)) { if (++gone >= 3) { leftStatus = "The room was closed."; yield break; } }
            else if (info != null)
            {
                gone = 0;
                if (info.hostState == "ok" && info.epoch != L.epoch)
                {
                    leftStatus = "Joining the new match..."; Debug.Log("[zbmenu] restart: joining the new match of " + L.code);
                    ZbCarry.Begin(false); // level B+: re-apply my own items when my new avatar spawns
                    JoinRoom(g, L.code, false, true);
                    yield break;
                }
                if (info.hostState == "restarting" && !claimLost && info.restart != null)
                {
                    bool mine = info.restart.successor == L.id;
                    if (mine || info.restart.claimOpen)
                    {
                        float wait = 3f - (Time.unscaledTime - leftAt); // a short, visible pause (and a chance to cancel)
                        if (wait > 0f) leftStatus = "You are the new host. " + (ZbCarry.Continues ? "Continuing the match" : "Starting a new match") + " in " + Mathf.CeilToInt(wait) + "...";
                        else { leftStatus = "Starting the new match..."; Debug.Log("[zbmenu] restart: claiming " + L.code + " as host"); Claim(g, L); yield break; }
                    }
                    else leftStatus = "Waiting for " + (info.restart.claimOpen ? "a" : "the") + " new host...";
                }
            }
            if (Time.unscaledTime - t0 > 60f) { leftStatus = "Could not find the new match."; yield break; }
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    static void Claim(sc_gamecontrol g, ZbNet.Lost L)
    {
        claiming = true; quickJoining = false; quickHosting = false; needClick = true; countdownEnd = -1f; hostCd = -1f; hostShrunk = false;
        ZbNet.SetClaim(L.code, L.id, L.token);
        ZbCarry.Begin(true); // level B+: the new match continues at the old wave with the old room's rules
        g.iamserver = true;
        Network.InitializeSecurity();
        Network.incomingPassword = "";
        Network.InitializeServer(16, g.porttoconec == 0 ? 25678 : g.porttoconec, false);
        g.gamestate = 3;
        SceneManager.LoadScene(1);
    }

    static void DrawHostLeft(sc_gamecontrol g, int cx)
    {
        Frame(g);
        // the continue timer is polled by the routine; the screen only shows its latest status
        Msg(g, cx, -190, "The host left the game.", Color.yellow);
        Msg(g, cx, -150, "Your money from this match ($" + leftMoney + ") was saved.", Color.white);
        { string cl = ZbCarry.ScreenLine(); if (cl != null) Msg(g, cx, -120, cl, Color.white); } // level B+
        Msg(g, cx, -90, leftStatus, Color.green);
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true; // the match left the pointer locked (a script of the old scene re-locks it after the disconnect)
        if (Btn(g, cx, 0, "Main menu")) { restartToken++; g.gamestate = 0; }
    }

    // ---- settings ----------------------------------------------------------------------------------------------
    static void DrawSettings(sc_gamecontrol g, int cx)
    {
        Frame(g);
        if (DrawSettingsBody(g, cx, "Go back")) { g.gamestate = 9; g.errormsg = g.language[g.languageset].frase[9]; }
    }

    // The settings rows (shared by the main menu screen and the in-game pause panel). Returns true when the last button was pressed.
    static bool DrawSettingsBody(sc_gamecontrol g, int cx, string closeText)
    {
        ZbSettings.Load();
        int y = -150;
        // field of view
        Slot(g, cx, y);
        GUI.Label(new Rect(cx - 190, Screen.height / 2 + y + 5, 200f, 32f), "Field of view " + ZbSettings.fov);
        ZbSettings.fov = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(cx + 20, Screen.height / 2 + y + 14, 170f, 15f), ZbSettings.fov, ZbSettings.FOV_MIN, ZbSettings.FOV_MAX));
        y += 50;
        // mouse sensitivity (original value, saved by sc_gamecontrol.save())
        Slot(g, cx, y);
        GUI.Label(new Rect(cx - 190, Screen.height / 2 + y + 5, 200f, 32f), "Sensitivity " + g.mousesense.ToString("F2"));
        g.mousesense = GUI.HorizontalSlider(new Rect(cx + 20, Screen.height / 2 + y + 14, 170f, 15f), g.mousesense, 0.2f, 3f);
        y += 50;
        // volume (original value)
        Slot(g, cx, y);
        GUI.Label(new Rect(cx - 190, Screen.height / 2 + y + 5, 200f, 32f), "Volume " + g.audiovol.ToString("F2"));
        g.audiovol = GUI.HorizontalSlider(new Rect(cx + 20, Screen.height / 2 + y + 14, 170f, 15f), g.audiovol, 0f, 1f);
        y += 50;
        // graphics quality
        if (Btn(g, cx, y, "Graphics: " + ZbSettings.qualityNames[ZbSettings.quality]))
        {
            ZbSettings.quality = (ZbSettings.quality + 1) % 3; ZbSettings.ApplyQuality(); Persist(g);
        }
        y += 50;
        // invert Y
        if (Btn(g, cx - 30, y, "Invert vertical look")) { ZbSettings.invertY = !ZbSettings.invertY; Persist(g); }
        GUI.DrawTexture(new Rect(cx + 175, Screen.height / 2 + y + 10, ZbSettings.invertY ? 32f : 30f, ZbSettings.invertY ? 22f : 25f), ZbSettings.invertY ? g.enabledsign : g.disabledsign, ScaleMode.StretchToFill, true, 0f);
        y += 50;
        // sliders save when the mouse is released (not every frame of a drag): a slider consumes the MouseUp event, so watch the hot control
        if (Event.current.type == EventType.Repaint)
        {
            int hot = GUIUtility.hotControl;
            if (prevHot != 0 && hot == 0) Persist(g);
            prevHot = hot;
        }
        if (Btn(g, cx, y, closeText)) { Persist(g); return true; }
        return false;
    }

    static int prevHot;
    static void Persist(sc_gamecontrol g) { ZbSettings.Save(); g.save(); ApplySens(g); }

    // sensitivity is copied into the player at spawn in the original; push a changed value into the running player too
    static void ApplySens(sc_gamecontrol g)
    {
        foreach (var p in UnityEngine.Object.FindObjectsByType<sc_player>(FindObjectsSortMode.None)) { var nv = p.GetComponent<NetworkView>(); if (nv != null && nv.isMine) p.mouse_sense = g.mousesense; }
        foreach (var sp in UnityEngine.Object.FindObjectsByType<sc_spawner>(FindObjectsSortMode.None)) { var nv = sp.GetComponent<NetworkView>(); if (nv != null && nv.isMine) sp.mousesense = g.mousesense; }
    }

    // ---- in-game settings (the pause panel's Options button) -------------------------------------------------
    public static bool overlay;
    static float nextOverlayCheck;

    // ---- server list (state 26) --------------------------------------------------------------------------------
    public static bool Full(int index)
    {
        var l = ZbNet.hostList;
        return index >= 0 && index < l.Length && l[index].connectedPlayers >= l[index].playerLimit;
    }

    static string Wave(HostData h) { return h.ended ? "Ended" : h.started ? "Wave " + Mathf.Max(1, h.wave) : "Lobby"; }
    static string Region(HostData h) { return !string.IsNullOrEmpty(h.country) ? h.country + (string.IsNullOrEmpty(h.colo) ? "" : " " + h.colo) : (string.IsNullOrEmpty(h.colo) ? "-" : h.colo); }

    static int ColX(int k) { return Mathf.Max(690, Screen.width - 123 - 100 - 90 * k); } // k = 0 region, 1 ping, 2 wave (right to left)

    public static void ListHeader(sc_gamecontrol g)
    {
        GUI.Label(new Rect(ColX(2), 10f, 90f, 19f), "Wave");
        GUI.Label(new Rect(ColX(1), 10f, 90f, 19f), "Ping");
        GUI.Label(new Rect(ColX(0), 10f, 100f, 19f), "Region");
        GUI.Label(new Rect(Screen.width - 123, 10f, 90f, 19f), "Players");
    }

    public static void ListRow(sc_gamecontrol g, HostData h, float y)
    {
        GUI.Label(new Rect(ColX(2), y, 90f, 19f), Wave(h));
        GUI.Label(new Rect(ColX(1), y, 90f, 19f), h.ping >= 0 ? h.ping + " ms" : "...");
        GUI.Label(new Rect(ColX(0), y, 100f, 19f), Region(h));
        if (h.connectedPlayers >= h.playerLimit)
        {
            var old = GUI.color; GUI.color = Color.red;
            GUI.Label(new Rect(Screen.width - 175, y, 50f, 19f), "FULL");
            GUI.color = old;
        }
    }

    // ---- per frame work (NetRunner.Update) ---------------------------------------------------------------------
    static int lastState = -99;
    static float nextRefresh;
    static string selCode;
    static int listStamp;
    static float nextMeta; static int lastMetaKey = -1, lastMetaCd = -1;
    public static float countdownEnd = -1f;      // everybody: when the auto start fires (Time.unscaledTime), -1 none
    static float hostCd = -1f; static bool hostShrunk;

    public static void Tick()
    {
        ZbSettings.Load();
        ZbSettings.Tick();
        var g = UnityEngine.Object.FindFirstObjectByType<sc_gamecontrol>();
        if (g == null) return;

        // server list: refresh on open and every 10 s, keep the selected row selected
        if (g.gamestate == 26)
        {
            if (lastState != 26) { nextRefresh = Time.unscaledTime + 10f; }
            else if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 10f;
                selCode = g.server_select >= 0 && g.server_select < ZbNet.hostList.Length ? ZbNet.hostList[g.server_select].guid : null;
                MasterServer.RequestHostList(Type(g));
            }
            if (ZbNet.hostListStamp != listStamp)
            {
                listStamp = ZbNet.hostListStamp;
                if (selCode != null) { g.server_select = -1; for (int i = 0; i < ZbNet.hostList.Length; i++) if (ZbNet.hostList[i].guid == selCode) g.server_select = i; selCode = null; }
            }
        }
        if (lastState != g.gamestate)
        {
            if (g.gamestate == 6) quickJoining = false; // connected
            lastState = g.gamestate;
        }
        if (g.gamestate == 0) needClick = false;
        if (claiming && ZbNet.Connected) claiming = false;

        ZbBoard.Tick(g); // phase 5: match results (before HostTick: the last C_SCORE goes out before the C_META that ends the match)
        HostTick(g);
    }

    // The host reports wave / started / ended to the lobby (C_META) and runs the Quick Play auto start.
    static void HostTick(sc_gamecontrol g)
    {
        if (!(ZbNet.Connected && Network.isServer && g.gamestate == 3)) { if (g.gamestate != 3) { quickHosting = false; } return; }
        var mc = UnityEngine.Object.FindFirstObjectByType<sc_matchcontroller>();
        if (mc == null) return;
        int cd = 0;
        if (quickHosting && !mc.gamestarted)
        {
            // Quick Play room: start by itself. 10 s after the host is in the match; when another player joins the rest is cut to 5 s.
            if (hostCd < 0f)
            {
                // The original ends a match at once when nobody is spawned (sc_matchcontroller: no "Player" object = game ended), and a player
                // spawns on the first click, so the countdown starts when the first player is in the scene.
                if (GameObject.FindGameObjectsWithTag("Player").Length > 0) hostCd = Time.unscaledTime + 10f;
            }
            if (hostCd >= 0f)
            {
                if (ZbNet.remoteCount > 0 && !hostShrunk) { hostShrunk = true; hostCd = Mathf.Min(hostCd, Time.unscaledTime + 5f); }
                float left = hostCd - Time.unscaledTime;
                if (left <= 0f) { if (GameObject.FindGameObjectsWithTag("Player").Length > 0) { mc.gamestarted = true; hostCd = -1f; } }
                else cd = Mathf.Clamp(Mathf.CeilToInt(left), 1, 60);
            }
            countdownEnd = cd > 0 ? hostCd : -1f;
        }
        else countdownEnd = -1f;

        int flags = (mc.gamestarted ? 1 : 0) | (mc.gameended ? 2 : 0);
        int key = (Mathf.Min(mc.wave, 9999) << 10) | (flags << 8) | Mathf.Min(mc.spawners, 255);
        // on change (wave / flags / players / the countdown's seconds, i.e. once a second while it runs) and at least every 30 s
        if (!(key != lastMetaKey || cd != lastMetaCd || Time.unscaledTime >= nextMeta)) return;
        nextMeta = Time.unscaledTime + 30f; lastMetaKey = key; lastMetaCd = cd;
        ZbNet.SendMeta(Mathf.Min(mc.wave, 9999), flags, Mathf.Min(mc.spawners, 255), cd);
    }

    // ---- overlays drawn on top of everything (NetRunner.OnGUI) -------------------------------------------------
    static GUIStyle big;
    public static void OverlayGUI()
    {
        var g = UnityEngine.Object.FindFirstObjectByType<sc_gamecontrol>();
        if (g == null || (g.gamestate != 3 && g.gamestate != 6)) { overlay = false; return; }
        if (overlay)
        {
            // closes by itself when the pause panel is left (ESC, Resume)
            if (Time.unscaledTime >= nextOverlayCheck)
            {
                nextOverlayCheck = Time.unscaledTime + 0.25f;
                bool paused = false;
                foreach (var sp in UnityEngine.Object.FindObjectsByType<sc_spawner>(FindObjectsSortMode.None)) { var nv = sp.GetComponent<NetworkView>(); if (nv != null && nv.isMine && sp.state == 2) paused = true; }
                if (!paused) overlay = false;
            }
            if (overlay) DrawOverlaySettings(g);
        }
        if (big == null || big.font == null)
        {
            var src = g.newgui_square != null ? g.newgui_square.label : GUI.skin.label;
            big = new GUIStyle(src) { fontSize = 30, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false };
            big.normal.textColor = Color.white;
        }
        var shadow = new GUIStyle(big); shadow.normal.textColor = Color.black;
        { // phase 4: connection lost / host lost / host tab hidden
            Color sc; string st = ZbNet.StatusText(out sc);
            if (st != null)
            {
                var small = new GUIStyle(big) { fontSize = 20 }; var sshadow = new GUIStyle(small); sshadow.normal.textColor = Color.black; small.normal.textColor = sc;
                var rr = new Rect(0, 100, Screen.width, 40);
                GUI.Label(new Rect(rr.x + 2, rr.y + 2, rr.width, rr.height), st, sshadow);
                GUI.Label(rr, st, small);
            }
        }
        if (countdownEnd > 0f)
        {
            int s = Mathf.CeilToInt(countdownEnd - Time.unscaledTime);
            if (s > 0)
            {
                var r = new Rect(0, 60, Screen.width, 50);
                GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), "Match starts in " + s, shadow);
                GUI.Label(r, "Match starts in " + s, big);
            }
        }
        if (needClick && (g.gamestate == 6 || g.gamestate == 3))
        {
            var r = new Rect(0, Screen.height - 140, Screen.width, 50);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), "Click to play", shadow);
            GUI.Label(r, "Click to play", big);
            if (Event.current.type == EventType.MouseDown) needClick = false;
        }
    }

    static void DrawOverlaySettings(sc_gamecontrol g)
    {
        int cx = Screen.width / 2;
        GUI.depth = -1100;
        GUI.skin = g.newgui_square;
        GUI.color = new Color(0f, 0f, 0f, 0.92f);
        GUI.DrawTexture(new Rect(cx - 250, Screen.height / 2 - 175, 500, 360), Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (DrawSettingsBody(g, cx, "Back to the game")) overlay = false;
        GUI.Button(new Rect(cx - 250, Screen.height / 2 - 175, 500, 360), GUIContent.none, GUIStyle.none); // swallow stray clicks (drawn last: the rows above win)
    }

    // incoming S_COUNTDOWN (clients)
    public static void OnCountdown(int secs) { countdownEnd = secs > 0 ? Time.unscaledTime + secs : -1f; }

    // test hooks (NetRunner.Debug*): one JSON object with the menu / session state that the tests assert on
    public static string DebugJson()
    {
        var g = UnityEngine.Object.FindFirstObjectByType<sc_gamecontrol>();
        int deffov = -1;
        foreach (var p in UnityEngine.Object.FindObjectsByType<sc_player>(FindObjectsSortMode.None)) { var nv = p.GetComponent<NetworkView>(); if (nv != null && nv.isMine) deffov = p.def_fov; }
        var mc = UnityEngine.Object.FindFirstObjectByType<sc_matchcontroller>();
        int cd = countdownEnd > 0f ? Mathf.CeilToInt(countdownEnd - Time.unscaledTime) : 0;
        string list = "[";
        var hl = ZbNet.hostList;
        for (int i = 0; i < hl.Length; i++)
            list += (i > 0 ? "," : "") + "{\"code\":\"" + hl[i].guid + "\",\"name\":\"" + hl[i].gameName.Replace("\"", "") + "\",\"players\":" + hl[i].connectedPlayers + ",\"max\":" + hl[i].playerLimit
                + ",\"wave\":" + hl[i].wave + ",\"started\":" + (hl[i].started ? 1 : 0) + ",\"ping\":" + hl[i].ping + ",\"colo\":\"" + hl[i].colo + "\",\"country\":\"" + hl[i].country + "\"}";
        list += "]";
        return "{\"gamestate\":" + (g != null ? g.gamestate : -99) + ",\"countdown\":" + cd + ",\"quickHosting\":" + (quickHosting ? 1 : 0) + ",\"needClick\":" + (needClick ? 1 : 0) + ",\"overlay\":" + (overlay ? 1 : 0)
            + ",\"net\":{\"reconnecting\":" + (ZbNet.reconnecting ? 1 : 0) + ",\"hostState\":" + ZbNet.hostState + ",\"seq\":" + ZbNet.LastSeq + ",\"sent\":" + ZbNet.SentBuffered + ",\"epoch\":" + ZbNet.Epoch + ",\"grace\":" + ZbNet.GraceSec + ",\"attempts\":" + ZbNet.Attempts + ",\"claiming\":" + (claiming ? 1 : 0) + ",\"leftStatus\":\"" + leftStatus.Replace("\"", "") + "\",\"leftMoney\":" + leftMoney + "}"
            + ",\"started\":" + (mc != null && mc.gamestarted ? 1 : 0) + ",\"deffov\":" + deffov + ",\"inviteErr\":\"" + inviteErr.Replace("\"", "") + "\",\"nick\":\"" + (g != null ? g.playernick : "") + "\",\"list\":" + list + ",\"board\":" + ZbBoard.DebugJson() + "}";
    }
    // test hooks (NetRunner.Debug*)
    public static void DebugSkipNick() { PlayerPrefs.SetInt("ZBX_nick_ok", 1); PlayerPrefs.Save(); }
}
