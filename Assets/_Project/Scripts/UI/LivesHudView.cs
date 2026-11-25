using System.Collections;
using EscapeUNIFRANZ.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.UI
{
    [RequireComponent(typeof(Image),typeof(CanvasGroup))]
    public sealed class LivesHudView : MonoBehaviour
    {
        [SerializeField] private Texture2D atlas;
        [SerializeField] private int[] milliseconds;
        private readonly Sprite[] frames=new Sprite[32];
        private Image image;
        private CanvasGroup group;
        private GameSessionController session;
        private GameFlowController flow;
        private GameState state;
        private Coroutine animationRoutine;

        private void Awake()
        {
            image=GetComponent<Image>();image.raycastTarget=false;image.preserveAspect=true;
            group=GetComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            if(atlas==null)return;
            for(int i=0;i<32;i++) frames[i]=Sprite.Create(atlas,
                new Rect((i%8)*160,atlas.height-(i/8+1)*72,160,72),new Vector2(.5f,.5f),100);
            image.sprite=frames[0];
        }
        private void OnEnable()
        {
            session=GetComponentInParent<GameSessionController>();
            if(session==null)session=FindFirstObjectByType<GameSessionController>();
            flow=FindFirstObjectByType<GameFlowController>();
            if(session!=null){session.StateChanged+=Bind;Bind(session.State);}
        }
        private void Bind(GameState next)
        {
            if(state!=null)state.LivesChanged-=Changed;
            if(animationRoutine!=null)StopCoroutine(animationRoutine);
            animationRoutine=null;state=next;
            if(state!=null){state.LivesChanged+=Changed;image.sprite=frames[RestFrame(state.Lives)];}
        }
        public static int RestFrame(int lives)=>lives>=5 ? 0 : lives<=0 ? 31 : (5-lives)*6;
        private void Changed()
        {
            if(animationRoutine!=null)StopCoroutine(animationRoutine);
            if(state.Lives==5){image.sprite=frames[0];animationRoutine=null;return;}
            animationRoutine=StartCoroutine(Animate(state.Lives));
        }
        private IEnumerator Animate(int remaining)
        {
            int first=(4-remaining)*6+1;
            for(int i=first;i<=first+5;i++)
            {
                image.sprite=frames[i];
                yield return new WaitForSecondsRealtime(milliseconds!=null&&milliseconds.Length>i ? milliseconds[i]/1000f : .09f);
            }
            image.sprite=frames[RestFrame(remaining)];animationRoutine=null;
        }
        private void LateUpdate()
        {
            group.alpha=flow!=null&&(flow.CurrentScreen==PresentationScreen.Gameplay||flow.CurrentScreen==PresentationScreen.Defeat) ? 1 : 0;
            var rect=(RectTransform)transform;var parent=rect.parent as RectTransform;
            if(parent==null||Screen.width<=0||Screen.height<=0)return;
            var safe=Screen.safeArea;
            rect.anchoredPosition=new Vector2(-24-(Screen.width-safe.xMax)*parent.rect.width/Screen.width,
                (Application.isMobilePlatform ? 245 : 24)+safe.yMin*parent.rect.height/Screen.height);
        }
        private void OnDisable()
        {
            if(session!=null)session.StateChanged-=Bind;
            if(state!=null)state.LivesChanged-=Changed;
            if(animationRoutine!=null)StopCoroutine(animationRoutine);
            animationRoutine=null;
        }
        private void OnDestroy(){foreach(var sprite in frames)if(sprite!=null)Destroy(sprite);}
    }
}
