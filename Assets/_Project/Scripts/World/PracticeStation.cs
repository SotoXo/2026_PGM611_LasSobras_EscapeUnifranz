using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.UI;

namespace EscapeUNIFRANZ.World
{
    public sealed class PracticeStation : InteractableBehaviour, IZoneRuntimeInitializable
    {
        public MiniAutoController target;
        private GameFlowController flow;
        private NarrativeController narrative;
        private PracticeStage? Stage => BoundSession?.Tutorial?.TrainingStage;

        public override string Prompt => target != null
            ? "[E] Reponer latas de práctica"
            : Stage == PracticeStage.Exit ? "[E] Salir hacia recepción" : "[E] Leer terminal de emergencia";

        public void Initialize(ZoneRuntimeContext context)
        {
            flow = context.Session.GetComponent<GameFlowController>();
            narrative = context.Session.GetComponent<NarrativeController>();
            if (target == null) ProjectAudioHooks.SetSceneMusic(AudioCue.MusicTension, true);
        }

        protected override void PerformInteraction()
        {
            if (BoundSession?.IsPractice != true) return;
            if (target != null)
            {
                if (target.HitsRemaining == 0 && !target.gameObject.activeSelf)
                    target.ResetEncounter();
                var state = BoundSession.State;
                if (!state.HasItem("soda")) state.AddItem("soda");
                if (!state.HasItem(ThrowableCanController.EmptyCanItemId))
                    state.AddItem(ThrowableCanController.EmptyCanItemId);
                BoundSession.ShowMessage("Latas de práctica disponibles. Selecciona una en Inventario.");
            }
            else if (Stage == PracticeStage.Exit)
                flow?.CompletePractice();
            else if (Stage == PracticeStage.Instructions)
                narrative?.ShowTrainingBriefing();
            else
                BoundSession.ShowMessage("Sigue el objetivo del HUD o elige Ir al nivel 1 en Pausa.");
        }
    }
}
