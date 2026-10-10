// Playtest feature: teammates' health. Every player sends its own health (the owner knows it best) a few times a second to the others
// (sc_matchdata.synchealth, unbuffered RPC); everybody shows a compact list (name + health bar) at the left edge and a small bar above the
// head of every teammate that is on screen. The roster comes from the players' spawner objects (they exist on every peer for as long as the
// player is connected), so a player that left disappears and one that died is shown as "down".
using System.Collections.Generic;
using UnityEngine;

public static class ZbTeam
{
    static readonly Dictionary<int, int> hp = new Dictionary<int, int>();
    static readonly Dictionary<int, float> stamp = new Dictionary<int, float>();
    static float nextSend;
    static Texture2D px;
    static GUIStyle label;

    public static void Set(int owner, int v) { hp[owner] = v; stamp[owner] = Time.unscaledTime; }
    public static void Reset() { hp.Clear(); stamp.Clear(); }

    // NetRunner.Update: send my own health (twice a second, only in an online match)
    public static void Tick()
    {
        if (!ZbNet.Connected || Time.unscaledTime < nextSend) return;
        nextSend = Time.unscaledTime + 0.5f;
        var md = Object.FindFirstObjectByType<sc_matchdata>();
        if (md == null) return;
        foreach (var p in Object.FindObjectsByType<sc_player>(FindObjectsSortMode.None))
        {
            var nv = p.GetComponent<NetworkView>();
            if (nv == null || !nv.isMine) continue;
            var life = p.GetComponent<sc_life>();
            if (life == null) continue;
            int v = Mathf.Clamp(Mathf.RoundToInt(life.quantia), 0, 100);
            Set(ZbNet.localId, v);
            md.NetView().RPC("synchealth", RPCMode.Others, ZbNet.localId, v);
        }
    }

    public struct Mate { public int id; public string name; public int hp; public bool dead; public sc_player avatar; }
    static readonly List<Mate> mates = new List<Mate>();

    // teammates (everybody but me) with their health; hp < 0 = unknown yet, dead = no avatar in the scene (died or has not spawned)
    public static List<Mate> Mates()
    {
        mates.Clear();
        if (!ZbNet.Connected) return mates;
        var avatars = new Dictionary<int, sc_player>();
        foreach (var p in Object.FindObjectsByType<sc_player>(FindObjectsSortMode.None)) { var nv = p.GetComponent<NetworkView>(); if (nv != null) avatars[nv.ownerId] = p; }
        foreach (var sp in Object.FindObjectsByType<sc_spawner>(FindObjectsSortMode.None))
        {
            var nv = sp.GetComponent<NetworkView>();
            if (nv == null || nv.isMine) continue;
            int id = nv.ownerId; sc_player av; avatars.TryGetValue(id, out av);
            int v = -1; int h;
            if (hp.TryGetValue(id, out h)) v = h;
            else if (av != null) { var life = av.GetComponent<sc_life>(); if (life != null) v = Mathf.Clamp(Mathf.RoundToInt(life.quantia), 0, 100); }
            bool dead = av == null || v == 0;
            mates.Add(new Mate { id = id, name = string.IsNullOrEmpty(sp.playername) ? "player" : sp.playername, hp = dead ? 0 : v, dead = dead, avatar = av });
        }
        mates.Sort((a, b) => a.id.CompareTo(b.id));
        return mates;
    }

    static void Bar(Rect r, float f, bool dead)
    {
        if (px == null) { px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply(); }
        var old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.65f); GUI.DrawTexture(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), px);
        if (!dead) { GUI.color = Color.Lerp(new Color(0.85f, 0.15f, 0.1f), new Color(0.35f, 0.8f, 0.2f), Mathf.Clamp01(f)); GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(f), r.height), px); }
        GUI.color = old;
    }

    // NetRunner.OnGUI (only while a match is running: gamestate 3 / 6)
    public static void OnGUI(sc_gamecontrol g)
    {
        var list = Mates();
        if (list.Count == 0) return;
        var md = Object.FindFirstObjectByType<sc_matchdata>();
        if (label == null || label.font == null)
        {
            var src = md != null && md.chatskin != null ? md.chatskin.label : GUI.skin.label;
            label = new GUIStyle(src) { fontSize = 13, alignment = TextAnchor.MiddleLeft, wordWrap = false };
        }
        var old = GUI.color; int depth = GUI.depth; GUI.depth = -900;
        // list: left edge below the chat lines, one row per teammate
        float y = Screen.height * 0.36f;
        foreach (var m in list)
        {
            GUI.color = m.dead ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
            GUI.Label(new Rect(12, y, 110, 20), m.name, label);
            Bar(new Rect(124, y + 5, 90, 10), m.hp < 0 ? 1f : m.hp / 100f, m.dead);
            GUI.color = m.dead ? new Color(1f, 0.4f, 0.4f, 0.9f) : Color.white;
            GUI.Label(new Rect(220, y, 70, 20), m.dead ? "down" : (m.hp < 0 ? "..." : m.hp.ToString()), label);
            y += 24f;
        }
        // above the heads of teammates that are on screen
        var cam = Camera.main;
        if (cam != null)
            foreach (var m in list)
            {
                if (m.dead || m.avatar == null) continue;
                var wp = m.avatar.transform.position + Vector3.up * 2.15f;
                var sp = cam.WorldToScreenPoint(wp);
                if (sp.z <= 0.5f || sp.z > 60f || sp.x < 0 || sp.x > Screen.width || sp.y < 0 || sp.y > Screen.height) continue;
                float w = Mathf.Clamp(900f / sp.z, 34f, 70f);
                float gy = Screen.height - sp.y;
                GUI.color = Color.white;
                GUI.Label(new Rect(sp.x - w, gy - 20, 2 * w, 16), m.name, new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontSize = 11 });
                Bar(new Rect(sp.x - w / 2, gy - 4, w, 4), m.hp < 0 ? 1f : m.hp / 100f, false);
            }
        GUI.color = old; GUI.depth = depth;
    }

    public static string Json()
    {
        var sb = new System.Text.StringBuilder("[");
        var l = Mates();
        for (int i = 0; i < l.Count; i++) sb.Append(i > 0 ? "," : "").Append("{\"id\":").Append(l[i].id).Append(",\"name\":\"").Append(l[i].name.Replace("\"", "")).Append("\",\"hp\":").Append(l[i].hp).Append(",\"dead\":").Append(l[i].dead ? 1 : 0).Append(",\"head\":").Append(l[i].avatar != null ? 1 : 0).Append("}");
        return sb.Append("]").ToString();
    }
}
