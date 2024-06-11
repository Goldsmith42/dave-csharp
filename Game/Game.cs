using System.Runtime.InteropServices;
using DaveCsharp.Game.Drawing;
using SDL2;

namespace DaveCsharp.Game
{
    class Game
    {
        private GameState game = new();

        public static Point<byte> OnGrid(int x, int y) => new(
            x: (byte)(x / Common.TILE_SIZE),
            y: (byte)(y / Common.TILE_SIZE)
        );
        public static Point<byte> OnGrid(Point<ushort> p) => new(
            x: (byte)(p.X / Common.TILE_SIZE),
            y: (byte)(p.Y / Common.TILE_SIZE)
        );

        public void Init()
        {
            game.Init();
            game.LoadLevels();
        }

        private static bool GetKey(nint keyState, int arraySize, SDL.SDL_Scancode scancode)
        {
            byte[] keys = new byte[arraySize];
            Marshal.Copy(keyState, keys, 0, arraySize);
            return keys[(int)scancode] == 1;
        }

        internal void CheckInput()
        {
            _ = SDL.SDL_PollEvent(out SDL.SDL_Event e);

            var keyState = SDL.SDL_GetKeyboardState(out int arraySize);
            if (game.Mode == GameMode.Gameplay)
            {
                if (GetKey(keyState, arraySize, SDL.SDL_Scancode.SDL_SCANCODE_RIGHT)) game.TryRight = true;
                if (GetKey(keyState, arraySize, SDL.SDL_Scancode.SDL_SCANCODE_LEFT)) game.TryLeft = true;
                if (GetKey(keyState, arraySize, SDL.SDL_Scancode.SDL_SCANCODE_UP)) game.TryJump = true;
                if (GetKey(keyState, arraySize, SDL.SDL_Scancode.SDL_SCANCODE_DOWN)) game.TryDown = true;
                if (GetKey(keyState, arraySize, SDL.SDL_Scancode.SDL_SCANCODE_LCTRL)) game.TryFire = true;
                if (GetKey(keyState, arraySize, SDL.SDL_Scancode.SDL_SCANCODE_LALT)) game.TryJetpack = true;
            }
            else if (game.Mode == GameMode.Title)
            {
                SDL.SDL_Scancode[] keys = [
                    SDL.SDL_Scancode.SDL_SCANCODE_RETURN,
                    SDL.SDL_Scancode.SDL_SCANCODE_SPACE,
                    SDL.SDL_Scancode.SDL_SCANCODE_LCTRL,
                    SDL.SDL_Scancode.SDL_SCANCODE_LALT
                ];
                if (keys.Any(k => GetKey(keyState, arraySize, k)))
                {
                    game.Mode = GameMode.Gameplay;
                    StartLevel();
                }
            }
            if (e.type == SDL.SDL_EventType.SDL_QUIT) game.Quit = true;
        }

        internal void Render(Renderer renderer, GameAssets assets)
        {
            renderer.Clear();

            DrawWorld(renderer, assets);
            if (game.Mode is GameMode.Gameplay or GameMode.LevelTransition) 
            {
                DrawDave(renderer, assets);
                if (game.Mode is GameMode.Gameplay)
                {
                    DrawMonsters(renderer, assets);
                    DrawDaveBullet(renderer, assets);
                    DrawMonsterBullet(renderer, assets);
                }
            }
            DrawUI(renderer, assets);

            renderer.RenderScreen();
        }

        private byte UpdateFrame(Entity tile, byte salt) => (byte)tile.GetFrame(game.Tick, salt);

        private static int GetTileIndex(Point<byte> grid) => GetTileIndex(grid.X, grid.Y);
        private static int GetTileIndex(byte x, byte y) => y * 100 + x;
        private byte GetGridTile(Point<byte> grid) => GetGridTile(grid.X, grid.Y);
        private byte GetGridTile(byte x, byte y) => game.SelectedLevel.Tiles[GetTileIndex(x, y)];
        private Entity GetGridObject(Point<byte> grid) => Entity.GetByType((TileType)GetGridTile(grid));
        private Entity GetGridObject(byte x, byte y) => Entity.GetByType((TileType)GetGridTile(x, y));
        private static Entity GetTransitionGridObject(byte x, byte y)
        {
            // The layout of the transition level is always the same and simple, so we can get the tiles on the fly.
            TileType tileType;
            if (y is 3 or 5) tileType = TileType.BlueBrick;
            else if (y == 4 && x == 0) tileType = TileType.Door;
            else tileType = TileType.Empty;
            return Entity.GetByType(tileType);
        }

