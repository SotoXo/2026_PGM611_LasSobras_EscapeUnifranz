using UnityEngine;

namespace EscapeUNIFRANZ.Player
{
    /// <summary>
    /// Identifies only the Player's solid body collider. Child interaction sensors do not count.
    /// </summary>
    public static class PlayerPhysicsContact2D
    {
        public static bool IsPlayerBody(Collider2D other)
        {
            return other != null &&
                !other.isTrigger &&
                other.GetComponent<PlayerMovement2D>() != null;
        }
    }
}
