using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Headless WebGL build:
///   Unity -batchmode -quit -projectPath game -executeMethod BuildWebGL.Build -logFile -
/// Output: game/Build/WebGL (gitignored). Compression is disabled so the folder can be served from any static host.
/// </summary>
public static class BuildWebGL
{
    public static void Build()
    {
        ApplySettings();
        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build", "WebGL"));
        if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        Directory.CreateDirectory(outDir);

        var scenes = new System.Collections.Generic.List<string>();
        foreach (var s in EditorBuildSettings.scenes) if (s.enabled) scenes.Add(s.path);

        var opts = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        BuildReport rep = BuildPipeline.BuildPlayer(opts);
        Debug.Log("BUILDRESULT " + rep.summary.result + " size=" + rep.summary.totalSize + " errors=" + rep.summary.totalErrors);
        if (rep.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    public static void ApplySettings()
    {
        PlayerSettings.companyName = "AdrianKS (revival)";
        PlayerSettings.productName = "Zumbi Blocks";
        PlayerSettings.colorSpace = ColorSpace.Gamma;               // Unity 4 era: gamma rendering
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.runInBackground = true;                      // original set Application.runInBackground
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching = false;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        PlayerSettings.WebGL.initialMemorySize = 256;
        PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.stripEngineCode = false;                      // legacy code uses reflection (RPC shim)
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);
        PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ApiCompatibilityLevel.NET_Standard);
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        AssetDatabase.SaveAssets();
    }
}
