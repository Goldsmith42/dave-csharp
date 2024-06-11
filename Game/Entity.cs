namespace DaveCsharp.Game
{
    public class Entity
    {
        private enum AnimationSpeed
        {
            Fast = 5,
            Slow = 3
        }

        private record PartialProperties(
            bool? Animated = null,
            AnimationSpeed AnimationSpeed = AnimationSpeed.Fast,
            bool? Climbable = null,
            bool? HasCollision = null,
            bool? IsPickup = null,
            bool? IsHazard = null,
            ushort? ScoreValue = null
        );

        private readonly struct Properties
        {
            public bool Animated { get; }
            public AnimationSpeed AnimationSpeed { get; } = AnimationSpeed.Fast;
            public bool Climbable { get; }
            public bool HasCollision { get; }
            public bool IsPickup { get; }
            public bool IsHazard { get; }
            public ushort? ScoreValue { get; }

            public Properties(int numberOfFrames, PartialProperties? properties)
            {
                Animated = numberOfFrames > 1;
                if (properties is not null)
                {
                    if (properties.Animated.HasValue) Animated = properties.Animated.Value;
                    AnimationSpeed = properties.AnimationSpeed;
                    Climbable = properties.Climbable.HasValue && properties.Climbable.Value;
                    HasCollision = properties.HasCollision.HasValue && properties.HasCollision.Value;
                    IsHazard = properties.IsHazard.HasValue && properties.IsHazard.Value;
                    ScoreValue = properties.ScoreValue;
                    IsPickup = properties.IsPickup ?? ScoreValue.HasValue;
                }
            }
        }

        public record DaveProperties(
            Direction Direction,
            bool Jetpack,
            bool Jump,
            bool OnGround,
            bool Climbing
        );

        private static List<TileType> CreateRange(TileType start, TileType end)
        {
            List<TileType> range = [start];
            for (var i = start + 1; i <= end; i++) range.Add(i);
            return range;
        }
        private static List<TileType> CreateAnimationRange(TileType type, int numberOfFrames) => CreateRange(type, type + numberOfFrames - 1);


        private readonly IEnumerable<TileType> indexRange;
        private readonly Properties properties;


        private Entity(
            TileType type,
            PartialProperties? properties = null,
            int numberOfFrames = 1
        )
        {
            indexRange = CreateAnimationRange(type, numberOfFrames);
            this.properties = new Properties(numberOfFrames, properties);
        }

        private static Entity CreateMonster(TileType type) => new(type, new(
            AnimationSpeed: AnimationSpeed.Slow,
            IsPickup: false,
            ScoreValue: 300
        ));

        private static Entity CreateWall(TileType type) => new(type, new(HasCollision: true));

        public static Entity GetByType(TileType type)
        {
            foreach (var pi in typeof(Entity).GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public))
                if (pi.PropertyType == typeof(Entity))
                {
                    var value = pi.GetValue(null) as Entity;
                    if (value is not null && value.indexRange.Contains(type)) return value;
                }
            return new Entity(type);
        }

        private TileType Type => indexRange.First();

        public bool IsClimbable => properties.Climbable;
        public bool HasScoreValue => properties.ScoreValue.HasValue;
        public ushort ScoreValue => properties.ScoreValue ?? 0;
        public bool HasCollision => properties.HasCollision;
        public bool IsPickup => properties.IsPickup;
        public bool IsHazard => properties.IsHazard;

        public bool Is(Entity entity) => Type == entity.Type;

        public TileType GetFrame(uint tick, uint salt = 0) => properties.Animated ?
            (TileType)((byte)Type + (salt + tick / (uint)properties.AnimationSpeed) % indexRange.Count()) :
            Type;

        public static TileType GetDaveFrame(uint tick, DaveProperties daveProperties)
        {
            if (daveProperties.Jetpack)
                return daveProperties.Direction >= Direction.Neutral ?
                    DaveJetpackRight.GetFrame(tick) :
                    DaveJetpackLeft.GetFrame(tick);
            if (daveProperties.Climbing)
                return DaveClimb.GetFrame(tick);
            if (daveProperties.Jump || !daveProperties.OnGround)
                return daveProperties.Direction >= Direction.Neutral ?
                    TileType.DaveJumpRight :
                    TileType.DaveJumpLeft;
            return daveProperties.Direction switch
            {
                Direction.Left => DaveLeft.GetFrame(tick),
                Direction.Right => DaveRight.GetFrame(tick),
                _ => TileType.DaveDefault,
            };
        }

        public static Entity Empty { get; } = new(TileType.Empty);
        public static Entity Rock { get; } = CreateWall(TileType.Rock);
        public static Entity Door { get; } = new(TileType.Door);
        public static Entity SilverBar { get; } = CreateWall(TileType.SilverBar);
        public static Entity Jetpack { get; } = new(TileType.Jetpack, new(IsPickup: true));
        public static Entity BlueBrick { get; } = CreateWall(TileType.BlueBrick);
        public static Entity Fire { get; } = new(TileType.FireStart, new(IsHazard: true), numberOfFrames: 4);
        public static Entity Trophy { get; } = new(
            TileType.TrophyStart,
            new(ScoreValue: 1000),
            numberOfFrames: 5
        );
        public static Entity PipeHorizontal { get; } = CreateWall(TileType.PipeHorizontal);
        public static Entity PipeVertical { get; } = CreateWall(TileType.PipeVertical);
        public static Entity RedBrick { get; } = CreateWall(TileType.RedBrick);
        public static Entity NormalRock { get; } = CreateWall(TileType.NormalRock);
        public static Entity BlueWall { get; } = CreateWall(TileType.BlueWall);
        public static Entity Gun { get; } = new(TileType.Gun, new(IsPickup: true));
        public static Entity RockSlope1 { get; } = CreateWall(TileType.RockSlope1);
        public static Entity RockSlope2 { get; } = CreateWall(TileType.RockSlope2);
        public static Entity RockSlope3 { get; } = CreateWall(TileType.RockSlope3);
        public static Entity RockSlope4 { get; } = CreateWall(TileType.RockSlope4);
        public static Entity Weed { get; } = new(TileType.WeedStart, new(IsHazard: true), numberOfFrames: 4);
        public static Entity PurpleBarVertical { get; } = CreateWall(TileType.PurpleBarVertical);
        public static Entity PurpleBarHorizontal { get; } = CreateWall(TileType.PurpleBarHorizontal);
        public static Entity Tree { get; } = new(
            TileType.TreeStart,
            new(Climbable: true, Animated: false),
            numberOfFrames: 3
    );
        public static Entity Water { get; } = new(TileType.WaterStart, new(IsHazard: true), numberOfFrames: 5);
        public static Entity Stars { get; } = new(TileType.Stars, new(Climbable: true));
        public static Entity BlueDiamond { get; } = new(TileType.BlueDiamond, new(ScoreValue: 100));
        public static Entity PurpleBall { get; } = new(TileType.PurpleBall, new(ScoreValue: 50));
        public static Entity RedDiamond { get; } = new(TileType.RedDiamond, new(ScoreValue: 150));
        public static Entity Crown { get; } = new(TileType.Crown, new(ScoreValue: 300));
        public static Entity Ring { get; } = new(TileType.Ring, new(ScoreValue: 200));
        public static Entity Wand { get; } = new(TileType.Wand, new(ScoreValue: 500));
        public static Entity DaveRight { get; } = new(TileType.DaveRightStart, numberOfFrames: 3);
        public static Entity DaveLeft { get; } = new(TileType.DaveLeftStart, numberOfFrames: 3);
        public static Entity DaveClimb { get; } = new(TileType.DaveClimbStart, numberOfFrames: 3);
        public static Entity DaveJetpackRight { get; } = new(TileType.JetpackRightStart, numberOfFrames: 3);
        public static Entity DaveJetpackLeft { get; } = new(TileType.JetpackLeftStart, numberOfFrames: 3);
        public static Entity MonsterSpider { get; } = CreateMonster(TileType.MonsterSpiderStart);
        public static Entity MonsterPurpleThing { get; } = CreateMonster(TileType.MonsterPurpleThingStart);
        public static Entity MonsterRedSun { get; } = CreateMonster(TileType.MonsterRedSunStart);
        public static Entity MonsterGreenBar { get; } = CreateMonster(TileType.MonsterGreenBarStart);
        public static Entity MonsterGreySaucer { get; } = CreateMonster(TileType.MonsterGreySaucerStart);
        public static Entity MonsterDoubleMushroom { get; } = CreateMonster(TileType.MonsterDoubleMushroomStart);
        public static Entity MonsterGreenCircle { get; } = CreateMonster(TileType.MonsterGreenCircleStart);
        public static Entity MonsterSilverSpinner { get; } = CreateMonster(TileType.MonsterSilverSpinnerStart);
        public static Entity Explosion { get; } = new(TileType.ExplosionStart, new(AnimationSpeed: AnimationSpeed.Slow), numberOfFrames: 4);
        public static Entity Title { get; } = new(TileType.TitleStart, numberOfFrames: 4);
    }
}