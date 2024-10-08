using DaveCsharp.Common;
using DaveCsharp.Common.Configuration;

using SDL2;

namespace DaveCsharp.Extract
{
    class ExtractLevels
    {
        private const uint LEVEL_ADDR = 0x26e0a;
        private const uint TITLE_LEVEL_ADDRESS = 0x25ea4;

        public static void Extract(OriginalExeLocation exeLocation) => Extract(exeLocation.Relative ?
            Path.Combine(Environment.CurrentDirectory, exeLocation.Path) :
            exeLocation.Path
        );
        public static void Extract(string exeFilePath)
        {
            Console.WriteLine("Original executable path is " + exeFilePath);

            DaveLevel[] levels = new DaveLevel[10];
            var path = "assets/levels/";
            Directory.CreateDirectory(path);
            using var reader = new BinaryFileReader(exeFilePath).Open(LEVEL_ADDR);
            string filename;
            for (uint j = 0; j < 10; j++)
            {
                levels[j] = new();

                filename = $"level{j}.dat";
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

            // The title level is special and contains only a 70-byte tile section.
            reader.Seek(TITLE_LEVEL_ADDRESS);
            filename = "leveltitle.dat";
            using var fOut2 = File.OpenWrite(Path.Combine(path, filename));
            byte[] titleTiles = new byte[DaveLevel.TITLE_TILES_SIZE];
            for (uint i = 0; i < titleTiles.Length; i++) titleTiles[i] = reader.ReadByte();
            fOut2.Write(titleTiles);
            Console.WriteLine($"Saving {filename} as level data");

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