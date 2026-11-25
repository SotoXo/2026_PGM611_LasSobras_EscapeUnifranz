using System;
using System.Collections.Generic;
using EscapeUNIFRANZ.Core;
using UnityEngine;
using UnityEngine.Audio;

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
        CanThrow, CanImpact, FrancisBroadcast, FrancisFarewell,
        MenuFocus, PaperOpen, TerminalOpen, MusicDiscovery, MusicVictory, MusicDefeat, AttackImpact
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
        public AudioMixer mixer;
        public AudioMixerGroup musicBus, ambienceBus, sfxBus, uiBus, voicesBus;

        private static ProjectAudioHooks instance;
        private readonly Dictionary<AudioCue, AudioCueBinding> catalog = new();
        private readonly Dictionary<AudioCue, AudioSource> beds = new();
        private Vector3 bedGains = Vector3.one;
        private bool bedsFading;
        private readonly AudioSource[] positional = new AudioSource[8];
        private AudioSource fadingMusic;
        private AudioSource uiSource, voiceSource;
        private readonly float[] nextCueTime = new float[Enum.GetValues(typeof(AudioCue)).Length];
        private AudioCue alertCue;
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
            if (effects != null) effects.outputAudioMixerGroup = sfxBus;
            if (music != null) music.outputAudioMixerGroup = musicBus;
            if (ambience != null) ambience.outputAudioMixerGroup = ambienceBus;
        }

        private void Start() => GameOptions.BindMixer(mixer);

        private void OnDisable()
        {
            if (instance == this) instance = null;
        }

        public static void SetSceneMusic(AudioCue cue, bool withAmbience = false, Vector3? ambienceGains = null)
        {
            if (instance == null) return;
            instance.defaultMusic = cue;
            instance.alertUntil = 0f;
            instance.Play(cue);
            instance.bedGains = withAmbience ? ambienceGains ?? Vector3.one : Vector3.zero;
            instance.bedsFading = true;
            if (!withAmbience) return;
            instance.Play(AudioCue.ElectricalHum);
            instance.Play(AudioCue.HVAC);
            instance.Play(AudioCue.BuildingAmbience);
        }

        public static void AlertMusic(AudioCue cue, float seconds)
        {
            if (instance == null || instance.defaultMusic == AudioCue.MusicCore) return;
            if (instance.alertUntil > Time.time && Priority(cue) < Priority(instance.alertCue)) return;
            instance.alertCue = cue;
            instance.alertUntil = Time.time + seconds;
            instance.Play(cue);
        }

        private static int Priority(AudioCue cue) =>
            cue == AudioCue.MusicChase ? 3 : cue == AudioCue.MusicTension ? 2 : 1;

        public static void RestoreSceneMusic()
        {
            if (instance == null) return;
            instance.alertUntil = 0f;
            instance.Play(instance.defaultMusic);
        }

        public static void PlayOutcome(AudioCue cue)
        {
            if (instance == null) return;
            instance.alertUntil = 0f;
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
            if (volume <= 0.01f || !instance.AllowCue(cue)) return;
            for (int i = 0; i < instance.positional.Length; i++)
            {
                var source = instance.positional[i];
                if (source != null && source.isPlaying) continue;
                if (source == null) instance.positional[i] = source = instance.CreateSource(instance.sfxBus);
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
                    var source = cue == AudioCue.ElectricalHum && ambience != null ? ambience : CreateSource(ambienceBus);
                    StartLoop(source, entry.clip, 0f);
                    beds[cue] = source;
                    bedsFading = true;
                    break;
                case AudioCue.MusicMenu:
                case AudioCue.MusicExplore:
                case AudioCue.MusicTension:
                case AudioCue.MusicChase:
                case AudioCue.MusicCore:
                case AudioCue.MusicDiscovery:
                case AudioCue.MusicVictory:
                case AudioCue.MusicDefeat:
                    if (music != null && music.clip == entry.clip && music.isPlaying) return;
                    if (music == null) music = CreateSource(musicBus);
                    if (fadingMusic == null) fadingMusic = CreateSource(musicBus);
                    (music, fadingMusic) = (fadingMusic, music);
                    musicVolume = entry.volume;
                    if (music.clip != entry.clip || !music.isPlaying) StartLoop(music, entry.clip, 0f);
                    break;
                case AudioCue.FrancisBroadcast:
                case AudioCue.FrancisFarewell:
                    if (!AllowCue(cue)) break;
                    if (voiceSource == null) voiceSource = CreateSource(voicesBus);
                    voiceSource.Stop();
                    voiceSource.PlayOneShot(entry.clip, entry.volume);
                    break;
                case AudioCue.MenuFocus:
                case AudioCue.Button:
                case AudioCue.InventoryOpen:
                case AudioCue.InventoryClose:
                case AudioCue.MapOpen:
                case AudioCue.MapClose:
                case AudioCue.PaperOpen:
                case AudioCue.TerminalOpen:
                case AudioCue.TutorialNotification:
                    if (!AllowCue(cue)) break;
                    if (uiSource == null) uiSource = CreateSource(uiBus);
                    uiSource.PlayOneShot(entry.clip, entry.volume);
                    break;
                default:
                    if (effects != null && AllowCue(cue)) effects.PlayOneShot(entry.clip, entry.volume);
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
            if (music != null && !Mathf.Approximately(music.volume, musicVolume))
                music.volume = Mathf.MoveTowards(music.volume, musicVolume, step);
            if (fadingMusic != null && fadingMusic.isPlaying)
            {
                fadingMusic.volume = Mathf.MoveTowards(fadingMusic.volume, 0f, step);
                if (fadingMusic.volume <= 0f) fadingMusic.Stop();
            }
            if (!bedsFading) return;
            bedsFading = false;
            foreach (var bed in beds)
            {
                float gain = bed.Key == AudioCue.ElectricalHum ? bedGains.x :
                    bed.Key == AudioCue.HVAC ? bedGains.y : bedGains.z;
                float target = catalog[bed.Key].volume * Mathf.Clamp01(gain);
                float current = bed.Value.volume;
                if (Mathf.Approximately(current, target)) continue;
                float next = Mathf.MoveTowards(current, target, step);
                bed.Value.volume = next;
                bedsFading |= !Mathf.Approximately(next, target);
            }
        }

        private bool AllowCue(AudioCue cue)
        {
            int index = (int)cue;
            float now = Time.unscaledTime;
            if (now < nextCueTime[index]) return false;
            nextCueTime[index] = now + (cue == AudioCue.Flicker || cue == AudioCue.TerrorInterference ? .23f : .095f);
            return true;
        }

        private AudioSource CreateSource(AudioMixerGroup group)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = group;
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
