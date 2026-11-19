using UnityEngine;

namespace EscapeUNIFRANZ.Hazards
{
    internal enum MiniAutoWear { Healthy, Light, Heavy }

    internal sealed class MiniAutoSpriteSet
    {
        private const int CellSize = 64;
        private const float FrameSeconds = 0.14f;
        public const float CycleSeconds = 3 * FrameSeconds;

        private enum Direction { Right, Left, Up, Down }

        private readonly Sprite[,] movement;
        private readonly Sprite[,] vertical;
        private readonly Sprite[,] impact;

        private MiniAutoSpriteSet(Texture2D movement, Texture2D vertical, Texture2D impact)
        {
            this.movement = Slice(movement, 10, 3);
            this.vertical = Slice(vertical, 1, 2);
            this.impact = Slice(impact, 5, 3);
        }

        public static MiniAutoSpriteSet Create(Texture2D movement, Texture2D vertical, Texture2D impact)
        {
            if (!HasGrid(movement, 10, 3) || !HasGrid(vertical, 1, 2) || !HasGrid(impact, 5, 3))
                return null;

            return new MiniAutoSpriteSet(movement, vertical, impact);
        }

        public Sprite Frame(Vector2 facing, MiniAutoWear wear, bool moving, float movementTime, float impactTime, out bool flipX)
        {
            Direction direction;
            if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y))
                direction = facing.x > 0f ? Direction.Right : Direction.Left;
            else
                direction = facing.y > 0f ? Direction.Up : Direction.Down;

            // Impact and stationary wear share columns: right, up, down. Left mirrors right.
            flipX = false;
            if (impactTime >= 0f || (!moving && wear != MiniAutoWear.Healthy))
            {
                flipX = direction == Direction.Left;
                int column = direction == Direction.Up ? 1 : direction == Direction.Down ? 2 : 0;
                int row = impactTime >= 0f ? FrameIndex(impactTime) : wear == MiniAutoWear.Light ? 3 : 4;
                return impact[row, column];
            }

            // Movement rows are right, left, up and down for light and heavy wear,
            // followed by the two healthy lateral cycles. Healthy vertical art is static.
            int frame = FrameIndex(movementTime);
            switch (wear)
            {
                case MiniAutoWear.Light:
                    return movement[(int)direction, frame];
                case MiniAutoWear.Heavy:
                    return movement[4 + (int)direction, frame];
                default:
                    if (direction == Direction.Up || direction == Direction.Down)
                        return vertical[0, direction == Direction.Up ? 1 : 0];
                    return movement[8 + (int)direction, frame];
            }
        }

        public void Dispose()
        {
            foreach (Sprite sprite in movement) Object.Destroy(sprite);
            foreach (Sprite sprite in vertical) Object.Destroy(sprite);
            foreach (Sprite sprite in impact) Object.Destroy(sprite);
        }

        private static int FrameIndex(float time) => Mathf.Min(2, (int)(time / FrameSeconds));

        private static bool HasGrid(Texture2D atlas, int rows, int columns) =>
            atlas != null && atlas.width == columns * CellSize && atlas.height == rows * CellSize;

        private static Sprite[,] Slice(Texture2D atlas, int rows, int columns)
        {
            var sprites = new Sprite[rows, columns];
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    var rect = new Rect(column * CellSize, (rows - row - 1) * CellSize, CellSize, CellSize);
                    sprites[row, column] = Sprite.Create(atlas, rect, new Vector2(0.5f, 0f),
                        CellSize, 0, SpriteMeshType.FullRect);
                }
            }
            return sprites;
        }
    }
}
