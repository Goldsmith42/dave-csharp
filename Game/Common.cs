using System.Numerics;

namespace DaveCsharp.Game
{
    public static class Common
    {
        public const byte TILE_SIZE = 16;
    }

    public enum Direction
    {
        Left = -1,
        Neutral,
        Right
    }

    class Point<T>(T x, T y) where T : INumber<T>
    {
        public T X { get; set; } = x;
        public T Y { get; set; } = y;

        public bool IsEmpty => X == default && Y == default;
        public bool IsSet => X != default && Y != default;

        public Point(Point<T> point) : this(point.X, point.Y) { }

        public void Reset()
        {
            X = default!;
            Y = default!;
        }

        public Point<T> GetMultiple(T multiplier) => new(X * multiplier, Y * multiplier);

        public override int GetHashCode() => (X, Y).GetHashCode();

        public static Point<T> Default => new(default!, default!);
    }

    class DeadTimer
    {
        private byte value;
        public void Start() => value = 30;
        public bool Tick()
        {
            if (value > 0)
            {
                value--;
                if (value == 0) return true;
            }
            return false;
        }
        public bool IsActive => value > 0;
    }
}