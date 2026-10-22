using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace EscapeUNIFRANZ.Editor
{
    public static class AndroidBuildConfiguration
    {
        public const string ApkPath = "Builds/Android/EscapeUNIFRANZ.apk";

        [MenuItem("EscapeUNIFRANZ/Build/Configure Android APK")]
        public static void Configure()
        {
            string[] scenes = {
                "Assets/_Project/Scenes/UI/MainMenu.unity",
                "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity",
                "Assets/_Project/Scenes/Zones/Z00_Entrada.unity",
                "Assets/_Project/Scenes/Zones/Z01_Hall.unity",
                "Assets/_Project/Scenes/Zones/Z02_ARCA.unity",
                "Assets/_Project/Scenes/Zones/Z03_Piso2.unity",
                "Assets/_Project/Scenes/Zones/Z04_Piso3.unity",
                "Assets/_Project/Scenes/Zones/Z05_LabSoftware.unity",
                "Assets/_Project/Scenes/Zones/Z06_NucleoIA.unity"
            };
            foreach (string scene in scenes)
                if (!File.Exists(scene)) throw new FileNotFoundException("Missing build scene", scene);
            EditorBuildSettings.scenes = scenes.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();
            PlayerSettings.productName = "EscapeUNIFRANZ";
            PlayerSettings.companyName = "UNIFRANZ";
            PlayerSettings.bundleVersion = "1.0.1";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.unifranz.escape");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = 2;
            PlayerSettings.Android.splitApplicationBinary = false;
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.development = false;
            AssetDatabase.SaveAssets();
            Debug.Log("Android APK configured: ARM64, landscape, IL2CPP, nine scenes.");
        }

        [MenuItem("EscapeUNIFRANZ/Build/Build Android APK")]
        public static void Build()
        {
            Configure();
            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                target = BuildTarget.Android,
                locationPathName = ApkPath,
                options = BuildOptions.CompressWithLz4
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Android build failed: " + report.summary.result);
            Debug.Log("APK generated: " + Path.GetFullPath(ApkPath));
        }
    }
}
