using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Driftlight.EditorTools
{
    /// <summary>
    /// One-click builds. Menu: DRIFTLIGHT > Build macOS App / Build Windows App.
    /// Batch: -executeMethod Driftlight.EditorTools.DriftlightBuild.BuildMac
    /// Builds go to Builds/ (ignored by git).
    /// </summary>
    public static class DriftlightBuild
    {
        [MenuItem("DRIFTLIGHT/Build macOS App", priority = 100)]
        public static void BuildMac() => Build(BuildTarget.StandaloneOSX, "Builds/macOS/DRIFTLIGHT.app");

        [MenuItem("DRIFTLIGHT/Build Windows App", priority = 101)]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/DRIFTLIGHT.exe");

        static void Build(BuildTarget target, string path)
        {
            PlayerSettings.runInBackground = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = target,
                options = BuildOptions.None,
            });

            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
                throw new System.Exception($"[DRIFTLIGHT] Build failed: {summary.result} ({summary.totalErrors} errors)");
            Debug.Log($"[DRIFTLIGHT] Built {path} ({summary.totalSize / (1024 * 1024)} MB) in {summary.totalTime.TotalSeconds:0}s");
        }
    }
}
