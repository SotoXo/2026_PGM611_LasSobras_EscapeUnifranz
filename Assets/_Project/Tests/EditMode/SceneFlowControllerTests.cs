using System.Reflection;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using NUnit.Framework;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class SceneFlowControllerTests
    {
        private GameObject firstObject;
        private GameObject secondObject;

        [TearDown]
        public void TearDown()
        {
            if (firstObject != null)
            {
                Object.DestroyImmediate(firstObject);
            }

            if (secondObject != null)
            {
                Object.DestroyImmediate(secondObject);
            }
        }

        [Test]
        public void ResolveCheckpointEncounter_SelectsMatchingResetBoundary()
        {
            EncounterContext entrance = CreateEncounter("entrada_inicio", out firstObject);
            EncounterContext boss = CreateEncounter("francis_inicio", out secondObject);

            EncounterContext resolved = SceneFlowController.ResolveCheckpointEncounter(
                new[] { entrance, boss },
                "francis_inicio");

            Assert.That(resolved, Is.SameAs(boss));
        }

        [Test]
        public void ResolveCheckpointEncounter_IgnoresCompletedEncounter()
        {
            EncounterContext completed = CreateEncounter("piso2_antes_exoleg", out firstObject);
            SetAutoProperty(completed, "IsCompleted", true);

            EncounterContext resolved = SceneFlowController.ResolveCheckpointEncounter(
                new[] { completed },
                "piso2_antes_exoleg");

            Assert.That(resolved, Is.Null);
        }

        [Test]
        public void ResolveCheckpointEncounter_UsesActiveEncounterForPhaseCheckpoint()
        {
            EncounterContext francis = CreateEncounter("francis_inicio", out firstObject);
            SetAutoProperty(francis, "IsActive", true);

            EncounterContext resolved = SceneFlowController.ResolveCheckpointEncounter(
                new[] { francis },
                "francis_fase3");

            Assert.That(resolved, Is.SameAs(francis));
        }

        [Test]
        public void ResolveCheckpointEncounter_UsesSoleInactiveBoundaryForRepeatedPhaseRespawn()
        {
            EncounterContext francis = CreateEncounter("francis_inicio", out firstObject);

            EncounterContext resolved = SceneFlowController.ResolveCheckpointEncounter(
                new[] { francis },
                "francis_fase3");

            Assert.That(resolved, Is.SameAs(francis));
        }

        private static EncounterContext CreateEncounter(
            string checkpointSpawnId,
            out GameObject gameObject)
        {
            gameObject = new GameObject("EncounterTest");
            EncounterContext encounter = gameObject.AddComponent<EncounterContext>();
            FieldInfo field = typeof(EncounterContext).GetField(
                "checkpointSpawnId",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(encounter, checkpointSpawnId);
            return encounter;
        }

        private static void SetAutoProperty(
            EncounterContext encounter,
            string propertyName,
            bool value)
        {
            FieldInfo field = typeof(EncounterContext).GetField(
                $"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(encounter, value);
        }
    }
}
