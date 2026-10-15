using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed partial class NarrativePanelView
    {
        private void CompactBody()
        { body.rectTransform.sizeDelta=new Vector2(850f,116f); body.rectTransform.anchoredPosition=new Vector2(0f,135f); }
        public void Options(string heading,string content,string[] labels,Action<int> selected,string hint="Esc para volver")
        {
            Clear(); CompactBody(); overlay.SetActive(true); title.text=heading; body.text=content;
            if (hintButton!=null) hintButton.gameObject.SetActive(false);
            portsRoot.gameObject.SetActive(true); resetButton.gameObject.SetActive(false); feedback.text=hint;
            for(int i=0;i<labels.Length;i++)
            {
                int choice=i;
                var obj=new GameObject("Option_"+i,typeof(RectTransform),typeof(Image),typeof(Button));
                var rect=obj.GetComponent<RectTransform>(); rect.SetParent(portsRoot,false);
                rect.sizeDelta=new Vector2(265f,48f); rect.anchoredPosition=new Vector2(-280f+(i%3)*280f,72f-(i/3)*58f);
                obj.GetComponent<Image>().color=new Color(0.06f,0.16f,0.2f);
                var text=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                text.rectTransform.SetParent(rect,false); text.rectTransform.sizeDelta=rect.sizeDelta-new Vector2(8f,8f);
                text.font=body.font; text.fontSize=16f; text.text=labels[i]; text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false;
                obj.GetComponent<Button>().onClick.AddListener(()=>selected(choice));
            }
        }
    }
}
