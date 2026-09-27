using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Player;
using UnityEngine;

namespace EscapeUNIFRANZ.Hazards
{
    /// <summary>
    /// Owns danger contact only. Detection sensors never use this component.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class HazardContact2D : MonoBehaviour, IEncounterResettable
    {
        [SerializeField] private EncounterContext encounter;
        [SerializeField] private TimedHazardArea2D activationSource;
        [SerializeField] private string defeatReason = "Contacto con una amenaza activa.";

        private readonly Collider2D[] overlapResults = new Collider2D[8];
        private Collider2D hitbox;
        private bool armed = true;

        public bool IsDangerous => activationSource == null || activationSource.IsActive;

        private void Awake()
        {
            hitbox = GetComponent<Collider2D>();
            hitbox.isTrigger = true;
        }

        private void FixedUpdate()
        {
            if (!armed || !IsDangerous || hitbox == null)
            {
                return;
            }

            ContactFilter2D filter = ContactFilter2D.noFilter;
            filter.useTriggers = false;
            int count = hitbox.Overlap(filter, overlapResults);
            for (int index = 0; index < count; index++)
            {
                TryHandleContact(overlapResults[index]);
                if (!armed)
                {
                    return;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryHandleContact(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryHandleContact(other);
        }

        private void TryHandleContact(Collider2D other)
        {
            if (!armed || !ShouldRequestRespawn(
                IsDangerous,
                PlayerPhysicsContact2D.IsPlayerBody(other)))
            {
                return;
            }

            if (encounter != null && encounter.RequestRespawn(defeatReason))
            {
                armed = false;
            }
        }

        public static bool ShouldRequestRespawn(bool hazardActive, bool physicalPlayerContact)
        {
            return hazardActive && physicalPlayerContact;
        }

        public void ResetEncounter()
        {
            armed = true;
        }

        private void OnDrawGizmosSelected()
        {
            Collider2D hitbox = GetComponent<Collider2D>();
            Gizmos.color = new Color(1f, 0.05f, 0.05f, 0.9f);
            Gizmos.DrawWireCube(hitbox.bounds.center, hitbox.bounds.size);
        }
    }
}
