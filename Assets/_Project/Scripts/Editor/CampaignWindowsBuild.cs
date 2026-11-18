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
        public static void Build()
        {
            const string location = "Builds/Windows/EscapeUNIFRANZ.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(location));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = location,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CompressWithLz4
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build de Windows: " + report.summary.result);
            Debug.Log($"Build Windows x64: {Path.GetFullPath(location)} ({report.summary.totalSize} bytes).");
        }
    }
}
