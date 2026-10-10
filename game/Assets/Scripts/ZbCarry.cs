// Host leaves, level B+ ("continue", docs/MULTIPLAYER.md): when the host leaves and a remaining player claims the room (level B), the new match
// continues where the old one was instead of starting at wave 1.
//   * Every peer keeps a snapshot of the running match (wave, match time, zombies killed, the room's wave / loot rules, players) and of its OWN
//     player (inventory slots, selected slot, health, revives left, kills), refreshed 4 times a second while in an online match. All of it is
//     local or already synced to every peer (matchdata), so nothing of the departed host is needed.
//   * When the host leaves (reason 4) the snapshot is frozen (ZbMenu.HostLeftBank).
//   * The claimer (new host) sets the new matchcontroller to wave W - 1 with the old match time, waits until the players are back (or 25 s),
//     then starts the match: the original's first wave step runs wave++ -> W and spawns that wave exactly like a normal wave start
//     (per-wave zombie hp / damage / speed, count with the player scaling, the 120 s timer).
//   * Every player (claimer and joiners) re-applies its own inventory / health / revives / kills to its new avatar when it spawns (sc_spawner hook).
//   * The room's rules (Timed / Classic waves, More / Classic loot) are the old host's, not the claimer's own settings.
// Lost on purpose: zombies alive, ground loot, constructions, vehicles, the old host's ban list; match money is banked by level A (not carried).
using UnityEngine;

public static class ZbCarry
{
    // ---- live snapshot (this match) ----
    static float nextSnap;
    static int sWave, sMin, sSec, sKilled, sPlayers, sKills; static bool sStarted;
    static int roomFlags = -1;                 // from the host's `zbroom` RPC (clients) or own settings (host): 1 timed waves, 2 more loot
    static int sTimed = -1;                    // fallback: matchdata.waveleft >= 0 while started = Timed
    static bool sHasPlayer; static int[] sItem, sQuant, sAmmo; static int sSel, sLifes; static float sLife;

    // ---- frozen at the host-left moment ----
    public static bool frozen; static int fWave, fMin, fSec, fKilled, fPlayers, fKills, fWaves = -1, fLoot = -1; static bool fStarted, fHasPlayer;
    static int[] fItem, fQuant, fAmmo; static int fSel, fLifes; static float fLife;

    // ---- the continued match ----
    public static bool active;                 // this session continues an old match (claimer or joiner)
    static bool asHost, hostApplied, hostStarted, playerPending; static float hostPlayerAt = -1f, startAt = -1f, doneAt = -1f;
    static int wavesOverride = -1, lootOverride = -1;
    public static int Wave { get { return fWave; } }

    // room rules of a continued match (the new host uses the old host's rules); -1 = no override
    public static int WavesOverride() { return active && asHost ? wavesOverride : -1; }
    public static int LootOverride() { return active && asHost ? lootOverride : -1; }

    public static void Clear()
    {
        active = false; asHost = false; hostApplied = false; hostStarted = false; playerPending = false; frozen = false;
        hostPlayerAt = -1f; startAt = -1f; doneAt = -1f; wavesOverride = -1; lootOverride = -1;
        ResetSnap();
    }
    static void ResetSnap() { sWave = sMin = sSec = sKilled = sPlayers = sKills = 0; sStarted = false; roomFlags = -1; sTimed = -1; sHasPlayer = false; }

    // NetRunner.Update
    public static void Tick()
    {
        var g = Object.FindFirstObjectByType<sc_gamecontrol>();
        if (g == null) return;
        int gs = g.gamestate;
        if (gs == 0 || gs == 25 || gs == 26 || gs == 27 || gs == ZbMenu.ST_QUICK || gs == ZbMenu.ST_INVITE) { if (active || frozen || sStarted) Clear(); return; }
        if (!ZbNet.Connected || (gs != 3 && gs != 6)) return;
        if (Time.unscaledTime >= nextSnap) { nextSnap = Time.unscaledTime + 0.25f; Snapshot(); if (Network.isServer) SendRoom(); }
        if (active && asHost && Network.isServer && gs == 3) HostTick();
    }

    static sc_player Mine(out sc_spawner sp, out sc_deadplayer dead)
    {
        sp = null; dead = null; sc_player me = null;
        foreach (var s in Object.FindObjectsByType<sc_spawner>(FindObjectsSortMode.None)) { var nv = s.GetComponent<NetworkView>(); if (nv != null && nv.isMine) sp = s; }
        foreach (var p in Object.FindObjectsByType<sc_player>(FindObjectsSortMode.None)) { var nv = p.GetComponent<NetworkView>(); if (nv != null && nv.isMine) me = p; }
        foreach (var d in Object.FindObjectsByType<sc_deadplayer>(FindObjectsSortMode.None)) { var nv = d.GetComponent<NetworkView>(); if (nv != null && nv.isMine) dead = d; }
        return me;
    }

