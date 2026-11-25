using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.UI;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class PracticeProgressTests
    {
        [Test]
        public void PracticesConcreteActionsAndRejectsIncompleteInventoryReview()
        {
            var practice = new PracticeProgress();
            practice.ObserveMovement(Vector2.zero, new Vector2(.79f, 0));
            Assert.That(practice.Stage, Is.EqualTo(PracticeStage.Movement));
            practice.ObserveMovement(Vector2.zero, Vector2.right);
            Assert.That(practice.Stage, Is.EqualTo(PracticeStage.Pickup));
            practice.CollectFlashlight();
            practice.OpenInventory();
            practice.ExamineItem();
            practice.CloseInventory();
            Assert.That(practice.Stage, Is.EqualTo(PracticeStage.Inventory));
            practice.OpenInventory();
            practice.SelectItem();
            practice.CloseInventory();
            Assert.That(practice.Stage, Is.EqualTo(PracticeStage.Flashlight));
            practice.ToggleFlashlight(false);
            Assert.That(practice.Stage, Is.EqualTo(PracticeStage.Flashlight));
            practice.ToggleFlashlight(true);
            practice.ToggleFlashlight(false);
            Assert.That(practice.Stage, Is.EqualTo(PracticeStage.Cans));
            practice.HitRobot(true, false);
            practice.HitRobot(false, true);
            Assert.That(practice.Stage, Is.EqualTo(PracticeStage.Cans));
            practice.HitRobot(true, true);
            Assert.That(practice.Stage, Is.EqualTo(PracticeStage.Instructions));
            practice.ConfirmBriefing();
            Assert.That(practice.Stage, Is.EqualTo(PracticeStage.Exit));
        }

        [Test]
        public void PracticeInventoryIsDiscardedWhenStartingCampaign()
        {
            var owner = new GameObject("Sesión de prueba");
            try
            {
                var session = owner.AddComponent<GameSessionController>();
                session.StartPractice();
                session.State.AddItem("soda");
                session.State.LoseLife();
                session.StartNewGame();
                Assert.That(session.IsPractice, Is.False);
                Assert.That(session.State.HasItem("soda"), Is.False);
                Assert.That(session.State.Lives, Is.EqualTo(5));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void PracticeSceneHasAValidBuildEntry()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ZoneCatalog>("Assets/_Project/Data/Zones/ZoneCatalog.asset");
            Assert.That(catalog.TryResolveScene(EntranceTutorialController.PracticeZoneId, out var path, out _), Is.True);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null);
            Assert.That(System.Array.Exists(EditorBuildSettings.scenes, entry => entry.enabled && entry.path == path), Is.True);
        }
    }
}
