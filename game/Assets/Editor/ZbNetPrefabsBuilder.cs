using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Regenerates Assets/Resources/ZbNetPrefabs.asset: every prefab asset in the project, sorted by path.</summary>
public static class ZbNetPrefabsBuilder
{
    const string AssetPath = "Assets/Resources/ZbNetPrefabs.asset";

    [MenuItem("Zumbi/Rebuild net prefab table")]
    public static void Rebuild()
    {
        Directory.CreateDirectory("Assets/Resources");
        var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !p.Contains("/Editor/"))
            .OrderBy(p => p, System.StringComparer.Ordinal)
            .ToList();
        var list = new List<GameObject>();
        foreach (var p in paths)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go != null) list.Add(go);
        }
        var table = AssetDatabase.LoadAssetAtPath<ZbNetPrefabs>(AssetPath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<ZbNetPrefabs>();
            AssetDatabase.CreateAsset(table, AssetPath);
        }
        table.prefabs = list.ToArray();
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        Debug.Log("ZbNetPrefabs: " + list.Count + " prefabs");
    }
}
