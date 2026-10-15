using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.Puzzles;
using UnityEngine;
using UnityEngine.InputSystem;
namespace EscapeUNIFRANZ.Narrative
{
    [DefaultExecutionOrder(100)]
    public sealed partial class NarrativeController : MonoBehaviour
    {
        [SerializeField] private GameSessionController session;
        [SerializeField] private GameplayModeController mode;
        [SerializeField] private NarrativePanelView view;
        [SerializeField] private PlayerVisionController vision;
        [SerializeField] private ItemData[] itemCatalog;
        [SerializeField] private TMPro.TMP_Text equipmentLabel, pickupLabel;
        [SerializeField] private UnityEngine.UI.Image pickupIcon;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip correctSound, incorrectSound;
        private GameState state;
        private PuzzleInteractable activePuzzle;
        private PuzzleModel model;
        private string selectedPort;
        private bool modalOpen, previousCursor;
        private CursorLockMode previousLock;
        private GameplayMode returnMode;
        private float previousTimeScale;
        private GameplayMode modalMode;
        private void Awake() { ItemData.Register(itemCatalog); }
        private void OnEnable() { session.StateChanged+=Bind; mode.ModeChanged+=OnModeChanged; }
        private void Start() { view.Bind(this); Bind(session.State); }
        private void Update()
        {
            if (modalOpen && Keyboard.current?.escapeKey.wasPressedThisFrame==true) HandleClose();
            else if (!modalOpen && Keyboard.current!=null)
            { if (Keyboard.current.jKey.wasPressedThisFrame) ShowJournal(); else if (Keyboard.current.iKey.wasPressedThisFrame) ShowInventory(); else if (mode.AllowsInteraction && Keyboard.current.fKey.wasPressedThisFrame) ToggleFlashlight(); }
            if(inventoryExplanationUntil>0 && Time.unscaledTime>inventoryExplanationUntil) { inventoryExplanationUntil=0; if(modalOpen && modalMode==GameplayMode.Inventory && !examiningItem) view.Feedback("Examina o selecciona el objeto. Esc para cerrar.",false); }
            if (pickupLabel!=null && Time.unscaledTime>pickupToastUntil) { pickupLabel.gameObject.SetActive(false); if (pickupIcon!=null) pickupIcon.gameObject.SetActive(false); }
        }
        private void OnDisable()
        {
            session.StateChanged-=Bind; mode.ModeChanged-=OnModeChanged;
            if (state!=null) state.InventoryChanged-=ApplyEquipment;
            if (modalOpen) Close();
        }
        private void Bind(GameState next)
        {
            if (modalOpen) Close();
            if (state!=null) state.InventoryChanged-=ApplyEquipment;
            state=next;
            if (state!=null) state.InventoryChanged+=ApplyEquipment;
            ApplyEquipment();
        }
        private void ApplyEquipment()
        {
            vision?.SetFlashlightState(state!=null&&state.HasItem("flashlight"),state!=null&&state.FlashlightOn);
            if (equipmentLabel!=null) equipmentLabel.text=(state!=null && !string.IsNullOrEmpty(state.SelectedItemId) ? "OBJETO: "+ItemData.Label(state.SelectedItemId)+"   •   " : "")+(state!=null && state.HasItem("flashlight") ? "LINTERNA "+(state.FlashlightOn ? "ON" : "OFF") : "");
            if(equipmentLabel!=null) equipmentLabel.gameObject.SetActive(!string.IsNullOrEmpty(equipmentLabel.text));
        }
        private bool Open(GameplayMode next)
        {
            if (modalOpen || (!mode.AllowsInteraction && mode.CurrentMode!=GameplayMode.Pause)) return false;
            previousTimeScale=Time.timeScale; previousCursor=Cursor.visible; previousLock=Cursor.lockState;
            journalReading=false; reviewingPuzzleHint=false; examiningItem=false; returnMode=mode.CurrentMode; modalMode=next; modalOpen=true;
            mode.SetMode(next); Time.timeScale=0f; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; return true;
        }
        public void ShowClue(ClueData clue)
        {
            if (!Open(GameplayMode.Dialogue)) return;
            activePuzzle=null; model=null; selectedPort=null; view.ShowClue(clue);
        }
        public void ShowPuzzle(PuzzleInteractable owner)
        {
            if (!PuzzleModel.Validate(owner.puzzle,out string error)) { Debug.LogError(error,owner); return; }
            if (!Open(GameplayMode.Puzzle)) return;
            activePuzzle=owner; model=new PuzzleModel(owner.puzzle); selectedPort=null;
            view.ShowPuzzle(owner.puzzle);
        }
        public void SelectPort(string id)
        {
            if (!modalOpen || activePuzzle==null || model==null) return;
            PuzzleSubmission result;
            if (activePuzzle.puzzle.IsConnectionPuzzle)
            {
                if (selectedPort==null) { selectedPort=id; view.SelectPort(id); return; }
                if (selectedPort==id) { selectedPort=null; view.SelectPort(null); return; }
                result=model.Connect(selectedPort,id); selectedPort=null; view.SelectPort(null);
                view.RenderConnections(model.Connected);
            }
            else result=model.Command(id);
            if (!activePuzzle.puzzle.IsConnectionPuzzle) view.RenderCommandProgress(model.CompletedCommands);
            ProjectAudioHooks.Emit(activePuzzle.puzzle.IsConnectionPuzzle ? (result==PuzzleSubmission.Rejected ? AudioCue.CableError : AudioCue.CableConnect) : AudioCue.Switch);
            if (result==PuzzleSubmission.Rejected)
            { ProjectAudioHooks.Emit(AudioCue.TerminalError); view.Feedback("No coincide. Revisa la pista. Progreso válido: "+model.Progress+" / "+model.Total,true); Play(incorrectSound); return; }
            view.Feedback("Validado "+model.Progress+" / "+model.Total,false);
            if (result==PuzzleSubmission.Accepted) Play(correctSound);
            if (result==PuzzleSubmission.Completed)
            {
                var owner=activePuzzle;
                if (!owner.Complete())
                { if (modalOpen) { ResetPuzzle(); view.Feedback("El dispositivo no acepta cambios en este estado.",true); } return; }
                ProjectAudioHooks.Emit(AudioCue.PuzzleSolved); Play(correctSound); if (modalOpen) Close();
            }
        }
        public void ResetPuzzle()
        {
            if (model==null) return;
            model.Reset(); selectedPort=null; view.SelectPort(null); view.RenderConnections(model.Connected);
            if (activePuzzle!=null && !activePuzzle.puzzle.IsConnectionPuzzle) view.RenderCommandProgress(model.CompletedCommands);
            view.Feedback("Panel reiniciado. Consulta el registro antes de conectar.",false);
        }
        public void Close()
        {
            if (!modalOpen) return;
            if (modalMode==GameplayMode.Inventory) ProjectAudioHooks.Emit(AudioCue.InventoryClose);
            modalOpen=false; activePuzzle=null; model=null; selectedPort=null; view.Hide();
            Time.timeScale=previousTimeScale; Cursor.visible=previousCursor; Cursor.lockState=previousLock;
            if (mode.CurrentMode==modalMode) mode.SetMode(returnMode);
        }
        private void OnModeChanged(GameplayMode next)
        {
            if (!modalOpen || next==modalMode) return;
            modalOpen=false; activePuzzle=null; model=null; view.Hide();
            if (mode.AllowsMovement) { Time.timeScale=previousTimeScale; Cursor.visible=previousCursor; Cursor.lockState=previousLock; }
        }
        private void Play(AudioClip clip) { if (audioSource!=null && clip!=null) audioSource.PlayOneShot(clip); }
    }
}
