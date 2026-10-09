using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Headless WebGL build:
///   Unity -batchmode -quit -projectPath game -executeMethod BuildWebGL.Build -logFile -
/// Output: game/Build/WebGL (gitignored). Gzip compressed (*.gz): must be served with Content-Encoding: gzip
/// (server/src/index.js does that; a plain static server will not work).
/// </summary>
public static class BuildWebGL
{
    /// <summary>Development build with full stack traces and no compression: Build/WebGL-debug (serve with a plain static server).</summary>
    public static void BuildDebug() { Build(true); }

    public static void Build() { Build(false); }

    static void Build(bool debug)
    {
        ApplySettings();
        if (debug)
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
        }
        ZbNetPrefabsBuilder.Rebuild();
        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build", debug ? "WebGL-debug" : "WebGL"));
        if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        Directory.CreateDirectory(outDir);

        var scenes = new System.Collections.Generic.List<string>();
        foreach (var s in EditorBuildSettings.scenes) if (s.enabled) scenes.Add(s.path);

        var opts = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            options = debug ? BuildOptions.Development : BuildOptions.None
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
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;   // wasm is 35 MB raw; Workers assets cap files at 25 MiB. Gzip (not Brotli): Chrome only decodes br over https
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching = false;
        PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.WebGL, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSpeed);
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.template = "PROJECT:ZumbiBlocks";
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
