using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Input;
using UnityEngine;

namespace EscapeUNIFRANZ.Map
{
    /// <summary>
    /// Opens the informational campus map and owns the Map gameplay mode transition.
    /// </summary>
    public sealed class CampusMapController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private GameplayModeController gameplayMode;
        [SerializeField] private GameSessionController session;
        [SerializeField] private CampusMapData mapData;
        [SerializeField] private CampusMapView view;

        private void OnEnable()
        {
            if (inputReader != null)
            {
                inputReader.ToggleMapPressed += OnToggleMapPressed;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.ToggleMapPressed -= OnToggleMapPressed;
            }

            view?.Hide();
        }

        public bool Toggle()
        {
            if (gameplayMode == null || session == null || mapData == null || view == null)
            {
                return false;
            }

            if (gameplayMode.CurrentMode == GameplayMode.Map)
            {
                view.Hide();
                gameplayMode.SetMode(GameplayMode.Explore);
                return true;
            }

            if (!gameplayMode.AllowsMapToggle)
            {
                return false;
            }

            gameplayMode.SetMode(GameplayMode.Map);
            view.Show(mapData, session.State);
            return true;
        }

        private void OnToggleMapPressed()
        {
            Toggle();
        }

        public void CloseIfOpen()
        {
            if (gameplayMode != null && gameplayMode.CurrentMode == GameplayMode.Map)
            {
                view?.Hide();
                gameplayMode.SetMode(GameplayMode.Explore);
            }
        }
    }
}