        private void DrawMap(
            Renderer renderer,
            GameAssets assets,
            Func<byte, byte, Entity> getTile,
            Point<byte> numberOfTiles,
            Point<byte>? tileOffset = null
        )
        {
            tileOffset ??= new(x: 0, y: 0);
            SDL.SDL_Rect dest = new() { w = Common.TILE_SIZE };
            for (byte j = 0; j < numberOfTiles.Y; j++)
            {
                dest.y = Common.TILE_SIZE + (j + tileOffset.Y) * Common.TILE_SIZE;
                for (byte i = 0; i < numberOfTiles.X; i++)
                {
                    dest.x = (i + tileOffset.X) * Common.TILE_SIZE;
                    var tileIndex = UpdateFrame(getTile(i, j), i);
                    dest.h = Common.TILE_SIZE;
                    if (j < numberOfTiles.Y - 1)
                        renderer.RenderTexture(assets.GetTile(tileIndex), dest);
                    else
                    {
                        dest.h = (int)(dest.h / 2.5);
                        // For the last y, only draw the upper third
                        renderer.RenderTexture(assets.GetTile(tileIndex), dest, new()
                        {
                            x = 0,
                            y = 0,
                            h = dest.h,
                            w = dest.w
                        });
                    }
                }
            }
        }

        private void DrawWorld(Renderer renderer, GameAssets assets)
        {
            if (game.Mode == GameMode.Title)
                DrawMap(
                    renderer,
                    assets,
                    (x, y) => Entity.GetByType(game.GetTitleLevelTile(x, y)),
                    new Point<byte>(x: 10, y: 7),
                    new Point<byte>(x: 5, y: 3)
                );
            else
                DrawMap(
                    renderer,
                    assets,
                    game.Mode == GameMode.LevelTransition ?
                        (x, y) => GetTransitionGridObject((byte)(game.ViewX + x), y) :
                        (x, y) => GetGridObject((byte)(game.ViewX + x), y),
                    new Point<byte>(x: 20, y: 10)
                );
        }

        private void DrawDave(Renderer renderer, GameAssets assets)
        {
            var tileIndex = game.DaveDeadTimer.IsActive ?
                Entity.Explosion.GetFrame(game.Tick) :
                Entity.GetDaveFrame(game.DaveTick, new(
                    Direction: game.LastDir,
                    Jetpack: game.DaveJetpack,
                    Jump: game.DaveJump,
                    OnGround: game.OnGround,
                    Climbing: game.DaveClimb
                ));
            renderer.RenderTexture(assets.GetTile(tileIndex), new()
            {
                x = game.DaveP.X - game.ViewX * Common.TILE_SIZE,
                y = Common.TILE_SIZE + game.DaveP.Y,
                w = 20,
                h = 16
            });
        }

        private void DrawMonsters(Renderer renderer, GameAssets assets)
        {
            foreach (var m in game.ActiveMonsters)
            {
                var tileIndex = (m.DeadTimer.IsActive ? Entity.Explosion : m.Type ?? Entity.Empty).GetFrame(game.Tick);
                renderer.RenderTexture(assets.GetTile(tileIndex), new()
                {
                    x = m.MonsterP.X - game.ViewX * Common.TILE_SIZE,
                    y = Common.TILE_SIZE + m.MonsterP.Y,
                    w = 20,
                    h = 16
                });
            }
        }

