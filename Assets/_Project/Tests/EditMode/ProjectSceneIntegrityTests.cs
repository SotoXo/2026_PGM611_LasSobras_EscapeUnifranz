using System.Collections.Generic;
using System.Linq;
using EscapeUNIFRANZ.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class ProjectSceneIntegrityTests
    {
        [Test]
        public void AllScenesHaveValidReferencesAndUniqueGlobalLights()
        {
            var originalScene = SceneManager.GetActiveScene();
            var paths = FindProjectAssets("t:Scene").ToArray();
            Assert.That(paths, Is.Not.Empty);
            var issues = new List<string>();
            foreach (string path in paths)
            {
                using var inspection = SceneInspection.Open(path);
                var occupiedLayers = new HashSet<(int Layer, int BlendStyle)>();
                foreach (var root in inspection.Scene.GetRootGameObjects())
                {
                    InspectHierarchy(root, path, issues);
                    foreach (var light in root.GetComponentsInChildren<Light2D>())
                    {
                        if (!light.isActiveAndEnabled || light.lightType != Light2D.LightType.Global)
                            continue;

                        foreach (int layer in light.targetSortingLayers)
                        {
                            if (!occupiedLayers.Add((layer, light.blendStyleIndex)))
                                issues.Add($"{path} / {light.name}: duplicate global light on {SortingLayer.IDToName(layer)}, blend style {light.blendStyleIndex}");
                        }
                    }
                }
            }

            Assert.That(issues, Is.Empty, string.Join("\n", issues));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(originalScene));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void PrefabsAndScriptableAssetsHaveValidReferences()
        {
            var issues = new List<string>();
            foreach (string path in FindProjectAssets("t:Prefab"))
                InspectHierarchy(AssetDatabase.LoadAssetAtPath<GameObject>(path), path, issues);

            foreach (string path in FindProjectAssets("t:ScriptableObject"))
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path).OfType<ScriptableObject>())
                    InspectReferences(asset, path, issues);
            }

            Assert.That(issues, Is.Empty, string.Join("\n", issues));
        }

        [Test]
        public void BuildScenesResolveToTheirProjectAssets()
        {
            var enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            Assert.That(enabled, Is.Not.Empty);
            Assert.That(enabled.Select(scene => scene.path), Is.Unique);
            foreach (var scene in enabled)
            {
                Assert.That(AssetDatabase.GUIDToAssetPath(scene.guid), Is.EqualTo(scene.path));
                Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path), Is.Not.Null, scene.path);
            }
        }

        private static IEnumerable<string> FindProjectAssets(string filter) =>
            AssetDatabase.FindAssets(filter, new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !path.StartsWith("Assets/_Recovery/", System.StringComparison.Ordinal));

        private static void InspectHierarchy(GameObject root, string path, ICollection<string> issues)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                var obj = transform.gameObject;
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj) > 0)
                    issues.Add($"{path} / {obj.name}: missing script");
                if (PrefabUtility.IsPrefabAssetMissing(obj))
                    issues.Add($"{path} / {obj.name}: missing prefab asset");
                foreach (var component in obj.GetComponents<Component>())
                {
                    if (component != null)
                        InspectReferences(component, path, issues);
                }
            }
        }

        private static void InspectReferences(Object target, string path, ICollection<string> issues)
        {
            using var serialized = new SerializedObject(target);
            using var property = serialized.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference
                    && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                    issues.Add($"{path} / {target.name} ({target.GetType().Name}).{property.propertyPath}: missing reference");
            }
        }
    }
}
