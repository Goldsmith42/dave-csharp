namespace DaveCsharp.Game
{
    class MonsterState
    {
        private const sbyte PATH_END = unchecked((sbyte)0xea);

        private byte pathIndex;
        private Point<sbyte> nextP = Point<sbyte>.Default;

        public Entity? Type { get; set; } = null;
        public DeadTimer DeadTimer { get; set; } = new();
        public Point<byte> Monster { get; set; } = Point<byte>.Default;
        public Point<ushort> MonsterP { get; set; } = Point<ushort>.Default;

        public bool IsActive => Type is not null;

        public void Deactivate() => Type = null;

        private void ResetPath(byte[] path) => SetPath(path, 0);
        private void SetPath(byte[] path) => SetPath(path, pathIndex);
        private void SetPath(byte[] path, byte index)
        {
            nextP = new(
                x: (sbyte)path[index],
                y: (sbyte)path[index + 1]
            );
            pathIndex = (byte)(index + 2);
        }

        public void Move(byte[] path)
        {
            if (IsActive && !DeadTimer.IsActive)
                for (byte j = 0; j < 2; j++)
                {
                    if (nextP.IsEmpty) SetPath(path);
                    if (nextP.X == PATH_END && nextP.Y == PATH_END) ResetPath(path);

                    if (nextP.X < 0)
                    {
                        MonsterP.X -= 1;
                        nextP.X++;
                    }

                    if (nextP.X > 0)
                    {
                        MonsterP.X += 1;
                        nextP.X--;
                    }

                    if (nextP.Y < 0)
                    {
                        MonsterP.Y -= 1;
                        nextP.Y++;
                    }

                    if (nextP.Y > 0)
                    {
                        MonsterP.Y += 1;
                        nextP.Y--;
                    }
                }

                Monster.X = (byte)(MonsterP.X / Common.TILE_SIZE);
                Monster.Y = (byte)(MonsterP.Y / Common.TILE_SIZE);
        }
    }
}