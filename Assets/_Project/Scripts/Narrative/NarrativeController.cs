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
            bool hasFlashlight = state?.HasItem("flashlight") == true;
            bool lightOn = hasFlashlight && state.FlashlightOn;
            vision?.SetFlashlightState(hasFlashlight, lightOn);
            if (equipmentLabel == null) return;

            string status = string.IsNullOrEmpty(state?.SelectedItemId) ? "" : "OBJETO: " + ItemData.Label(state.SelectedItemId);
            if (hasFlashlight && !Application.isMobilePlatform)
            {
                if (status.Length > 0) status += "   •   ";
                status += lightOn ? "<color=#FFE0A6>[F] LINTERNA · ENCENDIDA</color>" : "[F] LINTERNA · APAGADA";
            }
            equipmentLabel.text = status;
            equipmentLabel.gameObject.SetActive(status.Length > 0);
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
        public void ShowTrainingBriefing()
        {
            if (!session.IsPractice || !Open(GameplayMode.Dialogue)) return;
            view.Options("CANAL DE EMERGENCIA",
                "FRANCIS • Enlace parcial. El objetivo del HUD indica la acción pendiente. Acércate a dispositivos para ver qué puedes hacer. Lee cada panel antes de actuar.",
                new[] { "Ir al primer nivel" }, _ =>
                {
                    Close();
                    session.Tutorial?.PracticeConfirmBriefing();
                    session.GetComponent<GameFlowController>()?.CompletePractice();
                }, Application.isMobilePlatform ? "Confirma para ir a Recepción" : "Esc para volver");
        }
        public void ShowPuzzle(PuzzleInteractable owner)
        {
            if (!PuzzleModel.Validate(owner.puzzle,out string error)) { Debug.LogError(error,owner); return; }
            if (!Open(GameplayMode.Puzzle)) return;
            activePuzzle=owner; model=new PuzzleModel(owner.puzzle); selectedPort=null;
            view.ShowPuzzle(owner.puzzle);
        }
        private System.Action elevatorMenuClosed;
        public bool ShowElevatorFloors(World.ZoneExitInteractable owner)
        {
            if(!Open(GameplayMode.Dialogue))return false;
            elevatorMenuClosed=owner.CancelFloorSelection;
            var labels=new string[4];
            for(int i=0;i<3;i++)labels[i]=World.ZoneExitInteractable.ElevatorLabels[i]+(i==owner.CurrentElevatorFloor?" (piso actual)":"");
            labels[3]="Salir del selector";
            view.Options("ASCENSOR · SELECCIONAR PISO","Elige tu destino. Las puertas se cerrarán después de confirmar.",labels,index=>
            {
                elevatorMenuClosed=null;Close();
                if(index==3)owner.CancelFloorSelection();else owner.SelectElevatorFloor(index);
            });
            return true;
        }

        public void ShowElevatorInteraction(World.ZoneExitInteractable owner, ElevatorCircuit circuit, bool hasPiece)
        {
            if (!Open(GameplayMode.Puzzle)) return;
            bool repaired = circuit == null;
            view.Options("ASCENSOR", repaired ? "El ascensor está listo. Entra hasta el fondo de la cabina para elegir un piso." :
                "El panel de control necesita reparación.", new[] { "Interactuar con el ascensor", "Volver" }, choice =>
            {
                if (choice != 0) { Close(); return; }
                if (repaired)
                {
                    Close();
                    owner.UseRepairedElevator();
                    return;
                }
                view.ShowElevatorCircuit(owner.elevatorPanel, owner.elevatorTiles, circuit, hasPiece, () =>
                {
                    if (owner.CompleteElevatorCircuit()) Close();
                });
            });
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
            if (modalMode==GameplayMode.Inventory)
            {
                session.Tutorial?.PracticeCloseInventory();
                ProjectAudioHooks.Emit(AudioCue.InventoryClose);
            }
            modalOpen=false; activePuzzle=null; model=null; selectedPort=null; view.Hide();
            Time.timeScale=previousTimeScale; Cursor.visible=previousCursor; Cursor.lockState=previousLock;
            if (mode.CurrentMode==modalMode) mode.SetMode(returnMode);
            var closed=elevatorMenuClosed;elevatorMenuClosed=null;closed?.Invoke();
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
