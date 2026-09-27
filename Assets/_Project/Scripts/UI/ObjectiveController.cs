using EscapeUNIFRANZ.Core;
using UnityEngine;

namespace EscapeUNIFRANZ.UI
{
    /// <summary>
    /// Owns the single current objective command and updates its view.
    /// </summary>
    public sealed class ObjectiveController : MonoBehaviour
    {
        [SerializeField] private ObjectiveView view;

        private GameState state;
        private string objectiveText = string.Empty;

        public void Bind(GameState gameState)
        {
            state = gameState;
            objectiveText = string.Empty;
            view?.Hide();
        }

        public void SetCurrent(string objectiveId, string text)
        {
            SetCurrent(objectiveId, text, 0, 0);
        }

        public void SetCurrent(string objectiveId, string text, int progress, int total)
        {
            if (state == null)
            {
                Debug.LogError("ObjectiveController has not been bound to a GameState.", this);
                return;
            }

            objectiveText = text ?? string.Empty;
            state.SetCurrentObjective(objectiveId, progress, total);
            view?.Show(ObjectiveProgressFormatter.Format(objectiveText, progress, total));
        }

        public void SetProgress(int progress, int total)
        {
            if (state == null)
            {
                return;
            }

            state.SetObjectiveProgress(progress, total);
            view?.Show(ObjectiveProgressFormatter.Format(objectiveText, progress, total));
        }
    }
}
