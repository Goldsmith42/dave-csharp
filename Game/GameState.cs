using DaveCsharp.Common;

namespace DaveCsharp.Game
{
    struct GameState
    {
        readonly bool[] collisionPoints = new bool[9];
        readonly MonsterState[] monsters = new MonsterState[5];
        readonly DaveLevel[] levels = new DaveLevel[10];
        readonly byte[] titleLevel = new byte[DaveLevel.TITLE_TILES_SIZE];

        public GameState() { }

        public void SetDavePosition() => DaveP = new(x: (short)(Dave.X * Common.TILE_SIZE), y: (short)(Dave.Y * Common.TILE_SIZE));

        public void Init()
        {
            Quit = false;
            Score = 0;
            CurrentLevel = 0;
            Lives = 3;
            ViewX = 0;
            ScrollX = 0;
            Dave = new(x: 2, y: 8);
            SetDavePosition();
            Trophy = false;
            Gun = false;
            Jetpack = 0;
            CheckDoor = false;
            JumpTimer = 0;
            TryRight = false;
            TryLeft = false;
            TryJump = false;
            DaveRight = false;
            DaveLeft = false;
            DaveJump = false;

            ResetMonsters();
        }

        public readonly void ResetMonsters()
        {
            for (int j = 0; j < monsters.Length; j++) monsters[j] = new();
        }

        public readonly void LoadLevels()
        {
            var path = "assets/levels/";
            for (int j = 0; j < levels.Length; j++)
            {
                levels[j] = new();
                using BinaryFileReader reader = new(Path.Combine(path, $"level{j}.dat"));
                for (int i = 0; i < levels[j].Path.Length; i++) levels[j].Path[i] = reader.ReadByte();
                for (int i = 0; i < levels[j].Tiles.Length; i++) levels[j].Tiles[i] = reader.ReadByte();
                for (int i = 0; i < levels[j].Padding.Length; i++) levels[j].Padding[i] = reader.ReadByte();
            }

            using BinaryFileReader titleReader = new(Path.Combine(path, "leveltitle.dat"));
            for (var i = 0; i < titleLevel.Length; i++) titleLevel[i] = titleReader.ReadByte();
        }

        public readonly void SetCollisionPoints(params bool[] values)
        {
            var n = Math.Min(values.Length, collisionPoints.Length);
            for (var i = 0; i < n; i++) collisionPoints[i] = values[i];
        }

        public readonly void SetMonsters(TileType type, params Point<ushort>[] monsterP)
        {
            var n = Math.Min(monsterP.Length, monsters.Length);
            for (var i = 0; i < n; i++)
            {
                monsters[i].Type = type;
                monsters[i].MonsterP = monsterP[i].GetMultiple(Common.TILE_SIZE);
            }
        }

        public readonly IEnumerable<MonsterState> ActiveMonsters => monsters.Where(m => m.IsActive);

        public readonly bool GetCollisionPoint(int index) => collisionPoints[index];
        public readonly bool CollisionUp => GetCollisionPoint(0) && GetCollisionPoint(1);

        public bool Quit { get; set; }
        public byte Tick { get; set; }
        public byte DaveTick { get; set; }
        public byte CurrentLevel { get; set; }
        public uint Score { get; private set; }
        public byte Lives { get; set; }
        public byte ViewX { get; set; }
        public sbyte ScrollX { get; set; }
        public Point<sbyte> Dave { get; set; } = Point<sbyte>.Default;
        public Point<short> DaveP { get; set; } = Point<short>.Default;
        public readonly bool OnGround => (!GetCollisionPoint(4) && !GetCollisionPoint(5)) || CanClimb;
        public Direction LastDir { get; set; }

        public bool TryRight { get; set; }
        public bool TryLeft { get; set; }
        public bool TryJump { get; set; }
        public bool TryFire { get; set; }
        public bool TryJetpack { get; set; }
        public bool TryDown { get; set; }
        public bool TryUp { get; set; }
        public bool DaveRight { get; set; }
        public bool DaveLeft { get; set; }
        public bool DaveJump { get; set; }
        public bool DaveFire { get; set; }
        public bool DaveJetpack { get; set; }
        public bool DaveClimb { get; set; }
        public bool DaveDown { get; set; }
        public bool DaveUp { get; set; }
        public byte JumpTimer { get; set; }
        public DeadTimer DaveDeadTimer { get; set; } = new();
        public byte JetpackDelay { get; set; }
        public Point<byte> CheckPickup { get; set; } = Point<byte>.Default;
        public bool CheckDoor { get; set; }
        public bool CanClimb { get; set; }
        public bool Trophy { get; set; }
        public bool Gun { get; set; }
        public byte Jetpack { get; set; }

        public Point<ushort> DBulletP { get; set; } = Point<ushort>.Default;
        public Direction DBulletDir { get; set; }
        public Point<ushort> EBulletP { get; set; } = Point<ushort>.Default;
        public Direction EBulletDir { get; set; }

        public GameMode Mode { get; set; } = GameMode.Title;

        public readonly DaveLevel SelectedLevel => levels[CurrentLevel];

        public uint AddScore(ushort newScore)
        {
            if (Score / 20000 != ((Score + newScore) / 20000)) Lives++;
            Score += newScore;
            return Score;
        }

        public readonly byte GetTitleLevelTile(byte x, byte y) => titleLevel[y * 10 + x];
    }
}