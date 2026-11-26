using UnityEngine;
namespace EscapeUNIFRANZ.Encounters
{
    public sealed class CrabHealthBar : MonoBehaviour
    {
        public MechanicalCrabController target;private Transform fill;private SpriteRenderer color;private static Sprite square;
        private void Awake()
        {
            if(square==null){var texture=new Texture2D(2,2);texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});texture.Apply();square=Sprite.Create(texture,new Rect(0,0,2,2),new Vector2(0,.5f),2);}
            var back=new GameObject("Vida_cangrejo");back.transform.SetParent(transform,false);back.transform.localPosition=new Vector3(-.65f,1.65f);
            var background=back.AddComponent<SpriteRenderer>();background.sprite=square;background.color=new Color(.12f,.02f,.02f);background.sortingLayerName="Effects";back.transform.localScale=new Vector3(1.3f,.12f,1);
            var front=new GameObject("Relleno");fill=front.transform;fill.SetParent(back.transform,false);fill.localPosition=Vector3.back*.01f;color=front.AddComponent<SpriteRenderer>();color.sprite=square;color.sortingLayerName="Effects";color.sortingOrder=1;
        }
        private void LateUpdate(){if(fill==null||target==null)return;fill.localScale=new Vector3(target.HitsRemaining/3f,1,1);color.color=target.HitsRemaining>1?new Color(.3f,.9f,.35f):new Color(1,.2f,.1f);}
    }
}
