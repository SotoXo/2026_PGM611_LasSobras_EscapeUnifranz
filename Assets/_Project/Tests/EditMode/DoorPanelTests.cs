using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class DoorPanelTests
    {
        [Test]
        public void DoorOpensThePassageAndCannotCloseOnAnOccupant()
        {
            var door = new GameObject("Door");
            var actor = new GameObject("Actor");
            try
            {
                var blocker = door.AddComponent<BoxCollider2D>();
                blocker.size = new Vector2(1f, .16f);
                var panel = door.AddComponent<DoorPanel2D>();
                using (var serialized = new SerializedObject(panel))
                {
                    serialized.FindProperty("blocker").objectReferenceValue = blocker;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                actor.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                actor.AddComponent<CircleCollider2D>().radius = .18f;
                Physics2D.SyncTransforms();

                panel.Open();
                Assert.That(blocker.enabled, Is.False);
                Assert.That(panel.TryClose(), Is.False);
                Assert.That(panel.IsOpen, Is.True);
                Assert.That(blocker.enabled, Is.False);

                actor.transform.position = Vector2.up;
                Physics2D.SyncTransforms();
                Assert.That(panel.TryClose(), Is.True);
                Assert.That(panel.IsOpen, Is.False);
                Assert.That(blocker.enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(actor);
                Object.DestroyImmediate(door);
            }
        }
    }
}
