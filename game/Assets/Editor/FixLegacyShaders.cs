// One-shot migration helper (already run): the ripped project contained "dummy" copies of Unity 4's built-in
// shaders (Transparent/Diffuse, Specular, ...). Unity 6 ships the same shaders under "Legacy Shaders/...",
// so we re-point every material at the real built-in and delete the dummies.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class FixLegacyShaders
{
    static readonly Dictionary<string,string> Map = new Dictionary<string,string>
    {
        {"Specular","Legacy Shaders/Specular"},
        {"Transparent/Cutout/Diffuse","Legacy Shaders/Transparent/Cutout/Diffuse"},
        {"Transparent/Cutout/VertexLit","Legacy Shaders/Transparent/Cutout/VertexLit"},
        {"Transparent/VertexLit","Legacy Shaders/Transparent/VertexLit"},
        {"Transparent/Diffuse","Legacy Shaders/Transparent/Diffuse"},
        {"Particles/Additive","Legacy Shaders/Particles/Additive"},
        {"Particles/Alpha Blended","Legacy Shaders/Particles/Alpha Blended"},
        {"Self-Illumin/Diffuse","Legacy Shaders/Self-Illumin/Diffuse"},
        {"Self-Illumin/VertexLit","Legacy Shaders/Self-Illumin/VertexLit"},
        {"RenderFX/Skybox","Skybox/6 Sided"},
        {"RenderFX/Skybox Blended","Skybox/6 Sided"},
    };
    public static void Run()
    {
        foreach (var g in AssetDatabase.FindAssets("t:Material", new[]{"Assets"}))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m.shader == null) continue;
            string target;
            if (Map.TryGetValue(m.shader.name, out target))
            {
                var s = Shader.Find(target);
                if (s == null) { Debug.LogError("FIXSHADER missing builtin " + target); continue; }
                Debug.Log("FIXSHADER " + p + " " + m.shader.name + " -> " + target);
                m.shader = s;
                EditorUtility.SetDirty(m);
            }
        }
        AssetDatabase.SaveAssets();
        // delete dummy shader copies (anything left under Assets/Shader that is not used by terrain)
        foreach (var g in AssetDatabase.FindAssets("t:Shader", new[]{"Assets/Shader"}))
            if (Map.ContainsKey(ShaderName(AssetDatabase.GUIDToAssetPath(g)))) AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(g));
        AssetDatabase.SaveAssets();
    }
    static string ShaderName(string path) { var s = AssetDatabase.LoadAssetAtPath<Shader>(path); return s != null ? s.name : ""; }
}
