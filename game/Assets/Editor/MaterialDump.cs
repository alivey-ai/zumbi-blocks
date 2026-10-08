using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
public static class MaterialDump
{
    public static void Run()
    {
        var sb = new StringBuilder(); var counts = new Dictionary<string,int>();
        foreach (var g in AssetDatabase.FindAssets("t:Material", new[]{"Assets"}))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            string n = m.shader != null ? m.shader.name : "<null>";
            counts[n] = counts.ContainsKey(n) ? counts[n]+1 : 1;
        }
        foreach (var kv in counts) sb.AppendLine("SHADER " + kv.Value + " " + kv.Key);
        Debug.Log(sb.ToString());
    }
}
