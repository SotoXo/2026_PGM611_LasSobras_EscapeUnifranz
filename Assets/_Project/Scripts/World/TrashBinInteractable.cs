using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Player;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    public sealed class TrashBinInteractable : InteractableBehaviour, IZoneRuntimeInitializable, IScanStateProvider
    {
        public const int Capacity=6;
        [SerializeField] private string binId;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Sprite[] remainingFrames;
        private int displayed=-1;
        public int Remaining
        {
            get
            {
                int count=Capacity;
                if(BoundSession?.State!=null)
                    for(int i=0;i<Capacity;i++)if(BoundSession.State.IsWorldItemDiscovered("trash_bin",binId+":"+i))count--;
                return count;
            }
        }
        public override bool CanInteract=>base.CanInteract && BoundSession?.State!=null && !string.IsNullOrWhiteSpace(binId);
        public override string Prompt=>Remaining>0 ? "[E] Sacar lata vacía ("+Remaining+" / 6)" : "Basurero sin latas";
        public string ScanStateText=>"[BASURERO · "+Remaining+" LATAS VACÍAS]";
        public void Initialize(ZoneRuntimeContext context)=>Refresh();
        private void LateUpdate(){if(Remaining!=displayed)Refresh();}
        private void Refresh()
        {
            displayed=Remaining;
            if(body!=null && remainingFrames!=null && remainingFrames.Length>displayed)body.sprite=remainingFrames[displayed];
        }
        protected override void PerformInteraction()
        {
            for(int i=0;i<Capacity;i++)
            {
                if(!BoundSession.State.DiscoverWorldItem("trash_bin",binId+":"+i))continue;
                BoundSession.State.AddItem(ThrowableCanController.EmptyCanItemId);
                var item=ItemData.Find(ThrowableCanController.EmptyCanItemId);
                InteractionFx.Pickup(item?.icon,transform.position,"Lata vacía");
                ProjectAudioHooks.Emit(AudioCue.Pickup);
                BoundSession.ShowMessage("LATA VACÍA · G: lanzar para atacar. No deja charcos.");
                Refresh();return;
            }
            BoundSession.ShowMessage("Este basurero ya no tiene latas.");
        }
    }
}
