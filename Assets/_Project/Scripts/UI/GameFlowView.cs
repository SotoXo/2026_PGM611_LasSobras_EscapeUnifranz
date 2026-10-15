using System.Collections;
using EscapeUNIFRANZ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.UI
{
    /// <summary>
    /// One persistent view for mutually exclusive application screens.
    /// </summary>
    public sealed class GameFlowView : MonoBehaviour
    {
        private static readonly string[] IntroTitles =
        {
            "PRIMER DÍA",
            "CORTE DE ENERGÍA",
            "UN ACCESO CERRADO"
        };

        private static readonly string[] IntroBodies =
        {
            "Primer día. Ingeniería de Sistemas. El edificio está casi vacío. Tú también estás empezando a aprender.",
            "Un apagón. La terminal intenta reiniciarse: SISTEMA DE EMERGENCIA ACTIVO. Después: ERROR DE COMUNICACIÓN.",
            "El acceso no responde. Una terminal permanece apagada junto a recepción. Busca una forma de devolverle energía."
        };

        [Header("Layers")]
        [SerializeField] private GameObject gameplayHudRoot;
        [SerializeField] private GameObject introRoot;
        [SerializeField] private GameObject loadingRoot;
        [SerializeField] private GameObject pauseRoot;
        [SerializeField] private GameObject controlsRoot;
        [SerializeField] private GameObject defeatRoot;
        [SerializeField] private GameObject victoryRoot;
        [SerializeField] private GameObject creditsRoot;

        [Header("Dynamic copy")]
        [SerializeField] private TMP_Text introTitle;
        [SerializeField] private TMP_Text introBody;
        [SerializeField] private TMP_Text introCounter;
        [SerializeField] private TMP_Text defeatReason;

        [Header("Intro")]
        [SerializeField] private Button introNextButton;
        [SerializeField] private Button introSkipButton;

        [Header("Pause")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button pauseControlsButton;
        [SerializeField] private Button pauseMenuButton;
        [SerializeField] private Button pauseInventoryButton, pauseMapButton, pauseJournalButton;

        [Header("Controls")]
        [SerializeField] private Button controlsBackButton;

        [Header("Defeat")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button defeatMenuButton;

        [Header("Victory")]
        [SerializeField] private Button victoryCreditsButton;
        [SerializeField] private Button victoryMenuButton;

        [Header("Credits")]
        [SerializeField] private Button creditsMenuButton;

        private GameFlowController controller;
        private Coroutine selectionRoutine;

        public void Bind(GameFlowController target)
        {
            if (controller == target)
            {
                return;
            }

            if (controller != null)
            {
                Unbind(controller);
            }

            controller = target;
            if (controller == null)
            {
                return;
            }

            Add(introNextButton, controller.ContinueIntro);
            Add(introSkipButton, controller.SkipIntro);
            Add(resumeButton, controller.ResumeGame);
            Add(pauseRestartButton, controller.RestartCheckpointFromPause);
            Add(pauseControlsButton, controller.OpenControls);
            Add(pauseMenuButton, controller.ReturnToMainMenu);
            Add(pauseInventoryButton, controller.OpenInventory);
            Add(pauseMapButton, controller.OpenMap);
            Add(pauseJournalButton, controller.OpenJournal);
            Add(controlsBackButton, controller.CloseControls);
            Add(retryButton, controller.RetryCheckpoint);
            Add(defeatMenuButton, controller.ReturnToMainMenu);
            Add(victoryCreditsButton, controller.OpenCredits);
            Add(victoryMenuButton, controller.ReturnToMainMenu);
            Add(creditsMenuButton, controller.ReturnToMainMenu);

        }

        public void Unbind(GameFlowController target)
        {
            if (controller != target || controller == null)
            {
                return;
            }

            Remove(introNextButton, controller.ContinueIntro);
            Remove(introSkipButton, controller.SkipIntro);
            Remove(resumeButton, controller.ResumeGame);
            Remove(pauseRestartButton, controller.RestartCheckpointFromPause);
            Remove(pauseControlsButton, controller.OpenControls);
            Remove(pauseMenuButton, controller.ReturnToMainMenu);
            Remove(pauseInventoryButton, controller.OpenInventory);
            Remove(pauseMapButton, controller.OpenMap);
            Remove(pauseJournalButton, controller.OpenJournal);
            Remove(controlsBackButton, controller.CloseControls);
            Remove(retryButton, controller.RetryCheckpoint);
            Remove(defeatMenuButton, controller.ReturnToMainMenu);
            Remove(victoryCreditsButton, controller.OpenCredits);
            Remove(victoryMenuButton, controller.ReturnToMainMenu);
            Remove(creditsMenuButton, controller.ReturnToMainMenu);
            controller = null;
        }

        public void Render(
            PresentationScreen screen,
            int introPanelIndex,
            int introPanelCount,
            string reason)
        {
            SetActive(introRoot, screen == PresentationScreen.Intro);
            SetActive(loadingRoot, screen == PresentationScreen.Loading);
            SetActive(pauseRoot, screen == PresentationScreen.Pause);
            SetActive(controlsRoot, screen == PresentationScreen.Controls);
            SetActive(defeatRoot, screen == PresentationScreen.Defeat);
            SetActive(victoryRoot, screen == PresentationScreen.Victory);
            SetActive(creditsRoot, screen == PresentationScreen.Credits);
            SetActive(gameplayHudRoot, screen == PresentationScreen.Gameplay);
            if(screen==PresentationScreen.Pause && pauseMapButton!=null)
            { pauseMapButton.interactable=controller!=null && controller.HasMapAccess; var label=pauseMapButton.GetComponentInChildren<TMP_Text>(true); if(label!=null) label.text=pauseMapButton.interactable ? "MAPA" : "MAPA NO DISPONIBLE"; }

            if (screen == PresentationScreen.Intro)
            {
                int index = Mathf.Clamp(introPanelIndex, 0, IntroBodies.Length - 1);
                if (introTitle != null) introTitle.text = IntroTitles[index];
                if (introBody != null) introBody.text = IntroBodies[index];
                if (introCounter != null)
                {
                    introCounter.text = $"REGISTRO {index + 1:00} / {introPanelCount:00}";
                }
            }

            if (defeatReason != null)
            {
                defeatReason.text = string.IsNullOrWhiteSpace(reason)
                    ? "El sistema de seguridad neutralizó al jugador."
                    : reason;
            }

            SelectDefaultButton(screen);
        }

        private void SelectDefaultButton(PresentationScreen screen)
        {
            if (selectionRoutine != null)
            {
                StopCoroutine(selectionRoutine);
            }

            ApplyDefaultSelection(screen);
            selectionRoutine = StartCoroutine(SelectDefaultButtonNextFrame(screen));
        }

        private IEnumerator SelectDefaultButtonNextFrame(PresentationScreen screen)
        {
            yield return null;
            selectionRoutine = null;
            ApplyDefaultSelection(screen);
        }

        private void ApplyDefaultSelection(PresentationScreen screen)
        {
            Button target = null;
            switch (screen)
            {
                case PresentationScreen.Intro:
                    target = introNextButton;
                    break;
                case PresentationScreen.Pause:
                    target = resumeButton;
                    break;
                case PresentationScreen.Controls:
                    target = controlsBackButton;
                    break;
                case PresentationScreen.Defeat:
                    target = retryButton;
                    break;
                case PresentationScreen.Victory:
                    target = victoryCreditsButton;
                    break;
                case PresentationScreen.Credits:
                    target = creditsMenuButton;
                    break;
            }

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            if (target != null && target.isActiveAndEnabled && target.interactable)
            {
                eventSystem.SetSelectedGameObject(target.gameObject);
            }
            else
            {
                eventSystem.SetSelectedGameObject(null);
            }
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