        private void DrawDaveBullet(Renderer renderer, GameAssets assets)
        {
            if (game.DBulletP.IsSet)
                renderer.RenderTexture(
                    assets.GetTile(game.DBulletDir > Direction.Neutral ? TileType.DaveBulletLeft : TileType.DaveBulletLeft),
                    new()
                    {
                        x = game.DBulletP.X - game.ViewX * Common.TILE_SIZE,
                        y = Common.TILE_SIZE + game.DBulletP.Y,
                        w = 12,
                        h = 3
                    }
                );
        }

        private void DrawMonsterBullet(Renderer renderer, GameAssets assets)
        {
            if (game.EBulletP.IsSet)
                renderer.RenderTexture(
                    assets.GetTile(game.EBulletDir > Direction.Neutral ? TileType.MonsterBulletRight : TileType.MonsterBulletLeft),
                    new()
                    {
                        x = game.EBulletP.X - game.ViewX * Common.TILE_SIZE,
                        y = Common.TILE_SIZE + game.EBulletP.Y,
                        w = 12,
                        h = 3
                    }
                );
        }

        private void DrawUI(Renderer renderer, GameAssets assets)
        {
            if (game.Mode == GameMode.Gameplay)
            {
                SDL.SDL_Rect dest = new() { x = 0, y = 16, w = 960, h = 1 };
                Renderer.Color white = new(0xee, 0xee, 0xee, 0xff);
                renderer.RenderColor(white, dest);
                dest.y = 166;
                renderer.RenderColor(white, dest);

                dest.x = 1;
                dest.y = 2;
                dest.w = 62;
                dest.h = 11;
                renderer.RenderTexture(assets.GetTile(TileType.UIScoreBanner), dest);

                dest.x = 120;
                renderer.RenderTexture(assets.GetTile(TileType.UILevelBanner), dest);

                dest.x = 200;
                renderer.RenderTexture(assets.GetTile(TileType.UILivesBanner), dest);

                dest.x = 64;
                dest.w = 8;
                dest.h = 11;
                renderer.RenderTexture(assets.GetTile((byte)((byte)TileType.UI0 + game.Score / 10000 % 10)), dest);

                dest.x += 8;
                renderer.RenderTexture(assets.GetTile((byte)((byte)TileType.UI0 + game.Score / 1000 % 10)), dest);

                dest.x += 8;
                renderer.RenderTexture(assets.GetTile((byte)((byte)TileType.UI0 + game.Score / 100 % 10)), dest);

                dest.x += 8;
                renderer.RenderTexture(assets.GetTile((byte)((byte)TileType.UI0 + game.Score / 10 % 10)), dest);

                dest.x += 8;
                renderer.RenderTexture(assets.GetTile(TileType.UI0), dest);

                dest.x = 170;
                renderer.RenderTexture(assets.GetTile(TileType.UI0 + (game.CurrentLevel + 1) / 10), dest);

                dest.x += 8;
                renderer.RenderTexture(assets.GetTile(TileType.UI0 + (game.CurrentLevel + 1) % 10), dest);

                for (byte i = 0; i < game.Lives; i++)
                {
                    dest.x = 255 + Common.TILE_SIZE * i;
                    dest.w = Common.TILE_SIZE;
                    dest.h = 12;
                    renderer.RenderTexture(assets.GetTile(TileType.UIIconLife), dest);
                }

                if (game.Trophy)
                {
                    dest.x = 72;
                    dest.y = 180;
                    dest.w = 176;
                    dest.h = 14;
                    renderer.RenderTexture(assets.GetTile(TileType.UIMessageTrophy), dest);
                }

                if (game.Gun)
                {
                    dest.x = 255;
                    dest.y = 180;
                    dest.w = 62;
                    dest.h = 11;
                    renderer.RenderTexture(assets.GetTile(TileType.UIIconGun), dest);
                }

                if (game.Jetpack != 0)
                {
                    dest.x = 1;
                    dest.y = 177;
                    dest.w = 62;
                    dest.h = 11;
                    renderer.RenderTexture(assets.GetTile(TileType.UIIconJetpack), dest);

                    dest.y = 190;
                    dest.h = 8;
                    renderer.RenderTexture(assets.GetTile(TileType.UIBarJetpack), dest);

                    dest.x = 2;
                    dest.y = 192;
                    dest.w = (int)(game.Jetpack * 0.23);
                    dest.h = 4;
                    renderer.RenderColor(new Renderer.Color(0xee, 0, 0, 0xff), dest);
                }
            }
            else if (game.Mode == GameMode.Title)
            {
                SDL.SDL_Rect dest = new() { x = 104, w = 112, h = 47 };
                renderer.RenderTexture(assets.GetTile(UpdateFrame(Entity.Title, 0)), dest);

                // TODO: Draw text
            };
        }

