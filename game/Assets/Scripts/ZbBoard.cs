// Phase 5 (docs/LEADERBOARD.md): leaderboards. Everything sits around the original game, like ZbMenu:
//   * the result of a match: multiplayer hosts send every player's kills to the relay (C_SCORE), which records the match itself when it ends;
//     offline single player asks /api/run for a signed start token when the match starts and posts /api/score when it ends (or is left)
//   * state 35: the Leaderboard screen (main menu, in the original's unused grey "Credits" slot)
//   * one line on the original "End of match" screen (state 12): where this run placed
// Only default rules count (Timed waves + More loot, what Quick Play always uses); Classic matches are not ranked.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public static class ZbBoard
{
    public const int ST_BOARD = 35;

    // ---- the match result ---------------------------------------------------------------------------------------------------------------
    static int lastState = -99;
    static bool tracking, submitted, wasStarted;   // this peer runs the match (host or offline solo) and it has started
    static bool solo;                              // offline single player (no relay)
    static string token = "";                      // solo start token (/api/run)
    static int sWave, sKills, sSecs, sRules;       // last snapshot of the running match (a solo player may leave mid-match)
    static float nextScore; static int lastScoreWave = -1;
    public static string result = "";              // line for the end-of-match screen
    public static Color resultColor = Color.white;
    static int resultGen;

    static int Rules() { return (ZbMenu.TimedWaves() ? 1 : 0) | (ZbMenu.MoreLoot() ? 2 : 0); }

    // ZbMenu.Tick, every frame (before HostTick, so the final C_SCORE goes out before the C_META that ends the match)
    public static void Tick(sc_gamecontrol g)
    {
        if (g.gamestate != lastState)
        {
            if (g.gamestate == 3 || g.gamestate == 6) { result = ""; resultGen++; } // a new match: forget the last result
            if (lastState == 3 && tracking && wasStarted && !submitted && solo) SubmitSolo(g); // a solo run that was left (ESC > Disconnect, ...)
            if (g.gamestate != 3) { tracking = false; wasStarted = false; }
            lastState = g.gamestate;
        }
        if (g.gamestate != 3 || !Network.isServer) return;
        var mc = UnityEngine.Object.FindFirstObjectByType<sc_matchcontroller>();
        if (mc == null) return;
        if (!tracking) { tracking = true; wasStarted = false; submitted = false; token = ""; lastScoreWave = -1; nextScore = 0f; }
        solo = Network.maxConnections <= 1; // Play alone: offline loopback (InitializeServer(1)); online rooms always have 2+
        if (mc.gamestarted && !wasStarted)
        {
            wasStarted = true;
            if (solo) ZbNet.Run(StartRun(resultGen));
        }
        if (!wasStarted) return;
        if (!solo && !ZbNet.Connected) return;
        sWave = mc.wave; sSecs = mc.minute * 60 + mc.second; sRules = Rules();
        if (solo)
        {
            var sp = mc.myspawner != null ? mc.myspawner.GetComponent<sc_spawner>() : null;
            if (sp != null) sKills = sp.kills;
            if (mc.gameended && !submitted) SubmitSolo(g);
            return;
        }
        // multiplayer host: on a new wave, at the end, and every 15 s (what the relay records if the host leaves mid-match)
        if (mc.wave != lastScoreWave || mc.gameended || Time.unscaledTime >= nextScore)
        {
            if (mc.gameended && submitted) return;
            if (mc.gameended) submitted = true;
            lastScoreWave = mc.wave; nextScore = Time.unscaledTime + 15f;
            var list = new List<KeyValuePair<string, int>>();
            foreach (var sp in Spawners(mc)) list.Add(new KeyValuePair<string, int>(sp.playername ?? "", sp.kills));
            ZbNet.SendScore(sRules, mc.wave, list);
        }
    }

    // the match's players (the host's own spawner is in the original's list twice, see ZbMenu.Players)
    public static List<sc_spawner> Spawners(sc_matchcontroller mc)
    {
        var l = new List<sc_spawner>();
        for (int i = 0; i < mc.spawners && i < mc.everybodyspawners.Length; i++)
        {
            var o = mc.everybodyspawners[i]; if (o == null) continue;
            var sp = o.GetComponent<sc_spawner>(); if (sp != null && !l.Contains(sp)) l.Add(sp);
        }
        return l;
    }

    [Serializable] class TokenRes { public string t; }
    [Serializable] class ScoreReq { public string t, nick, rules; public int wave, kills, secs; }
    [Serializable] class ScoreRes { public bool ok, board; public string mode, error, reason; public int today, all; }

    static IEnumerator Post(string path, string json, Action<long, string> done)
    {
        using (var wr = UnityWebRequest.Post(ZbWs.Origin() + path, json, "application/json"))
        {
            wr.timeout = 10;
            yield return wr.SendWebRequest();
            done(wr.responseCode, wr.downloadHandler != null ? wr.downloadHandler.text : "");
        }
    }

    static IEnumerator StartRun(int gen)
    {
        long code = 0; string body = "";
        yield return Post("/api/run", "{}", (c, b) => { code = c; body = b; });
        if (gen != resultGen) yield break;
        var r = code == 200 ? JsonUtility.FromJson<TokenRes>(body) : null;
        token = r != null && r.t != null ? r.t : "";
        Debug.Log("[zbboard] solo run token " + (token.Length > 0 ? "ok" : "failed (" + code + ")"));
    }

    static void SubmitSolo(sc_gamecontrol g)
    {
        submitted = true;
        string nick = ZbMenu.CleanNick(g.playernick);
        if ((sRules & 3) != 3) { Set("Classic rules: this match is not on the leaderboard.", Color.gray); }
        else if (nick == null) { Set("Choose a nickname (Quick Play asks for one) to get on the leaderboard.", Color.gray); return; }
        else if (token.Length == 0) { Set("Could not reach the leaderboard (offline?).", Color.gray); return; }
        else Set("Saving your result...", Color.white);
        var req = new ScoreReq { t = token, nick = nick ?? "", rules = (sRules & 3) == 3 ? "std" : "classic", wave = sWave, kills = sKills, secs = sSecs };
        Debug.Log("[zbboard] solo result wave " + sWave + " kills " + sKills + " secs " + sSecs + " rules " + req.rules);
        ZbNet.Run(SendSolo(JsonUtility.ToJson(req), resultGen, (sRules & 3) == 3));
    }

    static IEnumerator SendSolo(string json, int gen, bool ranked)
    {
        long code = 0; string body = "";
        yield return Post("/api/score", json, (c, b) => { code = c; body = b; });
        Debug.Log("[zbboard] score answer " + code + " " + body);
        if (!ranked) yield break;
        // the end-of-match screen comes a few seconds after the match ended: a newer match (gen) must not get this line
        if (gen != resultGen) yield break;
        var r = JsonUtility.FromJson<ScoreRes>(string.IsNullOrEmpty(body) ? "{}" : body);
        if (code == 200 && r != null && r.ok && r.board) OnResult(0, r.today, r.all);
        else if (code == 422) Set("This run did not pass the leaderboard checks.", Color.gray);
        else Set("Could not save the result (" + code + ").", Color.gray);
    }

    static void Set(string t, Color c) { result = t; resultColor = c; }

    // S_RESULT from the relay (multiplayer), or the /api/score answer (solo)
    public static void OnResult(int board, int today, int all)
    {
        if (board == 255) { Set("Not on the leaderboard (Classic rules or the checks failed).", Color.gray); return; }
        string b = board == 1 ? "team" : "solo";
        Set("Leaderboard (" + b + ", best wave): #" + today + " today, #" + all + " all time", today <= 3 || all <= 10 ? Color.green : Color.white);
        Debug.Log("[zbboard] result " + b + " today " + today + " all " + all);
        stale = true;
    }

    // sc_gamecontrol state 12 ("End of match"), below the Go back button
    public static void GameOverGUI(sc_gamecontrol g, int cx)
    {
        if (result.Length == 0) return;
        var old = GUI.color; GUI.color = resultColor;
        var st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
        GUI.Label(new Rect(cx - 300, Screen.height / 2 + 60, 600f, 44f), result, st);
        GUI.color = old;
        if (GUI.Button(new Rect(cx - 100, Screen.height / 2 + 108, 200f, 28f), "Leaderboard")) Open(g);
    }

    // ---- the Leaderboard screen (state 35) ----------------------------------------------------------------------------------------------
    [Serializable] public class Row { public string n, d; public int w, k; }
    [Serializable] public class Board { public string id; public Row[] rows; }
    [Serializable] public class Lb { public string day; public Board[] boards; }

    static Lb data; static string status = ""; static bool loading, stale = true; static float loadedAt = -999f;
    static int fMode, fPeriod, fMetric;   // 0 solo / 1 team, 0 today / 1 all time, 0 best wave / 1 most kills
    static bool filtersLoaded;

    // main menu (state 0): where the original draws its unused grey "Credits" row
    public static void MainMenuButton(sc_gamecontrol g, int cx)
    {
        GUI.color = Color.red;
        GUI.DrawTexture(new Rect(cx - g.newhud[0].width / 2, Screen.height / 2 + 50, g.newhud[0].width, g.newhud[0].height), g.newhud[0], ScaleMode.StretchToFill, true, 0f);
        GUI.color = Color.white;
        if (GUI.Button(new Rect(cx - 200, Screen.height / 2 + 55, 400f, 32f), "Leaderboard")) Open(g);
    }

    public static void Open(sc_gamecontrol g)
    {
        if (!filtersLoaded)
        {
            filtersLoaded = true;
            fMode = PlayerPrefs.GetInt("ZBX_lb_mode", 0); fPeriod = PlayerPrefs.GetInt("ZBX_lb_period", 1); fMetric = PlayerPrefs.GetInt("ZBX_lb_metric", 0);
        }
        g.gamestate = ST_BOARD;
        if (stale || Time.unscaledTime - loadedAt > 60f) Load();
    }

    static void Load()
    {
        if (loading) return;
        loading = true; stale = false; status = data == null ? "Loading..." : "";
        ZbNet.Run(LoadRoutine());
    }

    static IEnumerator LoadRoutine()
    {
        using (var wr = UnityWebRequest.Get(ZbWs.Origin() + "/api/leaderboard"))
        {
            wr.timeout = 10;
            yield return wr.SendWebRequest();
            loading = false;
            if (wr.result != UnityWebRequest.Result.Success) { status = "Could not load the leaderboard."; yield break; }
            var l = JsonUtility.FromJson<Lb>(wr.downloadHandler.text);
            if (l == null || l.boards == null) { status = "Could not load the leaderboard."; yield break; }
            data = l; loadedAt = Time.unscaledTime; status = "";
        }
    }

    static Row[] Rows()
    {
        if (data == null || data.boards == null) return null;
        string id = (fMode == 0 ? "solo" : "team") + "." + (fPeriod == 0 ? "today" : "all") + "." + (fMetric == 0 ? "wave" : "kills");
        foreach (var b in data.boards) if (b.id == id) return b.rows ?? new Row[0];
        return null;
    }

    // two-way switch in the original's colours: the chosen half has a red frame and yellow text
    static void Toggle(Rect r, string a, string b, ref int v, string key)
    {
        for (int i = 0; i < 2; i++)
        {
            var h = new Rect(r.x + i * r.width / 2, r.y, r.width / 2 - 4, r.height);
            if (v == i) { GUI.color = Color.red; GUI.Box(h, string.Empty); }
            GUI.color = v == i ? Color.yellow : new Color(0.75f, 0.75f, 0.75f);
            if (GUI.Button(h, i == 0 ? a : b) && v != i) { v = i; PlayerPrefs.SetInt(key, v); PlayerPrefs.Save(); }
        }
        GUI.color = Color.white;
    }

    static GUIStyle cell, cellR;
    public static void Draw(sc_gamecontrol g, int cx)
    {
        g.sharedhud = 1; GUI.skin = g.newgui_square; GUI.color = Color.white;
        if (cell == null || cell.font == null)
        {
            cell = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, wordWrap = false, clipping = TextClipping.Clip, fontSize = 15 };
            cellR = new GUIStyle(cell) { alignment = TextAnchor.MiddleRight };
        }
        int top = Screen.height / 2 - 250;
        // filters
        Toggle(new Rect(cx - 310, top, 200, 30), "Solo", "Team", ref fMode, "ZBX_lb_mode");
        Toggle(new Rect(cx - 100, top, 200, 30), "Today", "All time", ref fPeriod, "ZBX_lb_period");
        Toggle(new Rect(cx + 110, top, 200, 30), "Wave", "Kills", ref fMetric, "ZBX_lb_metric");
        // table
        int y = top + 42, rowH = 21, nRows = 15;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(cx - 315, y - 4, 630, 26 + rowH * nRows + 8), Texture2D.whiteTexture);
        GUI.color = Color.yellow;
        GUI.Label(new Rect(cx - 305, y, 40, 22), "#", cell);
        GUI.Label(new Rect(cx - 260, y, 300, 22), fMode == 0 ? "Player" : "Team", cell);
        GUI.Label(new Rect(cx + 40, y, 70, 22), "Wave", cellR);
        GUI.Label(new Rect(cx + 115, y, 70, 22), "Kills", cellR);
        GUI.Label(new Rect(cx + 200, y, 105, 22), "Date", cellR);
        y += 26;
        var rows = Rows();
        string me = (g.playernick ?? "").Trim().ToLowerInvariant();
        if (rows == null || rows.Length == 0)
        {
            GUI.color = Color.gray;
            string t = status.Length > 0 ? status : rows == null ? "Loading..." : fPeriod == 0 ? "Nobody yet today. Be the first!" : "Nobody yet. Be the first!";
            GUI.Label(new Rect(cx - 305, y + 2 * rowH, 610, 24), t, new GUIStyle(cell) { alignment = TextAnchor.MiddleCenter });
        }
        else
        {
            for (int i = 0; i < rows.Length && i < nRows; i++)
            {
                var r = rows[i];
                bool mine = me.Length > 0 && Array.Exists((r.n ?? "").ToLowerInvariant().Split(','), x => x.Trim() == me);
                GUI.color = mine ? Color.green : i < 3 ? new Color(1f, 0.85f, 0.4f) : Color.white;
                float ry = y + i * rowH;
                GUI.Label(new Rect(cx - 305, ry, 40, rowH), (i + 1) + ".", cell);
                GUI.Label(new Rect(cx - 260, ry, 300, rowH), r.n, cell);
                GUI.Label(new Rect(cx + 40, ry, 70, rowH), "" + r.w, cellR);
                GUI.Label(new Rect(cx + 115, ry, 70, rowH), "" + r.k, cellR);
                GUI.Label(new Rect(cx + 200, ry, 105, rowH), r.d, cellR);
            }
        }
        GUI.color = Color.gray;
        GUI.Label(new Rect(cx - 310, top + 42 + 26 + rowH * nRows + 6, 620, 22), "Timed waves + More loot only (Quick Play). Best run per player / team. Days are UTC.", new GUIStyle(cell) { alignment = TextAnchor.MiddleCenter, fontSize = 12 });
        GUI.color = Color.white;
        // the original's red outlined buttons
        int by = top + 42 + 26 + rowH * nRows + 36 - Screen.height / 2;
        GUI.color = Color.red;
        GUI.DrawTexture(new Rect(cx - g.newhud[0].width / 2, Screen.height / 2 + by, g.newhud[0].width, g.newhud[0].height), g.newhud[0], ScaleMode.StretchToFill, true, 0f);
        GUI.color = Color.white;
        if (GUI.Button(new Rect(cx - 200, Screen.height / 2 + by + 5, 400f, 32f), "Go back")) g.gamestate = 0;
        if (!loading && Time.unscaledTime - loadedAt > 60f && data != null) Load(); // left open: refresh once a minute
    }

    // test hook (NetRunner.DebugMenu): {"result":..., "boardRows":n}
    public static string DebugJson()
    {
        var rows = Rows();
        return "{\"result\":\"" + result.Replace("\"", "") + "\",\"rows\":" + (rows != null ? rows.Length : -1) + ",\"first\":\"" + (rows != null && rows.Length > 0 ? rows[0].n.Replace("\"", "") : "") + "\",\"solo\":" + (solo ? 1 : 0) + ",\"token\":" + (token.Length > 0 ? 1 : 0) + "}";
    }
}
