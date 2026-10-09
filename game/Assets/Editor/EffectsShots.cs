// Editor tool: renders particle effect prefabs to PNG strips (frames over time) for docs/img/effects-*.png.
//   Unity -batchmode -projectPath game -executeMethod EffectsShots.Run -logFile x.log
// Env ZB_FX = comma list of prefab names (default: all effect prefabs), ZB_FX_OUT = output dir.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EffectsShots
{
    static readonly string[] Default = { "muzle rifle", "Rflare_out", "shotRPG", "explosion", "explosionbig", "spark", "woodspark", "stdpart", "blood_dense", "bloodspill", "bloodspillcone", "bloodspillhard", "redflare", "muzle snipernopoof" };

    public static void Run()
    {
        string outDir = Environment.GetEnvironmentVariable("ZB_FX_OUT") ?? "../docs/img";
        string list = Environment.GetEnvironmentVariable("ZB_FX");
        var names = string.IsNullOrEmpty(list) ? Default : list.Split(',');
        Directory.CreateDirectory(outDir);
        foreach (var n in names)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/" + n + ".prefab");
            if (prefab == null) { Debug.LogError("FX missing " + n); continue; }
            string times = Environment.GetEnvironmentVariable("ZB_FX_T") ?? (n.StartsWith("muzle") ? "0.01,0.02,0.04,0.06,0.08,0.1,0.15" : prefab.GetComponentInChildren<ParticleSystem>(true).main.loop ? "0.3,0.7,1.2,2,3,4,5.5" : "0.03,0.1,0.25,0.5,0.9,1.5,2.5");
            var ts = new List<float>(); foreach (var s in times.Split(',')) ts.Add(float.Parse(s, System.Globalization.CultureInfo.InvariantCulture));
            Shoot(prefab, n, ts, outDir);
        }
    }

    static void Shoot(GameObject prefab, string n, List<float> ts, string outDir)
    {
        const int W = 256, H = 256;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
        var camGo = new GameObject("cam"); var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.12f, 0.13f, 0.15f);
        cam.transform.position = new Vector3(-2.6f, 1.4f, -2.6f); cam.transform.LookAt(new Vector3(0, 0.6f, 0.8f));
        cam.fieldOfView = 50; cam.nearClipPlane = 0.05f;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.transform.position = Vector3.zero; floor.transform.localScale = new Vector3(2, 1, 2); floor.GetComponent<Renderer>().sharedMaterial.color = new Color(0.2f, 0.2f, 0.2f);
        var systems = go.GetComponentsInChildren<ParticleSystem>(true);
        var sb = new System.Text.StringBuilder("FXINFO " + n + " systems=" + systems.Length);
        foreach (var ps in systems)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            var m = r != null ? r.sharedMaterial : null;
            sb.Append(" [" + ps.name + " dur=" + ps.main.duration + " loop=" + ps.main.loop + " mat=" + (m != null ? m.name + "/" + (m.shader != null ? m.shader.name : "null") : "null") + "]");
        }
        Debug.Log(sb.ToString());
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt;
        var strip = new Texture2D(W * ts.Count, H, TextureFormat.RGB24, false);
        float tPrev = 0;
        foreach (var ps in systems) ps.Simulate(0, false, true, true);
        for (int i = 0; i < ts.Count; i++)
        {
            float dt = ts[i] - tPrev; tPrev = ts[i];
            foreach (var ps in systems) ps.Simulate(dt, false, false, true);
            cam.Render();
            RenderTexture.active = rt;
            strip.ReadPixels(new Rect(0, 0, W, H), W * i, 0);
        }
        RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(outDir, "effects-" + n.Replace(' ', '_') + ".png"), strip.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(camGo); UnityEngine.Object.DestroyImmediate(floor);
        UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(strip);
    }
}
