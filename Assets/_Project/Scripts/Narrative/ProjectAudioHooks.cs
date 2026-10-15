using System;
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
        ElectricBuzz, LightRestore, TutorialNotification
    }
    [Serializable] public class AudioCueBinding
    { public AudioCue cue; public AudioClip clip; [Range(0f,1f)] public float volume=0.7f; }
    // One optional clip catalog on the existing persistent root; missing clips stay silent.
    public sealed class ProjectAudioHooks : MonoBehaviour
    {
        public AudioCueBinding[] bindings=new AudioCueBinding[0];
        public AudioSource effects, ambience, music;
        private static ProjectAudioHooks instance;
        private void OnEnable() { instance=this; }
        private void OnDisable() { if (instance==this) instance=null; }
        public static void Emit(AudioCue cue)
        {
            if (instance==null) return;
            var entry=Array.Find(instance.bindings,b=>b!=null && b.cue==cue);
            if (entry==null || entry.clip==null) return;
            if (cue>=AudioCue.MusicMenu && cue<=AudioCue.MusicCore)
            { if (instance.music==null || instance.music.clip==entry.clip && instance.music.isPlaying) return; instance.music.clip=entry.clip; instance.music.volume=entry.volume; instance.music.loop=true; instance.music.Play(); }
            else if (cue==AudioCue.ElectricalHum || cue==AudioCue.HVAC || cue==AudioCue.BuildingAmbience)
            { if (instance.ambience==null || instance.ambience.clip==entry.clip && instance.ambience.isPlaying) return; instance.ambience.clip=entry.clip; instance.ambience.volume=entry.volume; instance.ambience.loop=true; instance.ambience.Play(); }
            else if (instance.effects!=null) instance.effects.PlayOneShot(entry.clip,entry.volume);
        }
    }
}
