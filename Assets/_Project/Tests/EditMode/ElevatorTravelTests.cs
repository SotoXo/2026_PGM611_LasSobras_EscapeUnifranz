using System.Reflection;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Puzzles;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class ElevatorTravelTests
    {
        [Test]
        public void CircuitRequiresPieceAndRepairSurvivesRestoringSession()
        {
            var owner = new GameObject("Elevator");
            var runtime = new GameObject("Session");
            var texture = new Texture2D(2, 2);
            var panel = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
            try
            {
                var session = runtime.AddComponent<GameSessionController>(); session.StartNewGame();
                var exit = owner.AddComponent<ZoneExitInteractable>(); exit.elevatorPanel = panel; exit.Bind(null, session);
                var circuit = new ElevatorCircuit();
                int[] turns = { 0, 1, 2, 1, 1, 2, 0, 3, 1, 1, 0, 1, 0, 2, 1, 1 };
                for (int i = 0; i < turns.Length; i++) for (int j = 0; j < turns[i]; j++) circuit.Rotate(i);
                circuit.Install();
                typeof(ZoneExitInteractable).GetField("circuit", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(exit, circuit);
                Assert.That(exit.IsUnlocked, Is.False);
                Assert.That(exit.CompleteElevatorCircuit(), Is.False, "Repair cannot consume a missing inventory piece.");
                session.State.AddItem(ElevatorCircuit.ItemId);
                Assert.That(exit.CompleteElevatorCircuit(), Is.True);
                Assert.That(session.State.HasItem(ElevatorCircuit.ItemId), Is.False);
                Assert.That(session.State.HasFlag(GameFlagId.Piso2Unlocked), Is.True);
                Assert.That(exit.IsUnlocked, Is.True);
                Assert.That(exit.CompleteElevatorCircuit(), Is.False, "The piece and repair must not be applied twice.");
                var restored = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(session.State));
                session.RestoreState(restored);
                Assert.That(exit.IsUnlocked, Is.True, "A restored repaired circuit keeps the elevator available.");
            }
            finally
            {
                Object.DestroyImmediate(owner); Object.DestroyImmediate(runtime);
                Object.DestroyImmediate(panel); Object.DestroyImmediate(texture);
            }
        }
    }
}
