using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.Map
{
    public sealed class CampusMapNodeView : MonoBehaviour
    {
        [SerializeField] private string zoneId;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;

        public string ZoneId => zoneId;

        public void Render(string displayName, CampusMapNodeState state)
        {
            if (label != null)
            {
                label.text = $"{PrefixFor(state)} {displayName}";
            }

            if (background != null)
            {
                background.color = ColorFor(state);
            }
        }

        private static string PrefixFor(CampusMapNodeState state)
        {
            switch (state)
            {
                case CampusMapNodeState.Current: return ">";
                case CampusMapNodeState.Completed: return "OK";
                case CampusMapNodeState.Locked: return "X";
                case CampusMapNodeState.Unknown: return "?";
                default: return "·";
            }
        }

        private static Color ColorFor(CampusMapNodeState state)
        {
            switch (state)
            {
                case CampusMapNodeState.Current: return new Color(0.15f, 0.65f, 0.95f, 0.95f);
                case CampusMapNodeState.Completed: return new Color(0.18f, 0.65f, 0.32f, 0.9f);
                case CampusMapNodeState.Locked: return new Color(0.35f, 0.35f, 0.4f, 0.9f);
                case CampusMapNodeState.Unknown: return new Color(0.18f, 0.18f, 0.22f, 0.75f);
                default: return new Color(0.9f, 0.62f, 0.18f, 0.9f);
            }
        }
    }
}
