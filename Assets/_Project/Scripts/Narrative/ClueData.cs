using EscapeUNIFRANZ.Core;
using UnityEngine;
namespace EscapeUNIFRANZ.Narrative
{
    [CreateAssetMenu(menuName="Escape UNIFRANZ/Narrative/Clue")]
    public sealed class ClueData : ScriptableObject
    {
        public string id, title, category;
        [TextArea(3,12)] public string text;
        public GameFlagId associatedFlag;
    }
}
