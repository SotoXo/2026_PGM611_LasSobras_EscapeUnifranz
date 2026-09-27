using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Input;
using EscapeUNIFRANZ.World;
using UnityEngine;

namespace EscapeUNIFRANZ.Interaction
{
    public sealed class ScannerController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private GameplayModeController gameplayMode;
        [SerializeField] private Transform player;
        [SerializeField] private ScannerView view;
        [SerializeField, Min(0.1f)] private float scanRadius = 5f;
        [SerializeField, Min(0.1f)] private float scanSeconds = 2.5f;
        [SerializeField, Min(0.1f)] private float cooldownSeconds = 4f;

        private ScannerTimer timer;
        private Scannable2D[] zoneTargets = System.Array.Empty<Scannable2D>();

        private void Awake()
        {
            timer = new ScannerTimer(scanSeconds, cooldownSeconds);
            view?.Hide();
        }

        private void OnEnable()
        {
            if (inputReader != null)
            {
                inputReader.ScanPressed += OnScanPressed;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.ScanPressed -= OnScanPressed;
            }

            view?.Hide();
        }

        private void Update()
        {
            if (timer != null && timer.Tick(Time.unscaledTime))
            {
                view?.Hide();
            }
        }

        public void SetZone(ZoneContext zone)
        {
            zoneTargets = zone != null
                ? zone.GetComponentsInChildren<Scannable2D>(true)
                : System.Array.Empty<Scannable2D>();
            view?.Hide();
        }

        private void OnScanPressed()
        {
            if (player == null || gameplayMode == null ||
                (gameplayMode.CurrentMode != GameplayMode.Explore &&
                 gameplayMode.CurrentMode != GameplayMode.Encounter))
            {
                return;
            }

            timer ??= new ScannerTimer(scanSeconds, cooldownSeconds);
            if (!timer.TryActivate(Time.unscaledTime))
            {
                return;
            }

            int revealed = 0;
            float radiusSquared = scanRadius * scanRadius;
            for (int index = 0; index < zoneTargets.Length; index++)
            {
                Scannable2D target = zoneTargets[index];
                if (target == null || !target.gameObject.activeInHierarchy ||
                    ((Vector2)target.transform.position - (Vector2)player.position).sqrMagnitude > radiusSquared)
                {
                    continue;
                }

                if (target.RevealUntil(timer.ActiveUntil))
                {
                    revealed++;
                }
            }

            view?.Show(revealed);
        }
    }
}
