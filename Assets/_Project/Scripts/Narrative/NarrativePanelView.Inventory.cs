using System;
using System.Linq;
using EscapeUNIFRANZ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed partial class NarrativePanelView
    {
        public void ShowInventory(ItemData[] items,GameState state,string inspected,Action<string> inspect,Action select,Action examine,Action use,Action clear)
        {
            Clear(); CompactBody(); overlay.SetActive(true); title.text="INVENTARIO";
            portsRoot.gameObject.SetActive(true); resetButton.gameObject.SetActive(false); hintButton.gameObject.SetActive(false);
            var current=items.FirstOrDefault(i=>i.id==inspected);
            string status=current==null ? "" : state.SelectedItemId==current.id ? "SELECCIONADO" : "DISPONIBLE";
            body.text=current==null ? "Todavía no llevas objetos. Examina el entorno con E." : current.displayName+" • "+current.CategoryLabel+" • "+status+"\n"+current.description+"\n"+(current.consumable ? "Se instala y se consume." : "Permanece en tu inventario.");
            feedback.text="El objeto seleccionado queda listo para usarse en el mundo. Esc para cerrar.";
            for(int i=0;i<items.Length;i++)
            {
                var item=items[i]; var rect=InventoryButton("Item_"+item.id,new Vector2(-330f+(i%4)*220f,65f-(i/4)*75f),new Vector2(208f,68f),"",()=>inspect(item.id));
                rect.GetComponent<Image>().color=item.id==inspected ? new Color(0.1f,0.34f,0.37f) : new Color(0.06f,0.13f,0.17f);
                if (item.icon!=null)
                {
                    var icon=new GameObject("Icon",typeof(RectTransform),typeof(Image)).GetComponent<Image>(); icon.rectTransform.SetParent(rect,false);
                    icon.rectTransform.anchoredPosition=new Vector2(-75f,0); icon.rectTransform.sizeDelta=new Vector2(42f,42f); icon.sprite=item.icon; icon.preserveAspect=true; icon.raycastTarget=false;
                }
                else InventoryText(rect,"Symbol",item.fallbackIcon,new Vector2(-76f,0),new Vector2(38f,42f),18f);
                int quantity=state.CarriedItems.Count(id=>id==item.id);
                InventoryText(rect,"Name",item.displayName+(quantity>1 ? " ×"+quantity : ""),new Vector2(25f,0),new Vector2(140f,60f),15f);
            }
            if (current!=null)
            {
                InventoryButton("Select",new Vector2(-330f,-68f),new Vector2(208f,40f),"SELECCIONAR",select);
                InventoryButton("Examine",new Vector2(-110f,-68f),new Vector2(208f,40f),"EXAMINAR",examine);
                if (current.immediateUse!=ImmediateItemUse.None) InventoryButton("Use",new Vector2(110f,-68f),new Vector2(208f,40f),state.FlashlightOn ? "APAGAR LINTERNA" : "ENCENDER LINTERNA",use);
                InventoryButton("ClearSelection",new Vector2(330f,-68f),new Vector2(208f,40f),"QUITAR SELECCIÓN",clear);
            }
        }
        private RectTransform InventoryButton(string name,Vector2 pos,Vector2 size,string caption,Action action)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button)); var rect=obj.GetComponent<RectTransform>(); rect.SetParent(portsRoot,false); rect.anchoredPosition=pos; rect.sizeDelta=size;
            obj.GetComponent<Image>().color=new Color(0.06f,0.16f,0.2f); obj.GetComponent<Button>().onClick.AddListener(()=>{ProjectAudioHooks.Emit(AudioCue.Button); action();});
            InventoryText(rect,"Label",caption,Vector2.zero,size-new Vector2(10,6),15f); return rect;
        }
        private void InventoryText(RectTransform parent,string name,string caption,Vector2 pos,Vector2 size,float fontSize)
        {
            var text=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>(); text.rectTransform.SetParent(parent,false); text.rectTransform.anchoredPosition=pos; text.rectTransform.sizeDelta=size;
            text.font=body.font; text.fontSize=fontSize; text.text=caption; text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false;
        }
        public void ShowItemExamination(ItemData item)
        {
            Clear(); overlay.SetActive(true); title.text=item.displayName+" — EXAMINAR"; body.rectTransform.sizeDelta=new Vector2(850,330); body.rectTransform.anchoredPosition=new Vector2(0,38); body.text=item.examineText;
            portsRoot.gameObject.SetActive(false); resetButton.gameObject.SetActive(false); hintButton.gameObject.SetActive(false); closeButton.GetComponentInChildren<TMP_Text>(true).text="VOLVER AL INVENTARIO"; feedback.text="Esc para volver al inventario.";
        }
    }
}
