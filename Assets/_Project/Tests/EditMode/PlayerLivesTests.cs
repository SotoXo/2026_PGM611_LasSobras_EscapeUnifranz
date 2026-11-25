using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.UI;
using NUnit.Framework;
using UnityEngine;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class PlayerLivesTests
    {
        [Test]
        public void BootstrapContainsPixelArtLivesHudInBottomRight()
        {
            const string path="Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var view=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LivesHudView>(true)).Single();
                var rect=(RectTransform)view.transform;
                Assert.That(rect.anchorMin,Is.EqualTo(new Vector2(1,0)));
                Assert.That(rect.anchorMax,Is.EqualTo(new Vector2(1,0)));
                Assert.That(view.GetComponent<UnityEngine.UI.Image>().raycastTarget,Is.False);
                var data=new SerializedObject(view);
                var atlas=(Texture2D)data.FindProperty("atlas").objectReferenceValue;
                Assert.That(atlas.width,Is.EqualTo(1280));Assert.That(atlas.height,Is.EqualTo(288));
                Assert.That(atlas.filterMode,Is.EqualTo(FilterMode.Point));
                Assert.That(data.FindProperty("milliseconds").arraySize,Is.EqualTo(32));
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        [Test]
        public void FiveHitsReachZeroAndRetryRestoresFive()
        {
            var state=new GameState();int changes=0;state.LivesChanged+=()=>changes++;
            Assert.That(state.Lives,Is.EqualTo(5));
            for(int i=4;i>=0;i--){Assert.That(state.LoseLife(),Is.True);Assert.That(state.Lives,Is.EqualTo(i));}
            Assert.That(state.LoseLife(),Is.False);Assert.That(changes,Is.EqualTo(5));
            state.RestoreLives();Assert.That(state.Lives,Is.EqualTo(5));
        }
        [Test]
        public void SavedHealthSurvivesSerializationAndOlderSavesStartWithFive()
        {
            var state=new GameState();state.LoseLife();state.LoseLife();
            Assert.That(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(state)).Lives,Is.EqualTo(3));
            Assert.That(JsonUtility.FromJson<GameState>("{}").Lives,Is.EqualTo(5));
        }
        [Test]
        public void MultipleContactsInOneFrameOnlyRemoveOneHeart()
        {
            var obj=new GameObject("ContactCooldownTest");
            try
            {
                var session=obj.AddComponent<GameSessionController>();session.StartNewGame();
                session.RequestContactDamage(null,"Robot");session.RequestContactDamage(null,"Robot");
                Assert.That(session.State.Lives,Is.EqualTo(4));
            }
            finally{Object.DestroyImmediate(obj);}
        }
        [TestCase(5,0)] [TestCase(4,6)] [TestCase(3,12)]
        [TestCase(2,18)] [TestCase(1,24)] [TestCase(0,31)]
        public void HudKeepsCorrectFrameBetweenHits(int lives,int frame)
            => Assert.That(LivesHudView.RestFrame(lives),Is.EqualTo(frame));
    }
}
