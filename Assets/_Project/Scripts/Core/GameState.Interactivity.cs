using System;
using System.Collections.Generic;
using UnityEngine;
namespace EscapeUNIFRANZ.Core
{
    public enum ContextItemResult { Ready, Missing, WrongSelection }
    public enum KnownPointKind { Item, Panel, CCTV, Document, Door, Checkpoint, Camera }
    [Serializable] public sealed class KnownMapPoint
    { public string zoneId,id,label,status; public KnownPointKind kind; public int visualState; }
    public sealed partial class GameState
    {
        [SerializeField] private string selectedItemId=string.Empty;
        [SerializeField] private bool flashlightOn=true;
        [SerializeField] private string currentObjectiveSectorId;
        public string CurrentObjectiveSectorId => currentObjectiveSectorId;
        public void SetObjectiveSector(string id) { if(currentObjectiveSectorId==id) return; currentObjectiveSectorId=id; MapKnowledgeChanged?.Invoke(); }
        [SerializeField] private List<string> discoveredZones=new List<string>();
        [SerializeField] private List<KnownMapPoint> knownMapPoints=new List<KnownMapPoint>();
        public string SelectedItemId => selectedItemId;
        public bool FlashlightOn => flashlightOn;
        public bool HasActiveFlashlight => HasItem("flashlight") && flashlightOn;
        public IReadOnlyList<string> DiscoveredZones => discoveredZones;
        public IReadOnlyList<KnownMapPoint> KnownMapPoints => knownMapPoints;
        public bool SelectItem(string id)
        {
            if (!string.IsNullOrEmpty(id) && !HasItem(id)) return false;
            selectedItemId=id ?? string.Empty; InventoryChanged?.Invoke(); return true;
        }
        public ContextItemResult CheckContextItem(string expected)
        {
            if (!string.IsNullOrEmpty(selectedItemId) && selectedItemId!=expected) return ContextItemResult.WrongSelection;
            return HasItem(expected) ? ContextItemResult.Ready : ContextItemResult.Missing;
        }
        public bool ApplyContextItem(string expected,bool consume)
        { return CheckContextItem(expected)==ContextItemResult.Ready && (!consume || TryUseItem(expected)); }
        public bool ToggleFlashlight()
        { if (!HasItem("flashlight")) return false; flashlightOn=!flashlightOn; InventoryChanged?.Invoke(); return true; }
        public bool DiscoverZone(string id)
        { if (string.IsNullOrWhiteSpace(id) || discoveredZones.Contains(id)) return false; discoveredZones.Add(id); MapKnowledgeChanged?.Invoke(); return true; }
        public bool IsZoneDiscovered(string id)=>discoveredZones.Contains(id);
        public bool IsZoneKnown(string id)=>IsZoneDiscovered(id) || IsWorldItemDiscovered("sector",id);
        public void RecordMapPoint(string zone,string id,string label,KnownPointKind kind,string status,int visualState=0)
        {
            var point=knownMapPoints.Find(p=>p.zoneId==zone && p.id==id);
            if (point!=null && point.label==label && point.status==status && point.visualState==visualState) return;
            if (point==null) { point=new KnownMapPoint{zoneId=zone,id=id}; knownMapPoints.Add(point); }
            point.label=label; point.kind=kind; point.status=status; point.visualState=visualState; MapKnowledgeChanged?.Invoke();
        }
    }
}
