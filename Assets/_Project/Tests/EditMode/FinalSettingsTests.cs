using EscapeUNIFRANZ.Core;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class FinalSettingsTests
    {
        [Test]
        public void BrightnessPreservesAuthoredLightAtDefaultAndBoundsTheIncrease()
        {
            const float authored = .16f;
            Assert.That(GameOptions.AmbientIntensity(authored, .5f), Is.EqualTo(authored));
            Assert.That(GameOptions.AmbientIntensity(authored, 0f), Is.LessThan(authored));
            Assert.That(GameOptions.AmbientIntensity(authored, 1f), Is.EqualTo(.32f).Within(.001f));
        }

        [TestCase(30, false, 60)]
        [TestCase(60, false, 120)]
        [TestCase(120, false, -1)]
        [TestCase(-1, false, 30)]
        [TestCase(30, true, 60)]
        [TestCase(60, true, 30)]
        public void FrameRateChoicesMatchThePlatform(int current, bool mobile, int next) =>
            Assert.That(GameOptions.NextFrameLimit(current, mobile), Is.EqualTo(next));

        [Test]
        public void SettingsReturnToPauseWithTimeFrozen()
        {
            var flow = new PresentationFlowState();
            Assert.That(flow.OpenOptions(), Is.False);
            flow.StartNewGame();
            flow.SkipIntro();
            flow.CompleteLoading();
            flow.Pause();
            Assert.That(flow.OpenOptions(), Is.True);
            Assert.That(flow.ShouldFreezeTime, Is.True);
            Assert.That(flow.Resume(), Is.False);
            Assert.That(flow.CloseOptions(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Pause));
        }

        [Test]
        public void BrightnessAndMuteSurviveReloadingPreferences()
        {
            const string brightnessKey = "EscapeUNIFRANZ.Brightness";
            const string muteKey = "EscapeUNIFRANZ.Muted";
            bool hadBrightness = PlayerPrefs.HasKey(brightnessKey), hadMute = PlayerPrefs.HasKey(muteKey);
            float brightness = PlayerPrefs.GetFloat(brightnessKey), listenerVolume = AudioListener.volume;
            int mute = PlayerPrefs.GetInt(muteKey);
            var load = typeof(GameOptions).GetMethod("ReadPreferences", BindingFlags.Static | BindingFlags.NonPublic);
            try
            {
                GameOptions.SetBrightness(.73f);
                GameOptions.SetMuted(true);
                GameOptions.Save();
                load.Invoke(null, null);
                Assert.That(GameOptions.Brightness, Is.EqualTo(.73f).Within(.0001f));
                Assert.That(GameOptions.Muted, Is.True);
            }
            finally
            {
                if (hadBrightness) PlayerPrefs.SetFloat(brightnessKey, brightness); else PlayerPrefs.DeleteKey(brightnessKey);
                if (hadMute) PlayerPrefs.SetInt(muteKey, mute); else PlayerPrefs.DeleteKey(muteKey);
                PlayerPrefs.Save();
                load.Invoke(null, null);
                AudioListener.volume = listenerVolume;
            }
        }
    }
}
