using UnityEngine;

namespace EscapeUNIFRANZ.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerCameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0f)] private float smoothTime = 0.06f;
        [SerializeField, Min(0.1f)] private float teleportDistance = 3f;
        private Vector3 velocity;
        private Vector3 previousTarget;
        private bool initialized;

        private void OnEnable() { initialized = false; }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
            if (!initialized || (target.position - previousTarget).sqrMagnitude > teleportDistance * teleportDistance)
            {
                transform.position = desired;
                velocity = Vector3.zero;
                initialized = true;
            }
            else if (Time.deltaTime > 0f)
            {
                transform.position = Vector3.SmoothDamp(transform.position, desired,
                    ref velocity, smoothTime, Mathf.Infinity, Time.deltaTime);
            }
            previousTarget = target.position;
        }
    }
}
