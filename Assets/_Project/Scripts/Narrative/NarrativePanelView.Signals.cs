using EscapeUNIFRANZ.Puzzles;
using UnityEngine;
using UnityEngine.UI;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed partial class NarrativePanelView
    {
        // Simple original line icons; no image download or font symbol dependency.
        private void DrawSignalIcon(RectTransform parent,CablePort port)
        {
            var holder=new GameObject("SignalIcon",typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(parent,false); holder.anchoredPosition=new Vector2(-88f,0f); holder.sizeDelta=new Vector2(28f,28f);
            if (port.icon!=null)
            { var image=holder.gameObject.AddComponent<Image>(); image.sprite=port.icon; image.preserveAspect=true; image.raycastTarget=false; return; }
            if (port.id.Contains("PWR"))
            { Stroke(holder,new Vector2(5,12),new Vector2(-5,1)); Stroke(holder,new Vector2(-5,1),new Vector2(4,1)); Stroke(holder,new Vector2(4,1),new Vector2(-5,-12)); }
            else if (port.id.Contains("DATA"))
            { Stroke(holder,new Vector2(-11,5),new Vector2(11,5)); Stroke(holder,new Vector2(11,5),new Vector2(6,10)); Stroke(holder,new Vector2(11,5),new Vector2(6,0)); Stroke(holder,new Vector2(11,-5),new Vector2(-11,-5)); Stroke(holder,new Vector2(-11,-5),new Vector2(-6,-10)); Stroke(holder,new Vector2(-11,-5),new Vector2(-6,0)); }
            else if (port.id.Contains("GND"))
            { Stroke(holder,new Vector2(0,12),Vector2.zero); Stroke(holder,new Vector2(-11,0),new Vector2(11,0)); Stroke(holder,new Vector2(-7,-5),new Vector2(7,-5)); Stroke(holder,new Vector2(-3,-10),new Vector2(3,-10)); }
            else if (port.id.Contains("CTRL"))
            { Stroke(holder,new Vector2(-10,-10),new Vector2(-10,10)); Stroke(holder,new Vector2(0,-10),new Vector2(0,10)); Stroke(holder,new Vector2(10,-10),new Vector2(10,10)); Stroke(holder,new Vector2(-13,3),new Vector2(-7,3)); Stroke(holder,new Vector2(-3,-4),new Vector2(3,-4)); Stroke(holder,new Vector2(7,7),new Vector2(13,7)); }
            else { Stroke(holder,new Vector2(-10,-10),new Vector2(10,-10)); Stroke(holder,new Vector2(-10,10),new Vector2(10,10)); }
        }
        private static void Stroke(RectTransform parent,Vector2 a,Vector2 b)
        {
            var line=new GameObject("Stroke",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>(); line.SetParent(parent,false);
            PositionLine(line,a,b); line.sizeDelta=new Vector2(line.sizeDelta.x,2f); line.GetComponent<Image>().raycastTarget=false; line.GetComponent<Image>().color=new Color(0.9f,0.96f,0.98f);
        }
    }
}
