using UnityEngine;

namespace EscapeUNIFRANZ.Hazards
{
    public sealed class PatrolPath2D : MonoBehaviour
    {
        [SerializeField] private Transform[] points;
        [SerializeField] private bool pingPong = true;

        public int Count => points?.Length ?? 0;
        public bool PingPong => pingPong;

        public Vector2 GetPoint(int index)
        {
            return points != null && index >= 0 && index < points.Length && points[index] != null
                ? points[index].position
                : transform.position;
        }

        private void OnDrawGizmosSelected()
        {
            if (points == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            for (int index = 0; index < points.Length; index++)
            {
                if (points[index] == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(points[index].position, 0.15f);
                if (index + 1 < points.Length && points[index + 1] != null)
                {
                    Gizmos.DrawLine(points[index].position, points[index + 1].position);
                }
            }
        }
    }
}