        internal void Update()
        {
            if (game.Mode is GameMode.Gameplay or GameMode.LevelTransition)
            {
                CheckCollision();
                if (game.Mode is not GameMode.LevelTransition)
                {
                    PickupItem(game.CheckPickup);
                    UpdateDBullet();
                    UpdateEBullet();
                    VerifyInput();
                }
                MoveDave();
                if (game.Mode is GameMode.Gameplay)
                {
                    MoveMonsters();
                    FireMonsters();
                    ScrollScreen();
                    ApplyGravity();
                }
            }
            UpdateLevel();
            ClearInput();
        }

        private void StartLevelTransition()
        {
            game.AddScore(2000);
            if (game.CurrentLevel < 9)
            {
                game.Mode = GameMode.LevelTransition;
                game.Dave = new(x: 0, y: 4);
                ResetDave();
            }
            else
            {
                // TODO: Proper ending
                Console.WriteLine($"You won the game with {game.Score} points");
                game.Quit = true;
            }
        }

        private void UpdateLevel()
        {
            game.Tick++;

            if (game.Mode == GameMode.LevelTransition)
            {
                if (game.NextLevel)
                {
                    game.NextLevel = false;
                    game.CurrentLevel++;
                    StartLevel();
                }
            }
            else
            {
                if (game.JetpackDelay > 0) game.JetpackDelay--;

                if (game.DaveJetpack)
                {
                    game.Jetpack--;
                    if (game.Jetpack == 0) game.DaveJetpack = false;
                }

                if (game.CheckDoor)
                {
                    if (game.Trophy) StartLevelTransition();
                    else game.CheckDoor = false;
                }

                if (game.DaveDeadTimer.Tick())
                {
                    if (game.Lives > 0)
                    {
                        game.Lives--;
                        RestartLevel();
                    }
                    else game.Quit = true;
                }

                foreach (var monster in game.ActiveMonsters)
                    if (monster.DeadTimer.Tick()) monster.Deactivate();
                    else if (!monster.DeadTimer.IsActive && (monster.Monster.X == game.Dave.X) && (monster.Monster.Y == game.Dave.Y))
                    {
                        monster.DeadTimer.Start();
                        game.DaveDeadTimer.Start();
                    }
            }
        }

        public void RestartLevel()
        {
            switch (game.CurrentLevel)
            {
                case 0: game.Dave = new(x: 2, y: 8); break;
                case 1: game.Dave = new(x: 1, y: 8); break;
                case 2: game.Dave = new(x: 2, y: 5); break;
                case 3: game.Dave = new(x: 1, y: 5); break;
                case 4: game.Dave = new(x: 2, y: 8); break;
                case 5: game.Dave = new(x: 2, y: 8); break;
                case 6: game.Dave = new(x: 1, y: 2); break;
                case 7: game.Dave = new(x: 2, y: 8); break;
                case 8: game.Dave = new(x: 6, y: 1); break;
                case 9: game.Dave = new(x: 2, y: 8); break;
            }

            game.SetDavePosition();
        }

        private void ResetDave()
        {
            game.SetDavePosition();
            game.DaveFire = false;
            game.DaveJetpack = false;
            game.DaveDeadTimer = new();
            game.Trophy = false;
            game.Gun = false;
            game.Jetpack = 0;
            game.CheckDoor = false;
            game.ViewX = 0;
            game.JumpTimer = 0;
            game.DBulletP.Reset();
            game.EBulletP.Reset();
        }

