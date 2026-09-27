using System;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.World;
using UnityEngine;

namespace EscapeUNIFRANZ.Hazards
{
    public sealed class DisableByFlag : MonoBehaviour, IZoneRuntimeInitializable
    {
        [SerializeField] private GameFlagId disabledFlag;
        [SerializeField] private Behaviour[] behaviours;
        [SerializeField] private GameObject[] objectsToHide;

        private GameState state;

        public void Initialize(ZoneRuntimeContext context)
        {
            state = context.Session.State;
            state.FlagChanged += OnFlagChanged;
            Apply();
        }

        private void OnDestroy()
        {
            if (state != null)
            {
                state.FlagChanged -= OnFlagChanged;
            }
        }

        private void OnFlagChanged(GameFlagId flag)
        {
            if (flag == disabledFlag)
            {
                Apply();
            }
        }

        private void Apply()
        {
            bool disabled = IsDisabled(state, disabledFlag);
            Array.ForEach(behaviours ?? Array.Empty<Behaviour>(), item =>
            {
                if (item != null)
                {
                    item.enabled = !disabled;
                }
            });
            Array.ForEach(objectsToHide ?? Array.Empty<GameObject>(), item =>
            {
                if (item != null)
                {
                    item.SetActive(!disabled);
                }
            });
        }

        public static bool IsDisabled(GameState gameState, GameFlagId flag)
        {
            return gameState != null && flag != GameFlagId.None && gameState.HasFlag(flag);
        }
    }
}
