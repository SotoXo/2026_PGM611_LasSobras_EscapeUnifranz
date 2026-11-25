using EscapeUNIFRANZ.Narrative;
using UnityEngine;

namespace EscapeUNIFRANZ.UI
{
    public sealed partial class EntranceTutorialController
    {
        public const string PracticeZoneId = "practica";
        public const string PracticeSpawnId = "practica_inicio";
        private PracticeProgress practice;
        private PracticeStage? displayedStage;
        public PracticeStage? TrainingStage => practice?.Stage;

        private void SetPracticeZone(string id)
        {
            practice = id == PracticeZoneId ? new PracticeProgress() : null;
            displayedStage = null;
        }

        private void UpdatePractice()
        {
            if (mode == null || !mode.AllowsMovement)
            {
                Hide();
                return;
            }
            if (!positionReady)
            {
                initialPosition = player.transform.position;
                positionReady = true;
            }
            if (practice.Stage == PracticeStage.Movement)
                practice.ObserveMovement(initialPosition, player.transform.position);

            PracticeStage stage = practice.Stage;
            if (displayedStage != stage)
            {
                displayedStage = stage;
                session.Objectives?.SetCurrent("practica_" + stage, Objective(stage));
                ProjectAudioHooks.Emit(AudioCue.TutorialNotification);
            }
            if (label == null) return;
            string caption = Instruction(stage);
            if (label.text != caption) label.text = caption;
            if (!label.gameObject.activeSelf) label.gameObject.SetActive(true);
        }

        private static string Objective(PracticeStage stage)
        {
            switch (stage)
            {
                case PracticeStage.Movement: return "Muévete por la sala de calibración";
                case PracticeStage.Pickup: return "Recoge la linterna de práctica";
                case PracticeStage.Inventory: return "Examina y selecciona la linterna";
                case PracticeStage.Flashlight: return "Enciende y apaga la linterna";
                case PracticeStage.Cans: return "Prueba las dos latas con el robot";
                case PracticeStage.Instructions: return "Consulta la terminal de emergencia";
                default: return "Confirma tu salida en la terminal";
            }
        }

        private string Instruction(PracticeStage stage)
        {
            bool touch = Application.isMobilePlatform;
            switch (stage)
            {
                case PracticeStage.Movement: return touch ? "Desplázate con la palanca táctil" : "Desplázate con WASD o las flechas";
                case PracticeStage.Pickup: return touch ? "Acércate a la linterna y toca Interactuar" : "Acércate a la linterna y pulsa E";
                case PracticeStage.Inventory: return touch ? "Toca Inventario. Examina, vuelve, selecciona y cierra."
                    : "Abre con I. Examina, vuelve, selecciona y cierra.";
                case PracticeStage.Flashlight: return (touch ? "Toca Linterna" : "Pulsa F") +
                    (practice.TurnedOn ? " para apagarla" : " para ver el rincón oscuro");
                case PracticeStage.Cans:
                    return (!practice.SawFullImpact ? "Selecciona la llena: inmoviliza. " : "Selecciona la vacía: daña. ") +
                        (touch ? "Mira al robot y toca Lanzar." : "Mira al robot y pulsa G.");
                case PracticeStage.Instructions: return "El HUD muestra tu objetivo. Lee la terminal de emergencia.";
                default: return touch ? "Toca Interactuar en la terminal para continuar" : "Pulsa E en la terminal para continuar";
            }
        }

        public void PracticeCollectFlashlight() => practice?.CollectFlashlight();
        public void PracticeOpenInventory() => practice?.OpenInventory();
        public void PracticeExamineItem() => practice?.ExamineItem();
        public void PracticeSelectItem() => practice?.SelectItem();
        public void PracticeCloseInventory() => practice?.CloseInventory();
        public void PracticeFlashlight(bool on) => practice?.ToggleFlashlight(on);
        public void PracticeCanHit(bool full, bool chosen) => practice?.HitRobot(full, chosen);
        public void PracticeConfirmBriefing() => practice?.ConfirmBriefing();
    }
}
