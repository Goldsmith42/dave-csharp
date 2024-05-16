using DaveCsharp.Common;
using SDL2;

namespace DaveCsharp.Extract
{
    class ExtractLevels
    {
        private const uint LEVEL_ADDR = 0x26e0a;

        public static void Extract() => Extract(Path.Combine(Environment.CurrentDirectory, "../original-game/DAVE.EXENEW"));
        public static void Extract(string exeFilePath)
        {
            DaveLevel[] levels = new DaveLevel[10];
            var path = "assets/levels/";
            Directory.CreateDirectory(path);
            using var reader = new BinaryFileReader(exeFilePath).Open(LEVEL_ADDR);
            for (uint j = 0; j < 10; j++)
            {
                levels[j] = new();

                var filename = $"level{j}.dat";
                using var fOut = File.OpenWrite(Path.Combine(path, filename));
                for (uint i = 0; i < levels[j].Path.Length; i++)
                    levels[j].Path[i] = reader.ReadByte();
                fOut.Write(levels[j].Path);

                for (uint i = 0; i < levels[j].Tiles.Length; i++)
                    levels[j].Tiles[i] = reader.ReadByte();
                fOut.Write(levels[j].Tiles);

                for (uint i = 0; i < levels[j].Padding.Length; i++)
                    levels[j].Padding[i] = reader.ReadByte();
                fOut.Write(levels[j].Padding);

                Console.WriteLine($"Saving {filename} as level data");
            }

            var tiles = new nint[158];
            for (uint i = 0; i < 158; i++) tiles[i] = SDL.SDL_LoadBMP(Path.Combine("assets/tiles/", $"tile{i}.bmp"));

            var map = SDL.SDL_CreateRGBSurface(0, 1600, 1600, 32, 0, 0, 0, 0);
            SDL.SDL_Rect dest = new();
            for (uint k = 0; k < 10; k++)
                for (uint j = 0; j < 10; j++)
                    for (uint i = 0; i < 100; i++)
                    {
                        var tileIndex = levels[k].Tiles[j * 100 + i];
                        dest.x = (int)(i * 16);
                        dest.y = (int)(k * 160 + j * 16);
                        dest.w = 16;
                        dest.h = 16;
                        SDL.SDL_Rect src = new() { w = dest.w, h = dest.h };
                        _ = SDL.SDL_BlitSurface(tiles[tileIndex], ref src, map, ref dest);
                    }
            SDL.SDL_SaveBMP(map, "assets/tiles/map.bmp");
        }
    }
}