        public void StartLevel()
        {
            game.Mode = GameMode.Gameplay;

            RestartLevel();
            game.ResetMonsters();

            switch (game.CurrentLevel)
            {
                case 2:
                    game.SetMonsters(
                        Entity.MonsterSpider,
                        new(x: 44, y: 4),
                        new(x: 59, y: 4)
                    );
                    break;
                case 3:
                    game.SetMonsters(
                        Entity.MonsterPurpleThing,
                        [new(x: 32, y: 2)]
                    );
                    break;
                case 4:
                    game.SetMonsters(
                        Entity.MonsterRedSun,
                        new(x: 15, y: 3),
                        new(x: 33, y: 3),
                        new(x: 49, y: 3)
                    );
                    break;
                case 5:
                    game.SetMonsters(
                        Entity.MonsterGreenBar,
                        new(x: 10, y: 8),
                        new(x: 28, y: 8),
                        new(x: 45, y: 2),
                        new(x: 40, y: 8)
                    );
                    break;
                case 6:
                    game.SetMonsters(
                        Entity.MonsterGreySaucer,
                        new(x: 5, y: 2),
                        new(x: 16, y: 1),
                        new(x: 46, y: 2),
                        new(x: 56, y: 3)
                    );
                    break;
                case 7:
                    game.SetMonsters(
                        Entity.MonsterDoubleMushroom,
                        new(x: 53, y: 5),
                        new(x: 72, y: 2),
                        new(x: 84, y: 1)
                    );
                    break;
                case 8:
                    game.SetMonsters(
                        Entity.MonsterGreenCircle,
                        new(x: 35, y: 8),
                        new(x: 41, y: 8),
                        new(x: 49, y: 2),
                        new(x: 65, y: 8)
                    );
                    break;
                case 9:
                    game.SetMonsters(
                        Entity.MonsterSilverSpinner,
                        new(x: 45, y: 8),
                        new(x: 51, y: 2),
                        new(x: 65, y: 3),
                        new(x: 82, y: 2)
                    );
                    break;
            }

            ResetDave();
        }

        private void MoveMonsters()
        {
            foreach (var m in game.ActiveMonsters) m.Move(game.SelectedLevel.Path);
        }

        private void FireMonsters()
        {
            if (game.EBulletP.IsEmpty)
                foreach (var m in game.ActiveMonsters)
                    if (IsVisible(m.MonsterP.X) && !m.DeadTimer.IsActive)
                    {
                        game.EBulletDir = game.DaveP.X < m.MonsterP.X ? Direction.Left : Direction.Right;
                        if (game.EBulletDir == Direction.Right)
                            game.EBulletP.X = (ushort)(m.MonsterP.X + 18);
                        if (game.EBulletDir == Direction.Left)
                            game.EBulletP.X = (ushort)(m.MonsterP.X - 8);
                        game.EBulletP.Y = (ushort)(m.MonsterP.Y + 8);
                    }
        }

        private void ApplyGravity()
        {
            if (!game.DaveJump && !game.OnGround && !game.DaveJetpack && !game.DaveClimb)
            {
                if (IsClear(game.DaveP.X + 4, game.DaveP.Y + 17))
                    game.DaveP.Y += 2;
                else
                {
                    byte notAlign = (byte)(game.DaveP.Y % Common.TILE_SIZE);
                    if (notAlign != 0)
                    {
                        game.DaveP.Y = (short)(notAlign < 8 ?
                            game.DaveP.Y - notAlign :
                            game.DaveP.Y + Common.TILE_SIZE - notAlign);
                    }
                }
            }
        }

        private void CheckCollision()
        {
            game.SetCollisionPoints(
                IsClear(game.DaveP.X + 4, game.DaveP.Y - 1),
                IsClear(game.DaveP.X + 10, game.DaveP.Y - 1),
                IsClear(game.DaveP.X + 11, game.DaveP.Y + 4),
                IsClear(game.DaveP.X + 11, game.DaveP.Y + 12),
                IsClear(game.DaveP.X + 10, game.DaveP.Y + 16),
                IsClear(game.DaveP.X + 4, game.DaveP.Y + 16),
                IsClear(game.DaveP.X + 3, game.DaveP.Y + 12),
                IsClear(game.DaveP.X + 3, game.DaveP.Y + 4)
            );

            Point<byte> grid = OnGrid(game.DaveP.X + 6, game.DaveP.Y + 8);
            var type = grid.X < 100 && grid.Y < 10 ? GetGridObject(grid) : Entity.Empty;
            if (type.IsClimbable) game.CanClimb = true;
            else
            {
                game.CanClimb = false;
                game.DaveClimb = false;
            }
        }

