using System.Collections.Generic;
using System.Linq;
using EscapeUNIFRANZ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EscapeUNIFRANZ.Map
{
    public sealed class CampusMapView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private List<CampusMapNodeView> nodeViews=new List<CampusMapNodeView>();
        [SerializeField] private TMP_Text detail,objective;
        [SerializeField] private Button closeButton;
        [SerializeField] private Image[] connectionLines;
        private CampusMapData data;
        private GameState state;
        private string selectedZone;
        public bool IsVisible=>root!=null && root.activeSelf;
        private void Awake() { Hide(); }
        public void BindClose(System.Action close) { if(closeButton==null) return; closeButton.onClick.RemoveAllListeners(); closeButton.onClick.AddListener(()=>close()); }
        public void Show(CampusMapData next,GameState gameState)
        {
            Hide(); data=next; state=gameState; if(root==null || data==null || state==null) return;
            selectedZone=state.CurrentZoneId; state.MapKnowledgeChanged+=Refresh; state.ObjectiveChanged+=Refresh; root.transform.SetAsLastSibling(); root.SetActive(true); Refresh();
        }
        private void Refresh()
        {
            foreach(var node in data.Nodes) { var view=nodeViews.FirstOrDefault(v=>v!=null && v.ZoneId==node.ZoneId); if(view==null) continue; view.Render(node.DisplayName,CampusMapProgression.Resolve(state,node),node.ZoneId==state.CurrentObjectiveSectorId); string id=node.ZoneId; view.Bind(()=>{ selectedZone=id; Refresh(); }); }
            if(connectionLines!=null) for(int i=0;i<connectionLines.Length && i<data.Connections.Count;i++)
            { var edge=data.Connections[i]; if(connectionLines[i]!=null) connectionLines[i].color=state.IsZoneKnown(edge.FromZoneId) && state.IsZoneKnown(edge.ToZoneId) ? new Color(.2f,.65f,.7f,.8f) : new Color(.23f,.25f,.28f,.5f); }
            var definition=data.Nodes.FirstOrDefault(n=>n.ZoneId==selectedZone);
            if(detail!=null && definition!=null)
            {
                var points=state.KnownMapPoints.Where(p=>p.zoneId==selectedZone).ToArray();
                detail.text=definition.DisplayName+"\n"+(state.IsZoneDiscovered(selectedZone) ? "Explorado parcialmente" : "Sector identificado por CCTV")+(selectedZone==state.CurrentZoneId ? " • ESTÁS AQUÍ" : "")+"\n\nELEMENTOS DESCUBIERTOS\n"+(points.Length==0 ? "Sin puntos registrados." : string.Join("\n",points.Select(p=>Mark(p)+" "+p.label+" — "+p.status)));
            }
            if(objective!=null) { var controller=GetComponentInParent<EscapeUNIFRANZ.UI.ObjectiveController>(); string sector=string.IsNullOrEmpty(state.CurrentObjectiveSectorId) ? state.CurrentZoneId : state.CurrentObjectiveSectorId; objective.text="OBJETIVO • "+(controller?.CurrentText ?? "Consulta el objetivo del HUD")+"\nSector: "+data.Nodes.FirstOrDefault(n=>n.ZoneId==sector)?.DisplayName+" • Explora para localizar el dispositivo."; }
        }
        private static string Mark(KnownMapPoint point)=>point.kind==KnownPointKind.Door ? point.visualState==2 ? "<color=#64DB8C>■</color>" : point.visualState==4 ? "<color=#EAC76C>■</color>" : "<color=#EF6970>■</color>" : point.kind==KnownPointKind.CCTV || point.kind==KnownPointKind.Camera ? "[CAM]" : point.kind==KnownPointKind.Checkpoint ? "+" : "·";
        public void Hide() { if(state!=null) { state.MapKnowledgeChanged-=Refresh; state.ObjectiveChanged-=Refresh; } state=null; root?.SetActive(false); }
        private void OnDisable() { Hide(); }
    }
}
