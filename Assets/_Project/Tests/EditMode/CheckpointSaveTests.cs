using System;
using System.IO;
using EscapeUNIFRANZ.Core;
using NUnit.Framework;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class CheckpointSaveTests
    {
        private string directory;
        private string path;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "EscapeUNIFRANZ-Test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, "campaign.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
            Directory.Delete(directory);
        }

        [Test]
        public void RestoresFinalPhaseAndInventory()
        {
            var state = new GameState();
            state.SetLocation("nucleo_ia", "from_lab");
            state.SetCheckpoint("nucleo_ia", "francis_fase3");
            state.TryCollectPickup("torch_entry", "flashlight");
            state.AddItem("soda");
            state.AddItem("soda");
            state.SelectItem("soda");
            state.ToggleFlashlight();
            state.DiscoverWorldItem("clue", "core_confirm");
            state.DiscoverWorldItem("puzzle", "francis_network");
            state.RecordMapPoint("nucleo_ia", "console", "Consola", KnownPointKind.Panel, "Validado");
            state.SetFlag(GameFlagId.FrancisNetworkPhaseComplete);
            state.SetFlag(GameFlagId.FrancisHardwarePhaseComplete);
            Assert.That(CheckpointSave.TryWrite(path, state), Is.True);
            Assert.That(CheckpointSave.TryLoad(path, out var loaded), Is.True);
            Assert.That(loaded.CurrentSpawnId, Is.EqualTo("from_lab"));
            Assert.That(loaded.CheckpointSpawnId, Is.EqualTo("francis_fase3"));
            Assert.That(loaded.HasFlag(GameFlagId.FrancisHardwarePhaseComplete), Is.True);
            Assert.That(loaded.IsWorldItemDiscovered("clue", "core_confirm"), Is.True);
            Assert.That(loaded.IsWorldItemDiscovered("puzzle", "francis_network"), Is.True);
            Assert.That(loaded.KnownMapPoints[0].status, Is.EqualTo("Validado"));
            Assert.That(loaded.IsZoneDiscovered("nucleo_ia"), Is.True);
            Assert.That(loaded.CarriedItems, Is.EquivalentTo(new[] { "flashlight", "soda", "soda" }));
            Assert.That(loaded.SelectedItemId, Is.EqualTo("soda"));
            Assert.That(loaded.FlashlightOn, Is.False);
            Assert.That(loaded.TryCollectPickup("torch_entry", "flashlight"), Is.False);
        }

        [Test]
        public void InvalidPrimaryFallsBackToThePreviousCompleteSnapshot()
        {
            var state = new GameState();
            state.SetLocation("hall", "from_entrada");
            state.SetCheckpoint("hall", "hall_principal");
            Assert.That(CheckpointSave.TryWrite(path, state), Is.True);
            state.SetFlag(GameFlagId.ArcaAccessRestored);
            Assert.That(CheckpointSave.TryWrite(path, state), Is.True);
            File.WriteAllText(path, "{\"version\":923}");

            Assert.That(CheckpointSave.TryLoad(path, out var loaded), Is.True);
            Assert.That(loaded.CheckpointZoneId, Is.EqualTo("hall"));
            Assert.That(loaded.HasFlag(GameFlagId.ArcaAccessRestored), Is.False);
        }

        [Test]
        public void MissingCheckpointDoesNotCreateASave()
        {
            Assert.That(CheckpointSave.TryWrite(path, new GameState()), Is.False);
            Assert.That(CheckpointSave.TryLoad(path, out var state), Is.False);
            Assert.That(state, Is.Null);
        }

        [Test]
        public void ContinueLoadsWithoutRunningTheIntro()
        {
            var flow = new PresentationFlowState();
            Assert.That(flow.LoadSavedGame(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Loading));
            Assert.That(flow.CompleteLoading(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Gameplay));
        }
    }
}
