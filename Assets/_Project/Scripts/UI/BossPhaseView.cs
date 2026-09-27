using EscapeUNIFRANZ.Encounters;
using TMPro;
using UnityEngine;

namespace EscapeUNIFRANZ.UI
{
    public sealed class BossPhaseView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text label;

        public void Show(FrancisBossState state)
        {
            if (root == null || label == null)
            {
                return;
            }

            switch (state)
            {
                case FrancisBossState.PhaseNetwork:
                    label.text = "FASE 1/3 — RED";
                    break;
                case FrancisBossState.PhaseHardware:
                    label.text = "FASE 2/3 — HARDWARE";
                    break;
                case FrancisBossState.PhaseShutdown:
                    label.text = "FASE 3/3 — APAGADO";
                    break;
                case FrancisBossState.Completed:
                    label.text = "SISTEMA APAGADO DE FORMA SEGURA";
                    break;
                default:
                    label.text = "FRANCIS INTENTA DESTRUIR EL NÚCLEO";
                    break;
            }

            root.SetActive(true);
        }

        public void Hide()
        {
            root?.SetActive(false);
        }
    }
}
