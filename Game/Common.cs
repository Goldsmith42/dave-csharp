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

    enum GameMode { Title, Gameplay, LevelTransition }

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

    /// <summary>
    /// A counter that can be manually ticked down and is considered to be active if the timer value is higher than zero.
    /// The timer is considered inactive until the <code>Start()</code> method is called.
    /// </summary>
    /// <param name="maxValue">The starting value of the </param>
    class TickTimer(byte maxValue)
    {
        private readonly byte maxValue = maxValue;

        protected byte Value { get; private set;}

        /// <summary>
        /// Resets the timer's value to the instance's maximum.
        /// </summary>
        public void Start() => Value = maxValue;
        /// <summary>
        /// Decrements the timer's value.
        /// </summary>
        /// <returns>true if the value has reached zero, otherwise false.</returns>
        public bool Tick()
        {
            if (Value > 0)
            {
                Value--;
                if (Value == 0) return true;
            }
            return false;
        }
        /// <summary>
        /// A value indicating whether the internal timer has reached zero.
        /// </summary>
        public bool IsActive => Value > 0;
    }

    /// <summary>
    /// A TickTimer used for the start of the level when Dave is blinking.
    /// </summary>
    class DaveStartTimer : TickTimer
    {
        public DaveStartTimer() : base(80) {}

        /// <summary>
        /// Indicates whether Dave should be invisible for the current tick.
        /// </summary>
        public bool InvisibleTick => IsActive && (Value / 10 % 2 == 0);
    }

    /// <summary>
    /// A TickTimer used for Dave or monsters dying.
    /// </summary>
    class DeadTimer : TickTimer
    {
        public DeadTimer() : base(30) {}
    }
}