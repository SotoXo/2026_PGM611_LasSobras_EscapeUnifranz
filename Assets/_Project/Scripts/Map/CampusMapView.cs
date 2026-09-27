using System.Collections.Generic;
using EscapeUNIFRANZ.Core;
using UnityEngine;

namespace EscapeUNIFRANZ.Map
{
    /// <summary>
    /// Renders the schematic campus map. Connections are static UI lines in the Canvas.
    /// </summary>
    public sealed class CampusMapView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private List<CampusMapNodeView> nodeViews = new List<CampusMapNodeView>();

        public bool IsVisible => root != null && root.activeSelf;

        private void Awake()
        {
            Hide();
        }

        public void Show(CampusMapData data, GameState state)
        {
            if (root == null || data == null || state == null)
            {
                Hide();
                return;
            }

            for (int index = 0; index < data.Nodes.Count; index++)
            {
                CampusMapNodeDefinition definition = data.Nodes[index];
                CampusMapNodeView view = FindView(definition.ZoneId);
                view?.Render(definition.DisplayName, CampusMapProgression.Resolve(state, definition));
            }

            root.SetActive(true);
        }

        public void Hide()
        {
            root?.SetActive(false);
        }

        private CampusMapNodeView FindView(string zoneId)
        {
            for (int index = 0; index < nodeViews.Count; index++)
            {
                if (nodeViews[index] != null && nodeViews[index].ZoneId == zoneId)
                {
                    return nodeViews[index];
                }
            }

            return null;
        }
    }
}
