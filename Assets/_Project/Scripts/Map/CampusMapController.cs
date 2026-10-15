using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Input;
using EscapeUNIFRANZ.Narrative;
using UnityEngine;
using UnityEngine.InputSystem;
namespace EscapeUNIFRANZ.Map
{
    [DefaultExecutionOrder(100)]
    public sealed class CampusMapController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private GameplayModeController gameplayMode;
        [SerializeField] private GameSessionController session;
        [SerializeField] private CampusMapData mapData;
        [SerializeField] private CampusMapView view;
        private GameplayMode returnMode;
        private float previousTime;
        private bool opened,previousCursor;
        private CursorLockMode previousLock;
        private void OnEnable() { if(inputReader!=null) inputReader.ToggleMapPressed+=OnToggleMapPressed; if(gameplayMode!=null) gameplayMode.ModeChanged+=OnModeChanged; }
        private void OnDisable() { if(inputReader!=null) inputReader.ToggleMapPressed-=OnToggleMapPressed; if(gameplayMode!=null) gameplayMode.ModeChanged-=OnModeChanged; CloseIfOpen(); }
        private void Update() { if(opened && Keyboard.current?.escapeKey.wasPressedThisFrame==true) CloseIfOpen(); }
        public bool Toggle()
        {
            if(opened) { CloseIfOpen(); return true; }
            if(gameplayMode==null || session==null || mapData==null || view==null || (!gameplayMode.AllowsMapToggle && gameplayMode.CurrentMode!=GameplayMode.Pause)) return false;
            if(!session.State.HasFlag(GameFlagId.CampusPlanRecovered)) { session.ShowMessage("MAPA NO DISPONIBLE. Recupera el plano en una terminal CCTV activa."); return false; }
            returnMode=gameplayMode.CurrentMode; previousTime=Time.timeScale; previousCursor=Cursor.visible; previousLock=Cursor.lockState; opened=true;
            gameplayMode.SetMode(GameplayMode.Map); Time.timeScale=0; Cursor.visible=true; Cursor.lockState=CursorLockMode.None;
            session.GetComponent<EscapeUNIFRANZ.UI.EntranceTutorialController>()?.Complete(EscapeUNIFRANZ.UI.TutorialAction.Map);
            view.Show(mapData,session.State); view.BindClose(CloseIfOpen); ProjectAudioHooks.Emit(AudioCue.MapOpen); return true;
        }
        private void OnToggleMapPressed() { Toggle(); }
        public void CloseIfOpen()
        {
            if(!opened) return; opened=false; view?.Hide(); Time.timeScale=previousTime; Cursor.visible=previousCursor; Cursor.lockState=previousLock;
            if(gameplayMode.CurrentMode==GameplayMode.Map) gameplayMode.SetMode(returnMode); ProjectAudioHooks.Emit(AudioCue.MapClose);
        }
        private void OnModeChanged(GameplayMode next) { if(opened && next!=GameplayMode.Map) { opened=false; view?.Hide(); if(gameplayMode.AllowsMovement) { Time.timeScale=previousTime; Cursor.visible=previousCursor; Cursor.lockState=previousLock; } } }
    }
}
