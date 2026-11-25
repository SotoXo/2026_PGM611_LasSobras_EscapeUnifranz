using UnityEngine;

namespace EscapeUNIFRANZ.UI
{
    public enum PracticeStage { Movement, Pickup, Inventory, Flashlight, Cans, Instructions, Exit }

    public sealed class PracticeProgress
    {
        private bool moved, collected, inventoryOpened, examined, selected, inventoryClosed;
        private bool turnedOn, turnedOff, fullImpact, emptyImpact, briefed;
        public bool SawFullImpact => fullImpact;
        public bool SawEmptyImpact => emptyImpact;
        public bool TurnedOn => turnedOn;

        public PracticeStage Stage
        {
            get
            {
                if (!moved) return PracticeStage.Movement;
                if (!collected) return PracticeStage.Pickup;
                if (!inventoryClosed) return PracticeStage.Inventory;
                if (!turnedOff) return PracticeStage.Flashlight;
                if (!fullImpact || !emptyImpact) return PracticeStage.Cans;
                return briefed ? PracticeStage.Exit : PracticeStage.Instructions;
            }
        }

        public void ObserveMovement(Vector2 origin, Vector2 position) => moved |= (position - origin).sqrMagnitude >= .64f;
        public void CollectFlashlight() => collected = true;
        public void OpenInventory() => inventoryOpened = true;
        public void ExamineItem() => examined |= inventoryOpened;
        public void SelectItem() => selected |= inventoryOpened;
        public void CloseInventory()
        {
            inventoryClosed |= inventoryOpened && examined && selected;
            inventoryOpened = false;
        }

        public void ToggleFlashlight(bool on)
        {
            turnedOn |= on;
            turnedOff |= turnedOn && !on;
        }

        public void HitRobot(bool full, bool chosen)
        {
            if (!chosen) return;
            if (full) fullImpact = true;
            else emptyImpact = true;
        }

        public void ConfirmBriefing()
        {
            if (Stage == PracticeStage.Instructions) briefed = true;
        }
    }
}
