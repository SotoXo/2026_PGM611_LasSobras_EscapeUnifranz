using System.Reflection;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.UI;
using NUnit.Framework;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class GameSessionControllerTests
    {
        private GameObject sessionObject;
        private GameObject viewObject;
        private GameObject phaseRoot;

        [TearDown]
        public void TearDown()
        {
            if (sessionObject != null)
            {
                Object.DestroyImmediate(sessionObject);
            }

            if (viewObject != null)
            {
                Object.DestroyImmediate(viewObject);
            }

            if (phaseRoot != null)
            {
                Object.DestroyImmediate(phaseRoot);
            }
        }

        [Test]
        public void StartNewGame_HidesStaleBossPhaseBanner()
        {
            viewObject = new GameObject("BossPhaseViewTest");
            BossPhaseView view = viewObject.AddComponent<BossPhaseView>();
            phaseRoot = new GameObject("BossPhaseRootTest");
            SetField(view, "root", phaseRoot);

            sessionObject = new GameObject("GameSessionTest");
            GameSessionController session = sessionObject.AddComponent<GameSessionController>();
            SetField(session, "bossPhaseView", view);
            phaseRoot.SetActive(true);

            session.StartNewGame();

            Assert.That(phaseRoot.activeSelf, Is.False);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
