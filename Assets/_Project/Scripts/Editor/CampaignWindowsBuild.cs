using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EscapeUNIFRANZ.EditorTools
{
    public static class CampaignWindowsBuild
    {
        [MenuItem("Escape UNIFRANZ/Campaña/Compilar Windows x64")]
        public static void Build() => BuildPlayer("Builds/Windows/EscapeUNIFRANZ.exe", BuildOptions.None);

        [MenuItem("Escape UNIFRANZ/Campaña/Compilar diagnóstico Windows x64")]
        public static void BuildDiagnostics() => BuildPlayer("Builds/Diagnostics/After/EscapeUNIFRANZ.exe",
            BuildOptions.Development | BuildOptions.ConnectWithProfiler);

        internal static void BuildBaseline() => BuildPlayer("Builds/Diagnostics/Before/EscapeUNIFRANZ.exe",
            BuildOptions.Development | BuildOptions.ConnectWithProfiler);

        private static void BuildPlayer(string location, BuildOptions flags)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(location));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = location,
                target = BuildTarget.StandaloneWindows64,
                options = flags | BuildOptions.CompressWithLz4
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build de Windows: " + report.summary.result);
            Debug.Log($"Build Windows x64: {Path.GetFullPath(location)} ({report.summary.totalSize} bytes).");
        }
    }
}
