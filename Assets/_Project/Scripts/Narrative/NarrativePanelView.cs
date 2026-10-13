using System.Collections.Generic;
using EscapeUNIFRANZ.Puzzles;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed partial class NarrativePanelView : MonoBehaviour
    {
        [SerializeField] private GameObject overlay;
        [SerializeField] private TMP_Text title, body, feedback;
        [SerializeField] private RectTransform portsRoot, wiresRoot;
        [SerializeField] private Button closeButton, resetButton, hintButton;
        private NarrativeController controller;
        private readonly Dictionary<string,RectTransform> ports=new Dictionary<string,RectTransform>();
        private readonly List<GameObject> cables=new List<GameObject>();
        private readonly List<GameObject> guides=new List<GameObject>();
        private readonly HashSet<string> blockedPorts=new HashSet<string>();
        private RectTransform pending;
        private string source;
        private void Awake() { Hide(); }
        public void Bind(NarrativeController target)
        {
            controller=target; closeButton.onClick.AddListener(target.HandleClose); resetButton.onClick.AddListener(target.ResetPuzzle);
            if (hintButton!=null) hintButton.onClick.AddListener(target.ReviewPuzzleHint);
        }
        public void ShowClue(ClueData clue)
        {
            body.rectTransform.sizeDelta=new Vector2(850f,330f); body.rectTransform.anchoredPosition=new Vector2(0f,38f);
            Clear(); overlay.SetActive(true); title.text=clue.title; body.text=clue.category+"\n\n"+clue.text;
            portsRoot.gameObject.SetActive(false); resetButton.gameObject.SetActive(false); feedback.text="E para revisar objetos • Esc o Cerrar para volver";
            if (hintButton!=null) hintButton.gameObject.SetActive(false);
        }
        public void ShowFoundHint(ClueData clue)
        {
            ShowClue(clue); closeButton.GetComponentInChildren<TMP_Text>(true).text="VOLVER AL PUZZLE";
            feedback.text="Solo muestra una pista leída. Tu progreso se conserva. Esc para volver.";
        }
        public void ShowPuzzle(PuzzleData data)
        {
            CompactBody();
            Clear(); overlay.SetActive(true); title.text="OBJETIVO: "+data.objective; body.text=data.instructions;
            if (hintButton!=null) hintButton.gameObject.SetActive(true);
            portsRoot.gameObject.SetActive(true); resetButton.gameObject.SetActive(true);
            feedback.text=data.IsConnectionPuzzle ? "Arrastra al destino o selecciona los dos extremos." : "Selecciona los botones. La pista explica el orden.";
            for(int i=0;i<data.ports.Length;i++)
            {
                var port=data.ports[i]; var obj=new GameObject(port.id,typeof(RectTransform),typeof(Image),typeof(Button));
                var rect=obj.GetComponent<RectTransform>(); rect.SetParent(portsRoot,false);
                rect.sizeDelta=new Vector2(220f,42f);
                rect.anchoredPosition=data.matchingColumns
                    ? new Vector2(port.output ? 280f : -280f,78f-port.row*52f)
                    : new Vector2(-280f+(i%3)*280f,75f-(i/3)*50f);
                obj.GetComponent<Image>().color=new Color(0.045f,0.1f,0.13f,1f);
                if (port.blocked) { blockedPorts.Add(port.id); obj.GetComponent<Image>().color=new Color(0.42f,0.07f,0.04f); }
                var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));
                var text=label.GetComponent<TextMeshProUGUI>(); text.rectTransform.SetParent(rect,false);
                text.rectTransform.anchorMin=Vector2.zero; text.rectTransform.anchorMax=Vector2.one;
                text.rectTransform.offsetMin=new Vector2(6f,4f); text.rectTransform.offsetMax=new Vector2(-6f,-4f);
                text.font=body.font; text.fontSize=18f; text.text=port.label; text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false;
                string id=port.id; obj.GetComponent<Button>().onClick.AddListener(()=>controller.SelectPort(id)); ports.Add(id,rect);
                if (data.IsConnectionPuzzle) DrawSignalIcon(rect,port);
                if (data.IsConnectionPuzzle)
                { var drag=obj.AddComponent<CablePortDrag>(); drag.portId=id; drag.controller=controller; }
            }
            if (data.diagramConnections!=null) foreach(var edge in data.diagramConnections)
            { var line=NewLine("Diagram",blockedPorts.Contains(edge.to) ? new Color(0.8f,0.2f,0.15f,0.45f) : new Color(0.3f,0.7f,0.9f,0.35f)); guides.Add(line.gameObject); PositionLine(line,Position(edge.from),Position(edge.to)); }
        }
        public void SelectPort(string id)
        {
            source=id;
            if (pending!=null) Destroy(pending.gameObject);
            pending=id==null ? null : NewLine("Pending",new Color(0.25f,0.85f,1f,0.7f));
            foreach(var p in ports) p.Value.GetComponent<Image>().color=p.Key==id ? new Color(0.1f,0.35f,0.45f) : blockedPorts.Contains(p.Key) ? new Color(0.42f,0.07f,0.04f) : new Color(0.045f,0.1f,0.13f);
        }
        private void Update()
        {
            if (pending==null || source==null || Mouse.current==null) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(wiresRoot,Mouse.current.position.ReadValue(),null,out Vector2 point))
                PositionLine(pending,Position(source),point);
        }
        public void RenderConnections(IReadOnlyList<CableConnection> connections)
        {
            foreach(var obj in cables) { obj.SetActive(false); Destroy(obj); } cables.Clear();
            foreach(var c in connections)
            {
                var line=NewLine(c.from+"_"+c.to,new Color(0.3f,0.9f,0.65f));
                cables.Add(line.gameObject); PositionLine(line,Position(c.from),Position(c.to));
            }
        }
        public void RenderCommandProgress(IEnumerable<string> commands)
        {
            var done=new HashSet<string>(commands);
            foreach(var port in ports) port.Value.GetComponent<Image>().color=done.Contains(port.Key) ? new Color(0.08f,0.35f,0.19f) : new Color(0.045f,0.1f,0.13f);
        }
        private Vector2 Position(string id) => wiresRoot.InverseTransformPoint(ports[id].TransformPoint(ports[id].rect.center));
        private RectTransform NewLine(string name,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image));
            var rect=obj.GetComponent<RectTransform>(); rect.SetParent(wiresRoot,false);
            var image=obj.GetComponent<Image>(); image.color=color; image.raycastTarget=false; return rect;
        }
        private static void PositionLine(RectTransform line,Vector2 a,Vector2 b)
        {
            Vector2 delta=b-a; line.anchoredPosition=(a+b)*0.5f; line.sizeDelta=new Vector2(delta.magnitude,3f);
            line.localRotation=Quaternion.Euler(0f,0f,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }
        public void Feedback(string message,bool error) { feedback.text=message; feedback.color=error ? new Color(1f,0.45f,0.35f) : new Color(0.65f,0.95f,0.85f); }
        public void Hide() { if (overlay!=null) overlay.SetActive(false); SelectPort(null); }
        private void Clear()
        {
            closeButton.GetComponentInChildren<TMP_Text>(true).text="CERRAR / ESC";
            SelectPort(null);
            foreach(Transform t in portsRoot) { t.gameObject.SetActive(false); Destroy(t.gameObject); }
            foreach(var c in cables) { c.SetActive(false); Destroy(c); } cables.Clear(); ports.Clear(); feedback.color=Color.white;
            foreach(var guide in guides) { guide.SetActive(false); Destroy(guide); } guides.Clear(); blockedPorts.Clear();
        }
    }
}
