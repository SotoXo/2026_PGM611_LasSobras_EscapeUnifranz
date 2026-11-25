using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class FinalInterfaceTests
    {
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
    }
}
