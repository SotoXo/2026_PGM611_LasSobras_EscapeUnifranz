using System.Linq;
using System.Reflection;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.EditorTools;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class FinalInterfaceTests
    {
        private const string Bootstrap = "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";

        [TestCase(1920, 1080, 1f)]
        [TestCase(1366, 768, 1f)]
        [TestCase(2560, 720, 1f)]
        [TestCase(780, 420, .777778f)]
        public void InventoryCardFitsAvailableArea(int width, int height, float expected)
        {
            var scale = SafeAreaPanel.FitScale(new Vector2(width, height), new Vector2(940, 540));
            Assert.That(scale, Is.EqualTo(expected).Within(.0001f));
        }

        [Test]
        public void BothCansHaveDistinctIconsAndRemainStackable()
        {
            const string items = "Assets/_Project/Data/Narrative/Items/";
            var full = AssetDatabase.LoadAssetAtPath<ItemData>(items + "soda.asset");
            var empty = AssetDatabase.LoadAssetAtPath<ItemData>(items + "empty_can.asset");
            Assert.That(full.icon, Is.Not.Null);
            Assert.That(empty.icon, Is.Not.Null);
            Assert.That(empty.icon, Is.Not.EqualTo(full.icon));
            Assert.That(empty.stackable && full.stackable, Is.True);
        }

        [Test]
        public void IntroPanelOffersTutorialAndFirstLevelAfterSkipping()
        {
            using var inspection = SceneInspection.Open(Bootstrap);
            var roots = inspection.Scene.GetRootGameObjects();
            var view = roots.SelectMany(root => root.GetComponentsInChildren<GameFlowView>(true)).Single();
            var controller = roots.SelectMany(root => root.GetComponentsInChildren<GameFlowController>(true)).Single();
            using var serialized = new SerializedObject(view);
            var next = (Button)serialized.FindProperty("introNextButton").objectReferenceValue;
            var skip = (Button)serialized.FindProperty("introSkipButton").objectReferenceValue;
            var render = typeof(GameFlowView).GetMethod("RenderIntro", BindingFlags.Instance | BindingFlags.NonPublic);

            view.Bind(controller);
            controller.NewGame();
            render.Invoke(view, new object[] { 0, 3, false });
            Assert.That(next.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo("CONTINUAR"));
            Assert.That(skip.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo("OMITIR INTRO"));

            skip.onClick.Invoke();
            Assert.That(controller.CurrentScreen, Is.EqualTo(PresentationScreen.StartChoice));
            render.Invoke(view, new object[] { 0, 3, true });
            Assert.That(next.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo("JUGAR TUTORIAL"));
            Assert.That(skip.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo("IR AL PRIMER NIVEL"));
            Assert.That(next.interactable && skip.interactable, Is.True);
        }

        [Test]
        public void PracticePauseOffersCampaignAndRestoresMapDuringCampaign()
        {
            using var inspection = SceneInspection.Open(Bootstrap);
            var roots = inspection.Scene.GetRootGameObjects();
            var view = roots.SelectMany(root => root.GetComponentsInChildren<GameFlowView>(true)).Single();
            var session = roots.SelectMany(root => root.GetComponentsInChildren<GameSessionController>(true)).Single();
            var flow = session.GetComponent<GameFlowController>();
            using var serialized = new SerializedObject(view);
            var map = (Button)serialized.FindProperty("pauseMapButton").objectReferenceValue;
            var inventory = (Button)serialized.FindProperty("pauseInventoryButton").objectReferenceValue;
            var refresh = typeof(GameFlowView).GetMethod("RefreshPauseActions", BindingFlags.Instance | BindingFlags.NonPublic);

            view.Bind(flow);
            session.StartPractice();
            refresh.Invoke(view, null);
            var exit = map.transform.parent.Find("PausePracticeExit").GetComponent<Button>();
            Assert.That(exit.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo("IR AL NIVEL 1"));
            Assert.That(exit.gameObject.activeSelf && exit.interactable, Is.True);
            Assert.That(inventory.navigation.selectOnDown, Is.SameAs(exit));
            Assert.That(map.gameObject.activeSelf, Is.False);

            session.StartNewGame();
            refresh.Invoke(view, null);
            Assert.That(exit.gameObject.activeSelf, Is.False);
            Assert.That(map.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void FlashlightInventoryShowsGameplayShortcutAndKeepsExamination()
        {
            const string path = "Assets/_Project/Data/Narrative/Items/flashlight.asset";
            using var inspection = SceneInspection.Open(Bootstrap);
            var view = inspection.Scene.GetRootGameObjects().SelectMany(root =>
                root.GetComponentsInChildren<NarrativePanelView>(true)).Single();
            var flashlight = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            var state = new GameState();
            state.AddItem(flashlight.id);
            using var serialized = new SerializedObject(view);
            var overlay = (GameObject)serialized.FindProperty("overlay").objectReferenceValue;

            view.ShowInventory(new[] { flashlight }, state, flashlight.id, _ => { }, () => { }, () => { }, () => { });
            var panel = overlay.transform.Find("Inventory");
            Assert.That(panel.Find("Description").GetComponent<TMP_Text>().text, Does.Contain("En juego, pulsa F."));
            Assert.That(panel.Find("State").GetComponent<TMP_Text>().text, Is.EqualTo("ENCENDIDA"));
            Assert.That(panel.Find("ToggleItem"), Is.Null);
            Assert.That(panel.Find("Examine").GetComponent<Button>().interactable, Is.True);
        }
    }
}
