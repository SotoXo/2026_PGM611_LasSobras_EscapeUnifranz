using System.Collections.Generic;
using EscapeUNIFRANZ.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class InteractionCandidateResolverTests
    {
        private sealed class FakeInteractable : IInteractable
        {
            public FakeInteractable(int priority, bool canInteract = true)
            {
                Priority = priority;
                CanInteract = canInteract;
            }

            public string Prompt => "Test";
            public int Priority { get; }
            public bool CanInteract { get; }
            public void Interact() { }
        }

        [Test]
        public void Resolve_ZeroCandidates_ReturnsNull()
        {
            IInteractable result = InteractionCandidateResolver.Resolve(
                new List<InteractionCandidate>(),
                Vector2.zero);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void Resolve_OneCandidate_ReturnsCandidate()
        {
            var target = new FakeInteractable(priority: 0);
            var candidates = new List<InteractionCandidate>
            {
                new InteractionCandidate(target, Vector2.one, stableOrder: 0)
            };

            IInteractable result = InteractionCandidateResolver.Resolve(candidates, Vector2.zero);

            Assert.That(result, Is.SameAs(target));
        }

        [Test]
        public void Resolve_HigherPriorityWinsBeforeDistance()
        {
            var nearby = new FakeInteractable(priority: 1);
            var important = new FakeInteractable(priority: 2);
            var candidates = new List<InteractionCandidate>
            {
                new InteractionCandidate(nearby, new Vector2(0.1f, 0f), stableOrder: 0),
                new InteractionCandidate(important, new Vector2(5f, 0f), stableOrder: 1)
            };

            IInteractable result = InteractionCandidateResolver.Resolve(candidates, Vector2.zero);

            Assert.That(result, Is.SameAs(important));
        }

        [Test]
        public void Resolve_EqualPriorityChoosesNearest()
        {
            var far = new FakeInteractable(priority: 1);
            var near = new FakeInteractable(priority: 1);
            var candidates = new List<InteractionCandidate>
            {
                new InteractionCandidate(far, new Vector2(3f, 0f), stableOrder: 0),
                new InteractionCandidate(near, new Vector2(1f, 0f), stableOrder: 1)
            };

            IInteractable result = InteractionCandidateResolver.Resolve(candidates, Vector2.zero);

            Assert.That(result, Is.SameAs(near));
        }

        [Test]
        public void Resolve_RemovedCandidateIsIgnored()
        {
            var remaining = new FakeInteractable(priority: 0);
            var candidates = new List<InteractionCandidate>
            {
                default,
                new InteractionCandidate(remaining, Vector2.one, stableOrder: 1)
            };

            IInteractable result = InteractionCandidateResolver.Resolve(candidates, Vector2.zero);

            Assert.That(result, Is.SameAs(remaining));
        }

        [Test]
        public void Resolve_ExactTieUsesStableOrderIndependentlyOfListOrder()
        {
            var registeredFirst = new FakeInteractable(priority: 1);
            var registeredSecond = new FakeInteractable(priority: 1);
            var candidates = new List<InteractionCandidate>
            {
                new InteractionCandidate(registeredSecond, Vector2.one, stableOrder: 20),
                new InteractionCandidate(registeredFirst, Vector2.one, stableOrder: 10)
            };

            IInteractable result = InteractionCandidateResolver.Resolve(candidates, Vector2.zero);

            Assert.That(result, Is.SameAs(registeredFirst));
        }
    }
}
