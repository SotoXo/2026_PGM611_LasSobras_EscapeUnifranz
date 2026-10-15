using System.Linq;
using EscapeUNIFRANZ.Core;
using UnityEngine;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed partial class NarrativeController
    {
        private string inspectingItem;
        private bool examiningItem;
        private float pickupToastUntil,inventoryExplanationUntil;
        public void ShowInventory()
        {
            if (!Open(GameplayMode.Inventory)) return;
            inspectingItem=state.SelectedItemId; examiningItem=false; InventoryPage(); ProjectAudioHooks.Emit(AudioCue.InventoryOpen);
            session.GetComponent<EscapeUNIFRANZ.UI.EntranceTutorialController>()?.Complete(EscapeUNIFRANZ.UI.TutorialAction.Inventory);
            if(state.CurrentZoneId=="entrada" && state.DiscoverWorldItem("onboarding","inventory_first_open")) { view.Feedback("Selecciona un objeto para examinarlo.",false); inventoryExplanationUntil=Time.unscaledTime+4f; }
        }
        private void InventoryPage()
        {
            examiningItem=false;
            var items=state.CarriedItems.Distinct().Select(ItemData.Find).Where(i=>i!=null).ToArray();
            if (!items.Any(i=>i.id==inspectingItem)) inspectingItem=items.FirstOrDefault()?.id;
            view.ShowInventory(items,state,inspectingItem,id=>{inspectingItem=id; InventoryPage();},
                ()=>{state.SelectItem(inspectingItem); InventoryPage();},
                ()=>{ var data=ItemData.Find(inspectingItem); if (data==null) return; examiningItem=true; view.ShowItemExamination(data); },
                ()=>{ var data=ItemData.Find(inspectingItem); if (data?.immediateUse==ImmediateItemUse.Flashlight) ToggleFlashlight(); InventoryPage(); },
                ()=>{state.SelectItem(null); InventoryPage();});
        }
        public void ToggleFlashlight()
        {
            if (state==null || !state.ToggleFlashlight()) { session.ShowMessage("No llevas una linterna."); return; }
            session.GetComponent<EscapeUNIFRANZ.UI.EntranceTutorialController>()?.Complete(EscapeUNIFRANZ.UI.TutorialAction.Flashlight);
            ProjectAudioHooks.Emit(state.FlashlightOn ? AudioCue.FlashlightOn : AudioCue.FlashlightOff);
        }
        public void NotifyPickup(string id)
        {
            var data=ItemData.Find(id); pickupToastUntil=Time.unscaledTime+4.5f;
            string message="OBJETO OBTENIDO: "+ItemData.Label(id).ToUpperInvariant();
            var tutorial=session.GetComponent<EscapeUNIFRANZ.UI.EntranceTutorialController>();
            if(id=="battery") { message="BATERÍA PORTÁTIL OBTENIDA"; tutorial?.Offer(EscapeUNIFRANZ.UI.TutorialAction.Inventory); }
            if(id=="flashlight") { message="LINTERNA OBTENIDA"; tutorial?.Offer(EscapeUNIFRANZ.UI.TutorialAction.Flashlight); }
            if (pickupLabel!=null) { pickupLabel.text=message; pickupLabel.gameObject.SetActive(true); }
            else session.ShowMessage(message);
            if (pickupIcon!=null) { pickupIcon.sprite=data?.icon; pickupIcon.gameObject.SetActive(data?.icon!=null); }
        }
        public void TeachOnce(string id,string message)
        {
            var tutorial=session.GetComponent<EscapeUNIFRANZ.UI.EntranceTutorialController>();
            if(tutorial==null || !tutorial.IsEntrance) return;
            if(id=="journal") tutorial.Offer(EscapeUNIFRANZ.UI.TutorialAction.Journal);
            else if(id=="map") tutorial.Offer(EscapeUNIFRANZ.UI.TutorialAction.Map);
            else if(id=="scanner") tutorial.Offer(EscapeUNIFRANZ.UI.TutorialAction.Scanner);
        }
    }
}
