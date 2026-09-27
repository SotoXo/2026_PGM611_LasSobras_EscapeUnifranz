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
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button controlsBackButton;
        [SerializeField] private Button creditsBackButton;

        private static bool newGameRequested;
        private bool isLoading;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            newGameRequested = false;
        }

        private void Awake()
        {
            // A stale request can only remain if Play Mode or the application ended
            // between clicking the button and loading Bootstrap.
            PlayerPrefs.DeleteKey(NewGameRequestKey);
            Add(newGameButton, StartNewGame);
            Add(controlsButton, ShowControls);
            Add(creditsButton, ShowCredits);
            Add(quitButton, QuitGame);
            Add(controlsBackButton, ShowMain);
            Add(creditsBackButton, ShowMain);
            ShowMain();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame &&
                (IsVisible(controlsPanel) || IsVisible(creditsPanel)))
            {
                ShowMain();
            }
        }

        private void OnDestroy()
        {
            Remove(newGameButton, StartNewGame);
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

        public void StartNewGame()
        {
            if (isLoading)
            {
                return;
            }

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

        public void ShowCredits()
        {
            ShowPanel(creditsPanel, creditsBackButton);
        }

        public void ShowMain()
        {
            SetActive(mainPanel, true);
            SetActive(controlsPanel, false);
            SetActive(creditsPanel, false);
            Select(newGameButton);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            Debug.Log("Salir solicitado desde el menú. Application.Quit solo cierra una build.", this);
#else
            Application.Quit();
#endif
        }

        private void ShowPanel(GameObject panel, Button defaultButton)
        {
            SetActive(mainPanel, false);
            SetActive(controlsPanel, panel == controlsPanel);
            SetActive(creditsPanel, panel == creditsPanel);
            Select(defaultButton);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (newGameButton != null) newGameButton.interactable = interactable;
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
