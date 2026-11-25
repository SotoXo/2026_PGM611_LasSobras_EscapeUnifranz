using System;
using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed partial class NarrativePanelView
    {
        private InventorySkin inventorySkin;
        private RectTransform inventoryRoot;
        private TMP_Text inventoryFeedback;
        private bool equipmentTab;
        private int inventoryCategory, inventoryPage;

        public void ShowInventory(ItemData[] items, GameState state, string inspected,
            Action<string> inspect, Action select, Action examine, Action use, Action clear)
        {
            Clear(); overlay.SetActive(true);
            title.transform.parent.gameObject.SetActive(false);
            if (inventorySkin == null) inventorySkin = Resources.Load<InventorySkin>("InventorySkin");
            inventoryRoot = InvRect(overlay.transform, "Inventory", 0, 0, 940, 540, false);
            inventoryRoot.gameObject.AddComponent<SafeAreaPanel>();
            Action redraw = () => ShowInventory(items, state, inspected, inspect, select, examine, use, clear);
            InvImage(inventoryRoot, "GridPanel", "panel_inventario", 100, 96, 472, 400);
            InvImage(inventoryRoot, "PreviewPanel", "panel_vista_previa", 572, 96, 344, 400);
            InvButton(inventoryRoot, "InventoryTab", equipmentTab ? "pestana_normal" : "pestana_seleccionada",
                100, 44, 220, 52, "INVENTARIO", () => { equipmentTab = false; inventoryPage = 0; redraw(); }, 22);
            InvButton(inventoryRoot, "EquipmentTab", equipmentTab ? "pestana_seleccionada" : "pestana_normal",
                324, 44, 220, 52, "EQUIPAMIENTO", () => { equipmentTab = true; inventoryCategory = 0; inventoryPage = 0; redraw(); }, 21);
            InvButton(inventoryRoot, "Close", "boton_cerrar", 868, 44, 48, 48, "", () => controller.HandleClose());
            string[] labels = { "TODOS", "OBJETOS", "LLAVES" };
            string[] icons = { "icono_liquidos", "icono_objetos", "icono_llaves" };
            for (int i = 0; i < labels.Length; i++)
            {
                int category = i;
                var tab = InvButton(inventoryRoot, "Category_" + i,
                    inventoryCategory == i ? "categoria_seleccionada" : "categoria_normal",
                    16, 104 + i * 100, 88, 92, "", () => { inventoryCategory = category; inventoryPage = 0; redraw(); });
                InvImage(tab, "Icon", icons[i], 12, 2, 64, 64);
                InvLabel(tab, "Label", labels[i], 3, 65, 82, 22, 13);
            }
            var visible = items.Where(item => (!equipmentTab || item.category == ItemCategory.Tool)
                && (inventoryCategory == 0 || (inventoryCategory == 2 ? IsInventoryKey(item) : !IsInventoryKey(item)))).ToArray();
            int pages = Mathf.Max(1, Mathf.CeilToInt(visible.Length / 20f));
            inventoryPage = Mathf.Clamp(inventoryPage, 0, pages - 1);
            var current = visible.FirstOrDefault(item => item.id == inspected) ?? visible.FirstOrDefault();
            // Filters must also update the controller's action target.
            if (current != null && current.id != inspected) { inspect(current.id); return; }
            for (int i = 0; i < 20; i++)
            {
                int index = inventoryPage * 20 + i;
                var item = index < visible.Length ? visible[index] : null;
                var slot = InvButton(inventoryRoot, "Slot_" + i,
                    item != null && item == current ? "casilla_seleccionada" : "casilla_normal",
                    118 + i % 5 * 90, 116 + i / 5 * 84, 80, 76, "",
                    () => { if (item != null) inspect(item.id); });
                slot.GetComponent<Button>().interactable = item != null;
                if (item == null) continue;
                InvItem(slot, item, 12, 10, 56, 56);
                int quantity = state.CarriedItems.Count(id => id == item.id);
                InvLabel(slot, "Quantity", "×" + quantity, 40, 55, 36, 20, 15);
                if (state.SelectedItemId == item.id) InvImage(slot, "Equipped", "casilla_seleccionada", 4, 3, 13, 13);
            }
            InvLabel(inventoryRoot, "ItemName", current == null ? "SIN OBJETOS" : current.displayName.ToUpperInvariant(), 592, 124, 304, 58, 23);
            if (current != null)
            {
                bool selected = state.SelectedItemId == current.id;
                bool immediate = current.immediateUse != ImmediateItemUse.None;
                InvItem(inventoryRoot, current, 692, 177, 104, 104);
                string info = selected ? "SELECCIONADO" : "";
                if (immediate) info += (selected ? " · " : "") + (state.FlashlightOn ? "ENCENDIDA" : "APAGADA");
                InvLabel(inventoryRoot, "State", info, 596, 286, 296, 24, 17);
                var description = InvLabel(inventoryRoot, "Description", current.description, 596, 310, 296, 106, 20);
                description.fontSizeMin = 15;
                InvButton(inventoryRoot, "Select", "boton_menu_seleccionado", 590, 432, 152, 44,
                    selected ? "DESELECCIONAR" : "SELECCIONAR", selected ? clear : select, 16);
                InvButton(inventoryRoot, "Examine", "boton_menu_normal", 748, 432, 152, 44, "EXAMINAR", examine, 18);
                if (immediate)
                    InvButton(inventoryRoot, "ToggleItem", "boton_menu_normal", 748, 246, 145, 34,
                        state.FlashlightOn ? "APAGAR" : "ENCENDER", use, 15);
            }
            else InvLabel(inventoryRoot, "Empty", "No llevas objetos en esta categoría.", 606, 230, 276, 130, 22);
            inventoryFeedback = InvLabel(inventoryRoot, "Feedback", Application.isMobilePlatform ? "Toca una casilla para revisar su contenido" : "Selecciona un objeto · Esc para cerrar", 100, 500, 816, 30, 15);
            if (pages > 1)
            {
                InvButton(inventoryRoot, "Previous", "boton_menu_normal", 118, 468, 80, 24, "<", () => { inventoryPage = (inventoryPage + pages - 1) % pages; redraw(); });
                InvLabel(inventoryRoot, "Page", (inventoryPage + 1) + " / " + pages, 220, 468, 200, 24, 14);
                InvButton(inventoryRoot, "Next", "boton_menu_normal", 478, 468, 80, 24, ">", () => { inventoryPage = (inventoryPage + 1) % pages; redraw(); });
            }
        }
        private static bool IsInventoryKey(ItemData item) => item.id == "key" || item.id.EndsWith("_card", StringComparison.Ordinal);
        private void ClearInventoryVisuals()
        {
            if (inventoryRoot != null) { inventoryRoot.gameObject.SetActive(false); Destroy(inventoryRoot.gameObject); }
            inventoryRoot = null; inventoryFeedback = null;
            if (title != null) title.transform.parent.gameObject.SetActive(true);
        }
        private RectTransform InvRect(Transform parent, string name, float x, float y, float width, float height, bool topLeft = true)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = topLeft ? new Vector2(0, 1) : Vector2.one * 0.5f;
            rect.pivot = rect.anchorMin; rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
            return rect;
        }
        private RectTransform InvImage(Transform parent, string name, string sprite, float x, float y, float width, float height)
        {
            var rect = InvRect(parent, name, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = inventorySkin != null ? inventorySkin.Find(sprite) : null;
            image.color = image.sprite != null ? Color.white : new Color(0.13f, 0.13f, 0.13f);
            image.raycastTarget = false;
            return rect;
        }
        private RectTransform InvButton(Transform parent, string name, string sprite, float x, float y, float width, float height, string caption, Action action, float fontSize = 18)
        {
            var rect = InvImage(parent, name, sprite, x, y, width, height);
            var image = rect.GetComponent<Image>(); image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(0.8f, 0.94f, 1f); colors.pressedColor = new Color(0.6f, 0.78f, 0.85f); colors.disabledColor = Color.white; button.colors = colors;
            button.onClick.AddListener(() => { ProjectAudioHooks.Emit(AudioCue.Button); action?.Invoke(); });
            if (!string.IsNullOrEmpty(caption)) InvLabel(rect, "Label", caption, 6, 2, width - 12, height - 4, fontSize);
            return rect;
        }
        private TMP_Text InvLabel(Transform parent, string name, string caption, float x, float y, float width, float height, float fontSize)
        {
            var rect = InvRect(parent, name, x, y, width, height);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = PresentationTheme.PixelFont; text.text = caption; text.fontSize = fontSize;
            text.enableAutoSizing = true; text.fontSizeMin = Mathf.Min(12, fontSize); text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center; text.color = new Color32(230, 230, 230, 255); text.raycastTarget = false;
            return text;
        }
        private void InvItem(Transform parent, ItemData item, float x, float y, float width, float height)
        {
            if (item.icon == null) { InvLabel(parent, "Symbol", item.fallbackIcon, x, y, width, height, 32); return; }
            var rect = InvRect(parent, "ItemIcon", x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = item.icon; image.preserveAspect = true; image.raycastTarget = false;
        }
        public void ShowItemExamination(ItemData item)
        {
            Clear(); overlay.SetActive(true); title.text = item.displayName + " — EXAMINAR";
            body.rectTransform.sizeDelta = new Vector2(850, 330); body.rectTransform.anchoredPosition = new Vector2(0, 38); body.text = item.examineText;
            portsRoot.gameObject.SetActive(false); resetButton.gameObject.SetActive(false); hintButton.gameObject.SetActive(false);
            closeButton.GetComponentInChildren<TMP_Text>(true).text = "VOLVER AL INVENTARIO"; feedback.text = "Esc para volver al inventario.";
        }
    }
}
