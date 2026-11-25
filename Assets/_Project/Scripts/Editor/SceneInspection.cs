using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EscapeUNIFRANZ.EditorTools
{
    public sealed class SceneInspection : IDisposable
    {
        private readonly SceneSetup[] previousSetup;
        private readonly Scene untitledScene;

        public Scene Scene { get; }

        private SceneInspection(Scene scene, SceneSetup[] previousSetup, Scene untitledScene)
        {
            Scene = scene;
            this.previousSetup = previousSetup;
            this.untitledScene = untitledScene;
        }

        public static SceneInspection Open(string path)
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Exit the prefab stage before inspecting a scene.");

            // URP registers global 2D lights by layer and blend style across open scenes.
            // Keep the Test Runner's untitled scene only when it has no competing light.
            var activeScene = SceneManager.GetActiveScene();
            if (SceneManager.sceneCount == 1 && string.IsNullOrEmpty(activeScene.path)
                && !activeScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Light2D>())
                    .Any(light => light.isActiveAndEnabled && light.lightType == Light2D.LightType.Global))
            {
                var inspectedScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(inspectedScene);
                return new SceneInspection(inspectedScene, null, activeScene);
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty || string.IsNullOrEmpty(scene.path))
                    throw new InvalidOperationException("Save the open scenes before inspecting another scene.");
            }

            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                return new SceneInspection(EditorSceneManager.OpenScene(path, OpenSceneMode.Single), setup, default);
            }
            catch
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
                throw;
            }
        }

        public void Dispose()
        {
            if (untitledScene.IsValid())
            {
                SceneManager.SetActiveScene(untitledScene);
                EditorSceneManager.CloseScene(Scene, true);
                return;
            }

            if (Scene.isDirty && previousSetup.Any(scene => scene.path == Scene.path))
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }
    }
}
