using System.Linq;
using System.Reflection;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Hazards;
using NUnit.Framework;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class HazardLogicTests
    {
        [Test]
        public void PatrolLoop_WrapsToFirstPoint()
        {
            var route = new PatrolRouteState();

            Assert.That(route.Advance(3, false), Is.EqualTo(1));
            Assert.That(route.Advance(3, false), Is.EqualTo(2));
            Assert.That(route.Advance(3, false), Is.EqualTo(0));
        }

        [Test]
        public void PatrolPingPong_ReversesAtEnds()
        {
            var route = new PatrolRouteState();

            Assert.That(route.Advance(3, true), Is.EqualTo(1));
            Assert.That(route.Advance(3, true), Is.EqualTo(2));
            Assert.That(route.Advance(3, true), Is.EqualTo(1));
            Assert.That(route.Advance(3, true), Is.EqualTo(0));
        }

        [Test]
        public void Detection_InRange_ReturnsTrue()
        {
            Assert.That(DetectionSensor2D.IsWithinRange(Vector2.zero, new Vector2(3f, 4f), 5f),
                Is.True);
        }

        [Test]
        public void Detection_OutOfRange_ReturnsFalse()
        {
            Assert.That(DetectionSensor2D.IsWithinRange(Vector2.zero, new Vector2(3f, 4f), 4.9f),
                Is.False);
        }

        [Test]
        public void DisableByFlag_ReflectsGameState()
        {
            var state = new GameState();
            Assert.That(DisableByFlag.IsDisabled(state, GameFlagId.ExolegsDisabled), Is.False);

            state.SetFlag(GameFlagId.ExolegsDisabled);

            Assert.That(DisableByFlag.IsDisabled(state, GameFlagId.ExolegsDisabled), Is.True);
        }

        [Test]
        public void DetectionSensor_HasNoRespawnDependency()
        {
            FieldInfo[] fields = typeof(DetectionSensor2D).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            Assert.That(fields.Any(field => field.FieldType == typeof(EncounterContext)), Is.False);
            Assert.That(fields.Any(field => field.FieldType == typeof(HazardContact2D)), Is.False);
        }

        [Test]
        public void ActiveHazard_PhysicalContactRequestsRespawn()
        {
            Assert.That(HazardContact2D.ShouldRequestRespawn(true, true), Is.True);
        }

        [Test]
        public void InactiveHazard_PhysicalContactDoesNotRequestRespawn()
        {
            Assert.That(HazardContact2D.ShouldRequestRespawn(false, true), Is.False);
        }

        [Test]
        public void ActiveHazard_PlayerSensorContactDoesNotRequestRespawn()
        {
            Assert.That(HazardContact2D.ShouldRequestRespawn(true, false), Is.False);
        }
    }
}