        private bool IsClear(Point<ushort> p, bool isDave = true) => IsClear(p.X, p.Y, isDave);
        private bool IsClear(int px, int py, bool isDave = true) => IsClear((ushort)px, (ushort)py, isDave);
        private bool IsClear(ushort px, ushort py, bool isDave = true)
        {
            Point<byte> grid = OnGrid(px, py);
            if (grid.X > 99 || grid.Y > 9) return true;

            Entity entity;
            try
            {
                entity = game.Mode == GameMode.Gameplay ? GetGridObject(grid) : GetTransitionGridObject(grid.X, grid.Y);
            }
            catch (IndexOutOfRangeException)
            {
                return true;
            }

            if (entity.HasCollision) return false;

            if (isDave)
            {
                if (entity.Is(Entity.Door)) game.CheckDoor = true;
                else if (entity.IsPickup) game.CheckPickup = new(grid);
                else if (entity.IsHazard && !game.DaveDeadTimer.IsActive)
                    game.DaveDeadTimer.Start();
                else if (game.Mode == GameMode.LevelTransition && grid.X >= 20)
                    game.NextLevel = true;
            }

            return true;
        }

        private bool IsVisible(ushort px) => ((px / Common.TILE_SIZE) - game.ViewX) is < 20 and >= 0;

        private void VerifyInput()
        {
            if (game.DaveDeadTimer.IsActive) return;
            if (game.TryRight && game.GetCollisionPoint(2) && game.GetCollisionPoint(3))
                game.DaveRight = true;
            if (game.TryLeft && game.GetCollisionPoint(6) && game.GetCollisionPoint(7))
                game.DaveLeft = true;
            if (game.TryJump)
            {
                if (game.OnGround && !game.DaveJump && !game.DaveJetpack && !game.CanClimb && game.CollisionUp)
                    game.DaveJump = true;
                else if (game.CanClimb)
                {
                    game.DaveUp = true;
                    game.DaveClimb = true;
                }
                if (game.DaveJetpack && game.CollisionUp)
                    game.DaveUp = true;
            }
            if (game.TryFire && game.Gun && game.DBulletP.IsEmpty)
                game.DaveFire = true;
            if (game.TryJetpack && game.Jetpack != 0 && game.JetpackDelay == 0)
            {
                game.DaveJetpack = !game.DaveJetpack;
                game.JetpackDelay = 10;
            }
            if (game.TryDown && (game.DaveJetpack || game.DaveClimb) && game.GetCollisionPoint(4) && game.GetCollisionPoint(6))
                game.DaveDown = true;
        }

        private void MoveDave()
        {
            game.Dave = new(
                x: (sbyte)(game.DaveP.X / Common.TILE_SIZE),
                y: (sbyte)(game.DaveP.Y / Common.TILE_SIZE)
            );

            if (game.Dave.Y > 9)
            {
                game.Dave.Y = 0;
                game.DaveP.Y = -16;
            }

            if (game.DaveRight || game.Mode == GameMode.LevelTransition)
            {
                game.DaveP.X += 2;
                game.LastDir = Direction.Right;
                game.DaveTick++;
                game.DaveRight = false;
            }

            if (game.DaveLeft)
            {
                game.DaveP.X -= 2;
                game.LastDir = Direction.Left;
                game.DaveTick++;
                game.DaveLeft = false;
            }

            if (game.DaveDown)
            {
                game.DaveP.Y += 2;
                game.DaveDown = false;
            }

            if (game.DaveUp)
            {
                game.DaveP.Y -= 2;
                game.DaveUp = false;
            }

            if (game.DaveJump)
            {
                if (game.JumpTimer == 0)
                {
                    game.JumpTimer = 30;
                    game.LastDir = Direction.Neutral;
                }
                if (game.CollisionUp)
                {
                    if (game.JumpTimer > 16) game.DaveP.Y -= 2;
                    if (game.JumpTimer is >= 12 and <= 15) game.DaveP.Y -= 1;
                }
                game.JumpTimer--;

                if (game.JumpTimer == 0) game.DaveJump = false;
            }

            if (game.DaveFire)
            {
                game.DBulletDir = game.LastDir;
                if (game.LastDir == Direction.Neutral) game.DBulletDir = Direction.Right;
                if (game.DBulletDir == Direction.Right) game.DBulletP.X = (ushort)(game.DaveP.X + 18);
                else if (game.DBulletDir == Direction.Left) game.DBulletP.X = (ushort)(game.DaveP.X - 8);
                game.DBulletP.Y = (ushort)(game.DaveP.Y + 8);
                game.DaveFire = false;
            }
        }

