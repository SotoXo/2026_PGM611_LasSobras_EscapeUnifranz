using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EscapeUNIFRANZ.World
{
    [DisallowMultipleComponent]
    public sealed class DoorPanel2D : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D blocker;
        [SerializeField] private SpriteRenderer leaf;

        private readonly List<Collider2D> occupants = new();
        private Vector3 closedPosition;
        private Vector3 closedScale;
        private float openness;
        private ShadowCaster2D shadow;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            closedPosition = leaf.transform.localPosition;
            closedScale = leaf.transform.localScale;
            shadow = blocker.GetComponent<ShadowCaster2D>();
        }

        public void Open()
        {
            IsOpen = true;
            blocker.enabled = false;
            if (shadow != null) shadow.enabled = false;
        }

        public bool TryClose()
        {
            var filter = ContactFilter2D.noFilter;
            filter.useTriggers = false;
            Vector2 size = Vector2.Scale(blocker.size, blocker.transform.lossyScale);
            Physics2D.OverlapBox(blocker.transform.TransformPoint(blocker.offset), size,
                blocker.transform.eulerAngles.z, filter, occupants);
            foreach (Collider2D occupant in occupants)
            {
                if (occupant.attachedRigidbody != null &&
                    occupant.attachedRigidbody.bodyType != RigidbodyType2D.Static)
                    return false;
            }

            IsOpen = false;
            blocker.enabled = true;
            if (shadow != null) shadow.enabled = true;
            return true;
        }

        private void Update()
        {
            float next = Mathf.MoveTowards(openness, IsOpen ? 1f : 0f, Time.deltaTime * 4f);
            if (next == openness)
                return;

            openness = next;
            float slide = Mathf.SmoothStep(0f, 1f, openness);
            float width = leaf.sprite.bounds.size.x * closedScale.x;
            leaf.transform.localPosition = closedPosition + Vector3.right * (width * 0.43f * slide);
            leaf.transform.localScale = new Vector3(closedScale.x * (1f - 0.86f * slide), closedScale.y, closedScale.z);
        }
    }
}
