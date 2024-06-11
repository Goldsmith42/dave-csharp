using DaveCsharp.Common.Configuration;
using DaveCsharp.Game.Drawing;

namespace DaveCsharp.Game
{
    readonly struct GameAssets
    {
        class MissingConfigurationException : Exception
        {
            public MissingConfigurationException() : base("Cannot find configuration required to extract game data") {}
        }

        readonly nint[] graphicsTiles = new nint[158];

        public GameAssets() { }

        public unsafe readonly void Init(Renderer renderer)
        {
            for (uint i = 0; i < graphicsTiles.Length; i++)
            {
                using var surface = Surface.FromFile(Path.Combine("assets/tiles/", $"tile{i}.bmp"));
                if (i is >= 53 and <= 59 or 67 or 68 or >= 71 and <= 73 or >= 77 and <= 82)
                {
                    byte maskOffset = i switch
                    {
                        >= 53 and <= 59 => 7,
                        >= 67 and <= 68 => 2,
                        >= 71 and <= 73 => 3,
                        >= 77 and <= 82 => 6,
                        _ => throw new Exception("Mask offset not set correctly")
                    };
                    using var mask = Surface.FromFile(Path.Combine("assets/tiles/", $"tile{i + maskOffset}.bmp"));

                    var surfP = surface.Pixels;
                    var maskP = mask.Pixels;

                    for (var j = 0; j < mask.NumberOfValues; j++) if (maskP[j] != 0) surfP[j] = 0xff;

                    surface.SetColorKey(1, 0xff, 0xff, 0xff);
                }
                else if (i is >= 89 and <= 120 or >= 129 and <= 132)
                    surface.SetColorKey(1, 0, 0, 0);
                graphicsTiles[i] = renderer.LoadTexture(surface);
            }
        }

        public static void Verify(OriginalExeLocation? originalExeLocation)
        {
            if (originalExeLocation is null)
                Console.WriteLine("Missing original executable location... This can still work if the files were extracted.");

            for (uint i = 0; i <= 157; i++)
                if (!File.Exists(Path.Combine("assets/tiles", $"tile{i}.bmp")))
                {
                    Console.WriteLine("Missing tile file, will extract from executable");
                    if (originalExeLocation is null)
                        throw new MissingConfigurationException();
                    Extract.ExtractTiles.Extract(originalExeLocation);
                    break;
                }

            for (uint i = 0; i <= 10; i++)
                if (!File.Exists(Path.Combine("assets/levels", $"level{(i == 10 ? "title" : i)}.dat")))
                {
                    Console.WriteLine("Missing level file, will extract from executable");
                    if (originalExeLocation is null)
                        throw new MissingConfigurationException();
                    Extract.ExtractLevels.Extract(originalExeLocation);
                    break;
                }
        }

        internal readonly nint GetTile(byte tileIndex) => graphicsTiles[tileIndex];
        internal readonly nint GetTile(TileType tileIndex) => GetTile((byte)tileIndex);
    }
}