    // Only overwrites what still exists (when the host leaves, its objects may already be gone): the last good value is kept.
    static void Snapshot()
    {
        var md = Object.FindFirstObjectByType<sc_matchdata>();
        var mc = Object.FindFirstObjectByType<sc_matchcontroller>();
        if (md != null)
        {
            sWave = mc != null && Network.isServer ? mc.wave : md.wave; sMin = md.minutes; sSec = md.seconds; sKilled = md.zombieskilled; sStarted = md.isstarted;
            if (md.isstarted) sTimed = md.waveleft >= 0 ? 1 : 0;
        }
        int n = Object.FindObjectsByType<sc_spawner>(FindObjectsSortMode.None).Length; // one spawner object per connected player (host included)
        if (n > 0) sPlayers = n;
        sc_spawner sp; sc_deadplayer dead;
        var me = Mine(out sp, out dead);
        if (sp != null) sKills = sp.kills;
        if (me != null && me.item != null)
        {
            var life = me.GetComponent<sc_life>();
            if (life != null && life.quantia > 0f)
            {
                Copy(me.item, ref sItem); Copy(me.quant, ref sQuant); Copy(me.ammo, ref sAmmo);
                sSel = me.itemselected; sLifes = me.lifes; sLife = life.quantia; sHasPlayer = true;
            }
        }
        else if (dead != null && dead.invent != null)
        {
            if (dead.died) sHasPlayer = false; // out of revives: the items lie on the ground; a fresh avatar with the normal loadout
            else { Copy(dead.invent, ref sItem); Copy(dead.quant, ref sQuant); Copy(dead.ammo, ref sAmmo); sSel = 0; sLifes = dead.lifes; sLife = -1f; sHasPlayer = true; }
        }
    }
    static void Copy(int[] src, ref int[] dst) { if (src == null) { dst = null; return; } if (dst == null || dst.Length != src.Length) dst = new int[src.Length]; System.Array.Copy(src, dst, src.Length); }

    // host: tell the others the room's rules (clients cannot know the loot rule otherwise), unbuffered, every 2 s
    static float nextRoom;
    static void SendRoom()
    {
        roomFlags = (ZbMenu.TimedWaves() ? 1 : 0) | (ZbMenu.MoreLoot() ? 2 : 0);
        if (Time.unscaledTime < nextRoom || ZbNet.remoteCount == 0) return;
        nextRoom = Time.unscaledTime + 2f;
        var md = Object.FindFirstObjectByType<sc_matchdata>();
        if (md != null) md.NetView().RPC("zbroom", RPCMode.Others, roomFlags);
    }
    public static void OnRoom(int flags) { roomFlags = flags; } // sc_matchdata.zbroom

    // ZbMenu.HostLeftBank: the host left for good (reason 4), this peer was a client in a match
    public static void Freeze()
    {
        // No new snapshot here: the session is already reset (local id 0), so "mine" would now match the departed host's objects.
        // The periodic snapshot (at most 0.25 s old) is the last good state.
        if (active && !sStarted) { frozen = true; active = false; Debug.Log("[zbcarry] host left before the continued match started: keeping wave " + fWave); return; }
        fWave = sWave; fMin = sMin; fSec = sSec; fKilled = sKilled; fPlayers = sPlayers; fKills = sKills; fStarted = sStarted;
        fWaves = roomFlags >= 0 ? (roomFlags & 1) : sTimed; fLoot = roomFlags >= 0 ? ((roomFlags & 2) != 0 ? 1 : 0) : -1;
        fHasPlayer = sHasPlayer;
        if (sHasPlayer) { Copy(sItem, ref fItem); Copy(sQuant, ref fQuant); Copy(sAmmo, ref fAmmo); fSel = sSel; fLifes = sLifes; fLife = sLife; }
        frozen = true; active = false;
        Debug.Log("[zbcarry] host left: wave " + fWave + " started " + fStarted + " time " + fMin + ":" + fSec + " killed " + fKilled + " players " + fPlayers + " waves " + fWaves + " loot " + fLoot + " items " + (fHasPlayer && fItem != null ? string.Join(",", fItem) : "-"));
    }

    // RestartRoutine: the relay's last known wave (from the old host's C_META) fills in when this peer had none (e.g. joined a moment ago)
    public static void RelayInfo(int wave, bool started)
    {
        if (!frozen) return;
        if (fWave <= 0 && wave > 0) { fWave = wave; Debug.Log("[zbcarry] wave from the relay: " + wave); }
        if (started) fStarted = true;
    }

    // a continuation only makes sense for a match that was running
    public static bool Continues { get { return frozen && fStarted && fWave >= 1; } }

    // ZbMenu.Claim (new host) / the join of the new match (everybody else)
    public static void Begin(bool host)
    {
        ResetSnap();
        active = Continues; asHost = host; hostApplied = false; hostStarted = false; hostPlayerAt = -1f; startAt = -1f; doneAt = -1f;
        playerPending = active && fHasPlayer;
        wavesOverride = active ? fWaves : -1; lootOverride = active ? fLoot : -1;
        if (active) Debug.Log("[zbcarry] continuing at wave " + fWave + (host ? " as the new host" : " as a client"));
    }

