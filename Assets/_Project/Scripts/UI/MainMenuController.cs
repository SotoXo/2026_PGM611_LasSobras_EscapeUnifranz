using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Narrative;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.UI
{
    /// <summary>
    /// Controls the standalone entry menu and hands a new-game request to Bootstrap.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string NewGameRequestKey = "EscapeUNIFRANZ.NewGameRequested";

        [SerializeField] private string bootstrapScenePath =
            "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject controlsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private Button optionsBackButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button controlsBackButton;
        [SerializeField] private Button creditsBackButton;

        private static bool newGameRequested;
        private static bool continueRequested;
        private bool isLoading;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            newGameRequested = false;
            continueRequested = false;
        }

        private void Awake()
        {
            // A stale request can only remain if Play Mode or the application ended
            // between clicking the button and loading Bootstrap.
            PlayerPrefs.DeleteKey(NewGameRequestKey);
            Add(newGameButton, StartNewGame);
            Add(continueButton, ContinueGame);
            Add(optionsButton, ShowOptions);
            Add(optionsBackButton, ShowMain);
            Add(controlsButton, ShowControls);
            Add(creditsButton, ShowCredits);
            Add(quitButton, QuitGame);
            Add(controlsBackButton, ShowMain);
            Add(creditsBackButton, ShowMain);
            ShowMain();
        }

        private void Start() => ProjectAudioHooks.SetSceneMusic(AudioCue.MusicMenu);

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame &&
                (IsVisible(controlsPanel) || IsVisible(creditsPanel) || IsVisible(optionsPanel)))
            {
                ShowMain();
            }
        }

        private void OnDestroy()
        {
            Remove(newGameButton, StartNewGame);
            Remove(continueButton, ContinueGame);
            Remove(optionsButton, ShowOptions);
            Remove(optionsBackButton, ShowMain);
            Remove(controlsButton, ShowControls);
            Remove(creditsButton, ShowCredits);
            Remove(quitButton, QuitGame);
            Remove(controlsBackButton, ShowMain);
            Remove(creditsBackButton, ShowMain);
        }

        public static bool ConsumeNewGameRequest()
        {
            bool requested = newGameRequested || PlayerPrefs.GetInt(NewGameRequestKey, 0) == 1;
            newGameRequested = false;
            if (requested)
            {
                PlayerPrefs.DeleteKey(NewGameRequestKey);
                PlayerPrefs.Save();
            }
            return requested;
        }

        public static bool ConsumeContinueRequest()
        {
            bool requested = continueRequested;
            continueRequested = false;
            return requested;
        }

        public void ContinueGame()
        {
            if (isLoading || !CanContinue()) return;

            ProjectAudioHooks.Emit(AudioCue.Button);
            isLoading = true;
            continueRequested = true;
            SetButtonsInteractable(false);
            SceneManager.LoadScene(bootstrapScenePath, LoadSceneMode.Single);
        }

        private static bool CanContinue() =>
            CheckpointSave.TryLoad(out var state) && !state.HasFlag(GameFlagId.GameCompleted);

        public void StartNewGame()
        {
            if (isLoading)
            {
                return;
            }

            ProjectAudioHooks.Emit(AudioCue.Button);
            isLoading = true;
            newGameRequested = true;
            PlayerPrefs.SetInt(NewGameRequestKey, 1);
            PlayerPrefs.Save();
            SetButtonsInteractable(false);
            SceneManager.LoadScene(bootstrapScenePath, LoadSceneMode.Single);
        }

        public void ShowControls()
        {
            ShowPanel(controlsPanel, controlsBackButton);
        }

        public void ShowOptions() => ShowPanel(optionsPanel, optionsBackButton);

        public void ShowCredits()
        {
            ShowPanel(creditsPanel, creditsBackButton);
        }

        public void ShowMain()
        {
            if (IsVisible(optionsPanel)) GameOptions.Save();
            SetActive(mainPanel, true);
            SetActive(controlsPanel, false);
            SetActive(creditsPanel, false);
            SetActive(optionsPanel, false);
            bool canContinue = CanContinue();
            if (continueButton != null)
            {
                continueButton.interactable = canContinue && !isLoading;
                continueButton.GetComponent<MenuButtonFeedback>()?.Refresh();
            }
            Select(newGameButton);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowPanel(GameObject panel, Button defaultButton)
        {
            ProjectAudioHooks.Emit(AudioCue.Button);
            SetActive(mainPanel, false);
            SetActive(controlsPanel, panel == controlsPanel);
            SetActive(creditsPanel, panel == creditsPanel);
            SetActive(optionsPanel, panel == optionsPanel);
            Select(defaultButton);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (newGameButton != null) newGameButton.interactable = interactable;
            if (continueButton != null) continueButton.interactable = interactable;
            if (optionsButton != null) optionsButton.interactable = interactable;
            if (controlsButton != null) controlsButton.interactable = interactable;
            if (creditsButton != null) creditsButton.interactable = interactable;
            if (quitButton != null) quitButton.interactable = interactable;
        }

        private static void Select(Button button)
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(
                    button != null && button.isActiveAndEnabled
                        ? button.gameObject
                        : null);
            }
        }

        private static bool IsVisible(GameObject target)
        {
            return target != null && target.activeSelf;
        }

        private static void SetActive(GameObject target, bool active)
        {
            target?.SetActive(active);
        }

        private static void Add(Button button, UnityEngine.Events.UnityAction action)
        {
            button?.onClick.AddListener(action);
        }

        private static void Remove(Button button, UnityEngine.Events.UnityAction action)
        {
            button?.onClick.RemoveListener(action);
        }
    }
}
