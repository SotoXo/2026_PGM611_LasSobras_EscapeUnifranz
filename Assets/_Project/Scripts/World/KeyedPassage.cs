using EscapeUNIFRANZ.Core;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class KeyedPassage : MonoBehaviour, IZoneRuntimeInitializable
    {
        private GameSessionController session;
        private GameState state;
        private BoxCollider2D barrier;

        public void Initialize(ZoneRuntimeContext context)
        {
            if (session != null) session.StateChanged -= Bind;
            session = context.Session;
            session.StateChanged += Bind;
            barrier = GetComponent<BoxCollider2D>();
            Bind(session.State);
        }

        private void Bind(GameState next)
        {
            if (state != null) state.InventoryChanged -= Refresh;
            state = next;
            if (state != null) state.InventoryChanged += Refresh;
            Refresh();
        }

        private void Refresh() => barrier.enabled = state?.HasItem("key") != true;

        private void OnDestroy()
        {
            if (session != null) session.StateChanged -= Bind;
            if (state != null) state.InventoryChanged -= Refresh;
        }
    }
}
