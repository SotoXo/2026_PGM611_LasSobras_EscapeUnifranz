using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Player;
using UnityEngine;
namespace EscapeUNIFRANZ.World
{
    public sealed class MineInventoryController : MonoBehaviour
    {
        public GameObject minePrefab;public static MineInventoryController Instance{get;private set;}
        private void Awake()=>Instance=this;
        private void OnDestroy(){if(Instance==this)Instance=null;}
        public static bool CanCraft(GameState state)=>state!=null&&state.HasItem("battery")&&state.HasItem("electronics_box");
        public static bool TryCraft(GameState state)
        {
            if(!CanCraft(state))return false;
            state.TryUseItem("battery");state.TryUseItem("electronics_box");state.AddItem("crafted_mine");return true;
        }
        public bool TryPlace(GameSessionController session,SceneFlowController flow)
        {
            if(minePrefab==null||flow?.PlayerBody==null||session?.ActiveZone==null||session.State.HasItem("crafted_mine")!=true)return false;
            var vision=flow.PlayerBody.GetComponentInChildren<PlayerVisionController>();var dir=vision!=null?vision.CurrentDirection:PlayerVisualController.FacingDirection.Front;
            Vector2 facing=dir==PlayerVisualController.FacingDirection.Back?Vector2.up:dir==PlayerVisualController.FacingDirection.Left?Vector2.left:dir==PlayerVisualController.FacingDirection.Right?Vector2.right:Vector2.down;
            Vector2 point=flow.PlayerBody.position+facing*.65f;
            if(Physics2D.OverlapBoxAll(point+Vector2.up*.1f,new Vector2(.3f,.24f),0).AnySolidExcept(flow.PlayerBody)){session.ShowMessage("No hay espacio para colocar la mina.");return false;}
            if(!session.State.TryUseItem("crafted_mine"))return false;
            var obj=Instantiate(minePrefab,point,Quaternion.identity,session.ActiveZone.transform);obj.GetComponent<CraftedMine>().Bind(flow.ModeController);
            session.ShowMessage("Mina colocada. Se arma en un segundo y detecta robots.");return true;
        }
    }
    static class MineOverlapFilter
    {
        public static bool AnySolidExcept(this Collider2D[] colliders,Rigidbody2D player){foreach(var c in colliders)if(!c.isTrigger&&c.attachedRigidbody!=player)return true;return false;}
    }
}
