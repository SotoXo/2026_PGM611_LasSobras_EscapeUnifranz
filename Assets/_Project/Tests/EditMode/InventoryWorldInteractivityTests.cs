using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class InventoryWorldInteractivityTests
    {
        [Test] public void PickupRegistersItemAndRejectsRepeatedOrSharedDuplicate()
        { var state=new GameState(); Assert.That(state.TryCollectPickup("pickup_a","battery"),Is.True); Assert.That(state.TryCollectPickup("pickup_a","battery"),Is.False); Assert.That(state.TryCollectPickup("pickup_b","battery"),Is.False); Assert.That(state.CarriedItems.Count,Is.EqualTo(1)); }
        [Test] public void InstalledConsumableCannotRespawnFromAnotherPickup()
        { var state=new GameState(); state.TryCollectPickup("a","battery"); state.SelectItem("battery"); Assert.That(state.ApplyContextItem("battery",true),Is.True); Assert.That(state.HasItem("battery"),Is.False); Assert.That(state.SelectedItemId,Is.Empty); Assert.That(state.TryCollectPickup("b","battery"),Is.False); }
        [TestCase("key")][TestCase("lab_card")][TestCase("maintenance_card")]
        public void ReusableCredentialRemainsAfterContextUse(string item)
        { var state=new GameState(); state.AddItem(item); Assert.That(state.ApplyContextItem(item,false),Is.True); Assert.That(state.ApplyContextItem(item,false),Is.True); Assert.That(state.HasItem(item),Is.True); }
        [TestCase("fuse")][TestCase("network_module")]
        public void ContextConsumesExactlyOneObject(string item)
        { var state=new GameState(); state.AddItem(item); Assert.That(state.ApplyContextItem(item,true),Is.True); Assert.That(state.ApplyContextItem(item,true),Is.False); }
        [Test] public void WrongSelectionPreservesBothItems()
        { var state=new GameState(); state.AddItem("key"); state.AddItem("battery"); state.SelectItem("key"); Assert.That(state.CheckContextItem("battery"),Is.EqualTo(ContextItemResult.WrongSelection)); Assert.That(state.ApplyContextItem("battery",true),Is.False); Assert.That(state.CarriedItems.Count,Is.EqualTo(2)); state.SelectItem(null); Assert.That(state.ApplyContextItem("battery",true),Is.True); }
        [Test] public void SelectionRejectsUnownedItems()
        { var state=new GameState(); Assert.That(state.SelectItem("battery"),Is.False); state.AddItem("key"); Assert.That(state.SelectItem("key"),Is.True); Assert.That(state.SelectedItemId,Is.EqualTo("key")); Assert.That(state.SelectItem(null),Is.True); Assert.That(state.SelectedItemId,Is.Empty); }
        [Test] public void FlashlightCannotToggleBeforePickupAndTogglesAfter()
        { var state=new GameState(); Assert.That(state.HasActiveFlashlight,Is.False); Assert.That(state.ToggleFlashlight(),Is.False); state.TryCollectPickup("torch","flashlight"); Assert.That(state.HasActiveFlashlight,Is.True); Assert.That(state.ToggleFlashlight(),Is.True); Assert.That(state.HasActiveFlashlight,Is.False); Assert.That(state.ToggleFlashlight(),Is.True); Assert.That(state.HasActiveFlashlight,Is.True); }
        [Test] public void ForwardLightDisabledWithoutOwnershipAndWhenSwitchedOff()
        {
            var obj=new GameObject("Vision test"); obj.AddComponent<PlayerVisualController>(); var vision=obj.AddComponent<PlayerVisionController>(); vision.ApplyLightConfiguration();
            try { var cone=obj.transform.Find("ForwardVisionLight").GetComponent<Light2D>(); vision.SetFlashlightState(false,true); Assert.That(cone.enabled,Is.False); Assert.That(cone.intensity,Is.Zero); vision.SetFlashlightState(true,true); Assert.That(cone.enabled,Is.True); Assert.That(cone.intensity,Is.GreaterThan(1f)); vision.SetFlashlightState(true,false); Assert.That(cone.enabled,Is.False); Assert.That(obj.transform.Find("ImmediateVisionLight").GetComponent<Light2D>().intensity,Is.GreaterThan(0)); }
            finally { Object.DestroyImmediate(obj); }
        }
        [Test] public void EnteringZoneDiscoversOnlyThatZone()
        { var state=new GameState(); state.SetLocation("entrada","inicio"); Assert.That(state.IsZoneKnown("entrada"),Is.True); Assert.That(state.IsZoneKnown("hall"),Is.False); Assert.That(state.IsZoneKnown("nucleo_ia"),Is.False); }
        [Test] public void UnlockFlagDoesNotRevealUnvisitedZone()
        { var state=new GameState(); state.SetFlag(GameFlagId.HallVisited); state.SetFlag(GameFlagId.Piso2Unlocked); var node=new CampusMapNodeDefinition("piso2","Simulación",GameFlagId.Piso2Visited,GameFlagId.Piso2Unlocked,GameFlagId.HallVisited); Assert.That(CampusMapProgression.Resolve(state,node),Is.EqualTo(CampusMapNodeState.Unknown)); }
        [Test] public void CCTVIdentifiesSectorWithoutDiscoveringContents()
        { var state=new GameState(); state.DiscoverWorldItem("sector","hall"); Assert.That(state.IsZoneKnown("hall"),Is.True); Assert.That(state.IsZoneDiscovered("hall"),Is.False); Assert.That(state.KnownMapPoints.Count,Is.Zero); }
        [Test] public void CheckpointAndLocationChangesPreserveInventoryAndKnowledge()
        {
            var state=new GameState(); state.TryCollectPickup("torch","flashlight"); state.AddItem("maintenance_card"); state.SelectItem("maintenance_card"); state.ToggleFlashlight(); state.DiscoverWorldItem("clue","note"); state.RecordMapPoint("entrada","cctv","Terminal CCTV",KnownPointKind.CCTV,"ONLINE");
            state.SetLocation("entrada","inicio"); state.SetCheckpoint("entrada","seguro"); state.SetLocation("hall","desde_entrada"); state.SetLocation(state.CheckpointZoneId,state.CheckpointSpawnId);
            Assert.That(state.HasItem("flashlight"),Is.True); Assert.That(state.SelectedItemId,Is.EqualTo("maintenance_card")); Assert.That(state.FlashlightOn,Is.False); Assert.That(state.IsZoneKnown("hall"),Is.True); Assert.That(state.IsWorldItemDiscovered("clue","note"),Is.True); Assert.That(state.KnownMapPoints.Count,Is.EqualTo(1));
        }
        [Test] public void KnownDoorStatusUpdatesWithoutDuplicatingPoint()
        { var state=new GameState(); state.RecordMapPoint("hall","arca","Puerta ARCA",KnownPointKind.Door,"Requiere llave",4); state.RecordMapPoint("hall","arca","Puerta ARCA",KnownPointKind.Door,"Disponible",2); Assert.That(state.KnownMapPoints.Count,Is.EqualTo(1)); Assert.That(state.KnownMapPoints[0].visualState,Is.EqualTo(2)); }
        [Test] public void DoorRequiresCredentialAndSystemThenKeepsKey()
        { var state=new GameState(); var flags=new[]{GameFlagId.PowerEntranceRestored}; Assert.That(DoorAccessRules.CanOpen(state,flags,"key"),Is.False); state.AddItem("key"); Assert.That(DoorAccessRules.CanOpen(state,flags,"key"),Is.False); state.SetFlag(flags[0]); Assert.That(DoorAccessRules.CanOpen(state,flags,"key"),Is.True); Assert.That(state.ApplyContextItem("key",false),Is.True); Assert.That(state.HasItem("key"),Is.True); }
    }
}
