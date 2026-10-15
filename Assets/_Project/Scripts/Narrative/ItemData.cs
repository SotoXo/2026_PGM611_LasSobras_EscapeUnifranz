using System.Collections.Generic;
using UnityEngine;
namespace EscapeUNIFRANZ.Narrative
{
    public enum ItemCategory { KeyItem, Tool, Consumable }
    public enum ImmediateItemUse { None, Flashlight }
    [CreateAssetMenu(menuName="Escape UNIFRANZ/Item")]
    public sealed class ItemData : ScriptableObject
    {
        public string id,displayName;
        [TextArea] public string description,examineText;
        public Sprite icon;
        public string fallbackIcon;
        public bool consumable,stackable,placeholderIcon;
        public ItemCategory category;
        public ImmediateItemUse immediateUse;
        private static readonly Dictionary<string,ItemData> catalog=new Dictionary<string,ItemData>();
        public static void Register(IEnumerable<ItemData> items)
        { catalog.Clear(); foreach(var item in items) if (item!=null) catalog[item.id]=item; }
        public static ItemData Find(string id)=>id!=null && catalog.TryGetValue(id,out var item) ? item : null;
        public static string Label(string id)=>Find(id)?.displayName ?? "Objeto";
        public string CategoryLabel=>category==ItemCategory.Tool ? "HERRAMIENTA" : category==ItemCategory.Consumable ? "CONSUMIBLE" : "OBJETO CLAVE";
    }
}
