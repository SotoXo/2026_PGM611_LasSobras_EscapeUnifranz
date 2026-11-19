using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EscapeUNIFRANZ.UI;

namespace EscapeUNIFRANZ.EditorTools
{
    public static class CampaignPerformanceAudit
    {
        public static void Baseline()
        {
            Snapshot("before");
            CampaignWindowsBuild.BuildBaseline();
        }

        public static void FinalSnapshot() => Snapshot("after");

        public static void RenderMenuPreviews()
        {
            var scene = EditorSceneManager.OpenScene(CampaignReleaseSetup.MainMenuPath, OpenSceneMode.Single);
            var camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>()).Single();
            var menu = scene.GetRootGameObjects().Select(g => g.GetComponent<MainMenuController>()).First(c => c != null);
            var canvas = menu.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            Directory.CreateDirectory("Logs");
            SavePreview(camera, 1920, 1080, "main-menu");
            SavePreview(camera, 1440, 1080, "main-menu");
            var fields = new SerializedObject(menu);
            ((GameObject)fields.FindProperty("mainPanel").objectReferenceValue).SetActive(false);
            var options = (GameObject)fields.FindProperty("optionsPanel").objectReferenceValue;
            options.SetActive(true);
            var controls = options.GetComponent<GameOptionsView>();
            string[] names = { "General", "Música", "Ambiente", "Efectos", "Interfaz", "Voces" };
            for (int i = 0; i < controls.volumeSliders.Length; i++)
            {
                controls.volumeSliders[i].value = i == 0 ? .8f : 1f;
                controls.volumeLabels[i].text = names[i] + (i == 0 ? "   80 %" : "   100 %");
            }
            controls.frameLimitLabel.text = "Frecuencia de pantalla";
            controls.frameLimit.interactable = false;
            SavePreview(camera, 1920, 1080, "options");
        }

        private static void SavePreview(Camera camera, int width, int height, string panel)
        {
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.aspect = (float)width / height;
                Canvas.ForceUpdateCanvases();
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera, request))
                    throw new InvalidOperationException("El renderizador no admite la captura del menú en EditMode.");
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes($"Logs/{panel}-{width}x{height}.png", image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static void Snapshot(string suffix)
        {
            var settings = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var output = new List<string>
            {
                $"Unity {Application.unityVersion}; active build target {EditorUserBuildSettings.activeBuildTarget}. Serialización en EditMode.",
                $"Graphics device: {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}"
            };
            try
            {
                foreach (var settingsScene in settings)
                {
                    var scene = EditorSceneManager.OpenScene(settingsScene.path, OpenSceneMode.Single);
                    var roots = scene.GetRootGameObjects();
                    T[] Active<T>() where T : Component => roots.SelectMany(g => g.GetComponentsInChildren<T>())
                        .Where(c => !(c is Behaviour b) || b.enabled).ToArray();
                    var lights = Active<Light2D>();
                    var composites = Active<CompositeCollider2D>();
                    var colliders = Active<Collider2D>();
                    var cameras = Active<Camera>();
                    output.Add($"{scene.name}: Camera={cameras.Length}, HDR={cameras.Count(c => c.allowHDR)}, Canvas={Active<Canvas>().Length}, " +
                        $"GraphicRaycaster={Active<GraphicRaycaster>().Length}, Graphics={Active<Graphic>().Length}, Particles={Active<ParticleSystem>().Length}, " +
                        $"Light2D={lights.Length}, shadow lights={lights.Count(l => l.shadowsEnabled)}, shadow casters={Active<ShadowCaster2D>().Length}, " +
                        $"Collider2D={colliders.Length}, composite inputs={colliders.Count(c => c.compositeOperation != Collider2D.CompositeOperation.None)}, " +
                        $"paths={composites.Sum(c => c.pathCount)}, AudioSource={Active<AudioSource>().Length}");
                    foreach (var camera in cameras)
                        if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var data))
                            output.Add($"  {camera.name}: post={data.renderPostProcessing}; MSAA={camera.allowMSAA}; ortho={camera.orthographicSize}");
                    var flicker = Active<HallwayFlicker>().FirstOrDefault();
                    if (flicker != null)
                    {
                        const string original = "Assets/_Project/Art/UI/MainMenu/Backgrounds/HallwayFlicker/";
                        for (int i = 1; i <= 4; i++)
                        {
                            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(original + $"MenuHallway_Light_{i:00}.png");
                            long oldBytes = (long)UnityEngine.Experimental.Rendering.GraphicsFormatUtility.ComputeMipmapSize(
                                source.width, source.height, source.graphicsFormat);
                            output.Add($"  Original {source.name}: {source.width}x{source.height}; {source.graphicsFormat}; base={oldBytes} bytes.");
                        }
                        var field = new SerializedObject(flicker).FindProperty("lightingStates");
                        for (int i = 0; i < field.arraySize; i++)
                        {
                            var sprite = (Sprite)field.GetArrayElementAtIndex(i).objectReferenceValue;
                            var texture = sprite.texture;
                            var path = AssetDatabase.GetAssetPath(texture);
                            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                            long bytes = (long)UnityEngine.Experimental.Rendering.GraphicsFormatUtility.ComputeMipmapSize(
                                texture.width, texture.height, texture.graphicsFormat);
                            output.Add($"  {texture.name}: source={width}x{height}; imported={texture.width}x{texture.height}; " +
                                $"{texture.graphicsFormat}, base={bytes} bytes; runtime object={Profiler.GetRuntimeMemorySizeLong(texture)} bytes (Editor); " +
                                $"readable={importer.isReadable}, mipmaps={importer.mipmapEnabled}, filter={importer.filterMode}");
                        }
                    }
                }
            }
            finally
            {
                if (setup.Any(scene => scene.isLoaded && scene.isActive && !string.IsNullOrEmpty(scene.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllLines($"Logs/performance-{suffix}.txt", output);
            Debug.Log(string.Join("\n", output));
        }
    }
}