        private void ClearInput()
        {
            game.TryJump = false;
            game.TryLeft = false;
            game.TryRight = false;
            game.TryFire = false;
            game.TryJetpack = false;
            game.TryDown = false;
            game.TryUp = false;
        }

        private void ScrollScreen()
        {
            if (game.Dave.X - game.ViewX >= 18) game.ScrollX = 15;
            if (game.Dave.X - game.ViewX < 2) game.ScrollX = -15;

            if (game.ScrollX > 0)
            {
                if (game.ViewX == 80) game.ScrollX = 0;
                else
                {
                    game.ViewX++;
                    game.ScrollX--;
                }
            }
            if (game.ScrollX < 0)
            {
                if (game.ViewX == 0) game.ScrollX = 0;
                else
                {
                    game.ViewX--;
                    game.ScrollX++;
                }
            }
        }

        private void PickupItem(Point<byte> grid)
        {
            if (!grid.IsSet) return;

            var entity = GetGridObject(grid);
            if (entity.HasScoreValue) game.AddScore(entity.ScoreValue);
            if (entity.Is(Entity.Jetpack)) game.Jetpack = 0xff;
            else if (entity.Is(Entity.Trophy)) game.Trophy = true;
            else if (entity.Is(Entity.Gun)) game.Gun = true;

            game.SelectedLevel.Tiles[GetTileIndex(grid)] = 0;
            game.CheckPickup.Reset();
        }

        private void UpdateDBullet()
        {
            if (game.DBulletP.IsEmpty) return;

            if (!IsClear(game.DBulletP, isDave: false)) game.DBulletP.Reset();

            Point<byte> grid = OnGrid(game.DBulletP);
            if (grid.X - game.ViewX < 1 || grid.X - game.ViewX > 20) game.DBulletP.Reset();

            if (game.DBulletP.X != 0)
            {
                game.DBulletP.X += (ushort)((sbyte)game.DBulletDir * 4);

                foreach (var monster in game.ActiveMonsters)
                {
                    Point<byte> m = new(x: monster.Monster.X, y: monster.Monster.Y);
                    if ((grid.Y == m.Y || grid.Y == m.Y + 1) && (grid.X == m.X || grid.X == m.X + 1))
                    {
                        game.DBulletP.Reset();
                        monster.DeadTimer.Start();
                        game.AddScore(monster.Type?.ScoreValue ?? 0);
                    }
                }
            }
        }

        private void UpdateEBullet()
        {
            if (game.EBulletP.IsEmpty) return;

            if (!IsClear(game.EBulletP, isDave: false)) game.EBulletP.Reset();
            if (!IsVisible(game.EBulletP.X)) game.EBulletP.Reset();
            if (game.EBulletP.X != 0)
            {
                game.EBulletP.X += (ushort)((sbyte)game.EBulletDir * 4);

                Point<byte> grid = OnGrid(game.EBulletP);
                if (grid.X == game.Dave.X && grid.Y == game.Dave.Y)
                {
                    game.EBulletP.Reset();
                    game.DaveDeadTimer.Start();
                }
            }
        }

        public bool Quit => game.Quit;
    }
}