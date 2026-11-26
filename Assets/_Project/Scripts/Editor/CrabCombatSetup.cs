using System.Linq;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EscapeUNIFRANZ.EditorTools
{
    public static class CrabCombatSetup
    {
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Zones/Z08_Piso3Aulas.unity",OpenSceneMode.Single);
            var zone=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ZoneContext>()).Single();
            var actors=zone.GetComponentsInChildren<MechanicalCrabController>();
            if(actors.Length==1){var obj=Object.Instantiate(actors[0].gameObject,zone.transform);obj.name="Cangrejo_Mecanico_2";obj.transform.position=new Vector2(35.1f,13.3f);}
            actors=zone.GetComponentsInChildren<MechanicalCrabController>();
            foreach(var actor in actors){actor.animationSpeed=1.75f;EditorUtility.SetDirty(actor);}
            var effect=zone.GetComponentInChildren<CrabRoomRupture>();effect.crabs=actors;effect.crab=actors[0];EditorUtility.SetDirty(effect);
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("CRAB_COMBAT_OK: two crabs, faster animations, shared staged room damage.");
        }
    }
}