    // the new host: prepare the match controller, wait for the players, start
    static void HostTick()
    {
        var mc = Object.FindFirstObjectByType<sc_matchcontroller>();
        var md = Object.FindFirstObjectByType<sc_matchdata>();
        if (mc == null || md == null) return;
        if (!hostApplied)
        {
            hostApplied = true;
            mc.wave = fWave - 1; md.wave = fWave - 1;       // the first wave step of the started match does wave++ and spawns wave fWave
            mc.minute = fMin; mc.second = fSec; md.zombieskilled = fKilled;
            mc.canlongwave = true;
            Debug.Log("[zbcarry] new host: match set to wave " + (fWave - 1) + " (+1 at start), time " + fMin + ":" + fSec + ", killed " + fKilled);
        }
        if (hostStarted || mc.gamestarted) { if (!hostStarted) { hostStarted = true; doneAt = Time.unscaledTime; } return; }
        if (GameObject.FindGameObjectsWithTag("Player").Length == 0) return; // the original ends a started match with nobody spawned
        if (hostPlayerAt < 0f) hostPlayerAt = Time.unscaledTime;
        int expected = Mathf.Max(1, fPlayers - 1);
        if (startAt < 0f && (ZbMenu.Players() >= expected || Time.unscaledTime - hostPlayerAt > 25f)) startAt = Time.unscaledTime + 3f;
        if (startAt > 0f && Time.unscaledTime >= startAt)
        {
            mc.gamestarted = true; hostStarted = true; doneAt = Time.unscaledTime;
            Debug.Log("[zbcarry] new host: match continues, players " + ZbMenu.Players() + " of " + expected);
        }
    }

    // sc_spawner, right after the new avatar was created and loaded with the normal loadout (before its Start runs)
    public static void OnSpawn(sc_spawner sp, sc_player p)
    {
        if (!playerPending || p == null) return;
        playerPending = false;
        if (fItem != null && p.item != null)
        {
            int n = Mathf.Min(p.item.Length, fItem.Length);
            for (int i = 0; i < n; i++) { p.item[i] = fItem[i]; if (fQuant != null && i < fQuant.Length) p.quant[i] = fQuant[i]; if (fAmmo != null && i < fAmmo.Length) p.ammo[i] = fAmmo[i]; }
            p.itemselected = Mathf.Clamp(fSel, 0, p.item.Length - 1); p.itemtoselect = p.itemselected;
        }
        p.lifes = fLifes;
        var life = p.GetComponent<sc_life>();
        if (life != null && fLife > 0f) life.quantia = fLife;
        if (fKills > 0 && sp != null) sp.NetView().RPC("zbkills", RPCMode.All, fKills);
        Debug.Log("[zbcarry] restored own player: items " + (fItem != null ? string.Join(",", fItem) : "-") + " life " + fLife + " revives " + fLifes + " kills " + fKills);
    }

    // one line for the "host left" screen
    public static string ScreenLine() { return Continues ? "The match continues at wave " + fWave + " with your items." : null; }

    // overlay line in the continued match (NetRunner.OnGUI), null when there is nothing to show
    public static string Line()
    {
        if (!active || !ZbNet.Connected) return null;
        var md = Object.FindFirstObjectByType<sc_matchdata>();
        bool started = asHost ? hostStarted : (md != null && md.isstarted);
        if (!started)
        {
            if (asHost && startAt > 0f) return "Host left - continuing at wave " + fWave + " in " + Mathf.Max(1, Mathf.CeilToInt(startAt - Time.unscaledTime));
            if (asHost) return "Host left - continuing at wave " + fWave + " - waiting for players (" + ZbMenu.Players() + "/" + Mathf.Max(1, fPlayers - 1) + ")";
            return "Host left - continuing at wave " + fWave;
        }
        if (doneAt < 0f) doneAt = Time.unscaledTime;
        return Time.unscaledTime - doneAt > 5f ? null : "Host left - continuing at wave " + fWave;
    }

    // overlay in the continued match (NetRunner.OnGUI)
    static GUIStyle st, sh;
    public static void OnGUI()
    {
        string t = Line();
        if (t == null) return;
        if (st == null) { st = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold }; st.normal.textColor = new Color(0.6f, 1f, 0.6f); sh = new GUIStyle(st); sh.normal.textColor = Color.black; }
        var r = new Rect(0, 140, Screen.width, 32);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), t, sh);
        GUI.Label(r, t, st);
    }

    // DebugDump
    public static string Json()
    {
        return "{\"active\":" + (active ? 1 : 0) + ",\"host\":" + (asHost ? 1 : 0) + ",\"frozen\":" + (frozen ? 1 : 0) + ",\"wave\":" + fWave + ",\"players\":" + fPlayers
            + ",\"waves\":" + fWaves + ",\"loot\":" + fLoot + ",\"room\":" + roomFlags + ",\"started\":" + (hostStarted ? 1 : 0) + ",\"pending\":" + (playerPending ? 1 : 0) + ",\"line\":\"" + (Line() ?? "") + "\"}";
    }
}
