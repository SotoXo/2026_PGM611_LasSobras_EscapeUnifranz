using System;
using System.Collections.Generic;
using EscapeUNIFRANZ.Core;
using UnityEngine;

namespace EscapeUNIFRANZ.Narrative
{
    public enum AudioCue
    {
        ElectricalHum, HVAC, BuildingAmbience, ElectronicInterference, MetalCreak,
        FootstepFloor, FootstepTile, Interaction, Pickup, Breathing,
        Fluorescent, Flicker, Blackout, Electricity,
        DoorLock, DoorUnlock, DoorOpen, DoorClose, ElectronicDoor,
        RobotServo, RobotWheels, RobotPatrol, RobotDetection, RobotAlert, RobotChase, RobotOff, RobotRestart,
        CameraMotor, CameraFeed, CameraDetection, CameraSignalLost,
        CableConnect, CableError, PuzzleSolved, Button, Switch, TerminalBoot, TerminalError,
        DistantImpact, MetallicNoise, TerrorInterference, RedAlarm, Stinger,
        MusicMenu, MusicExplore, MusicTension, MusicChase, MusicCore,
        InventoryOpen, InventoryClose, MapOpen, MapClose, FlashlightOn, FlashlightOff, ObjectUsed, ObjectError, Checkpoint,
        ElectricBuzz, LightRestore, TutorialNotification,
        CanThrow, CanImpact, FrancisBroadcast, FrancisFarewell
    }

    [Serializable]
    public sealed class AudioCueBinding
    {
        public AudioCue cue;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.7f;
    }

    public sealed class ProjectAudioHooks : MonoBehaviour
    {
        public AudioCueBinding[] bindings = Array.Empty<AudioCueBinding>();
        public AudioSource effects, ambience, music;

        private static ProjectAudioHooks instance;
        private readonly Dictionary<AudioCue, AudioCueBinding> catalog = new();
        private readonly Dictionary<AudioCue, AudioSource> beds = new();
        private readonly AudioSource[] positional = new AudioSource[8];
        private AudioSource fadingMusic;
        private Camera listenerCamera;
        private AudioCue defaultMusic;
        private float musicVolume;
        private float alertUntil;

        private void OnEnable()
        {
            instance = this;
            catalog.Clear();
            foreach (var binding in bindings)
                if (binding != null && binding.clip != null) catalog[binding.cue] = binding;
            GameOptions.ApplyVolume();
        }

        private void OnDisable()
        {
            if (instance == this) instance = null;
        }

        public static void SetSceneMusic(AudioCue cue, bool withAmbience = false)
        {
            if (instance == null) return;
            instance.defaultMusic = cue;
            instance.alertUntil = 0f;
            instance.Play(cue);
            if (!withAmbience) return;
            instance.Play(AudioCue.ElectricalHum);
            instance.Play(AudioCue.HVAC);
            instance.Play(AudioCue.BuildingAmbience);
        }

        public static void AlertMusic(AudioCue cue, float seconds)
        {
            if (instance == null || instance.defaultMusic == AudioCue.MusicCore) return;
            instance.alertUntil = Mathf.Max(instance.alertUntil, Time.time + seconds);
            instance.Play(cue);
        }

        public static void Emit(AudioCue cue)
        {
            if (instance != null) instance.Play(cue);
        }

        public static void EmitAt(AudioCue cue, Vector2 position)
        {
            if (instance == null || !instance.catalog.TryGetValue(cue, out var entry)) return;
            if (instance.listenerCamera == null) instance.listenerCamera = Camera.main;
            if (instance.listenerCamera == null) return;

            Vector2 offset = position - (Vector2)instance.listenerCamera.transform.position;
            float volume = entry.volume * Mathf.Clamp01(1f - offset.magnitude / 12f);
            if (volume <= 0.01f) return;
            for (int i = 0; i < instance.positional.Length; i++)
            {
                var source = instance.positional[i];
                if (source != null && source.isPlaying) continue;
                if (source == null) instance.positional[i] = source = instance.CreateSource();
                source.panStereo = Mathf.Clamp(offset.x / 6f, -0.85f, 0.85f);
                source.PlayOneShot(entry.clip, volume);
                return;
            }
        }

        private void Play(AudioCue cue)
        {
            if (!catalog.TryGetValue(cue, out var entry)) return;
            switch (cue)
            {
                case AudioCue.ElectricalHum:
                case AudioCue.HVAC:
                case AudioCue.BuildingAmbience:
                    if (beds.ContainsKey(cue)) return;
                    var source = cue == AudioCue.ElectricalHum && ambience != null ? ambience : CreateSource();
                    StartLoop(source, entry.clip, entry.volume);
                    beds[cue] = source;
                    break;
                case AudioCue.MusicMenu:
                case AudioCue.MusicExplore:
                case AudioCue.MusicTension:
                case AudioCue.MusicChase:
                case AudioCue.MusicCore:
                    if (music != null && music.clip == entry.clip && music.isPlaying) return;
                    if (music == null) music = CreateSource();
                    if (fadingMusic == null) fadingMusic = CreateSource();
                    (music, fadingMusic) = (fadingMusic, music);
                    musicVolume = entry.volume;
                    StartLoop(music, entry.clip, 0f);
                    break;
                default:
                    if (effects != null) effects.PlayOneShot(entry.clip, entry.volume);
                    break;
            }
        }

        private void Update()
        {
            if (alertUntil > 0f && Time.time >= alertUntil)
            {
                alertUntil = 0f;
                Play(defaultMusic);
            }

            float step = Time.unscaledDeltaTime * 0.25f;
            if (music != null) music.volume = Mathf.MoveTowards(music.volume, musicVolume, step);
            if (fadingMusic != null && fadingMusic.isPlaying)
            {
                fadingMusic.volume = Mathf.MoveTowards(fadingMusic.volume, 0f, step);
                if (fadingMusic.volume <= 0f) fadingMusic.Stop();
            }
        }

        private AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        private static void StartLoop(AudioSource source, AudioClip clip, float volume)
        {
            source.Stop();
            source.clip = clip;
            source.loop = true;
            source.volume = volume;
            source.Play();
        }
    }
}
