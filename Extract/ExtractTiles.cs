using DaveCsharp.Common;
using DaveCsharp.Common.Configuration;

namespace DaveCsharp.Extract
{
    public static class ExtractTiles
    {
        private static byte[] ConvertDictionary(Dictionary<uint, byte> values)
        {
            byte[] result = new byte[values.Count];
            foreach (var key in values.Keys) result[key] = values[key];
            return result;
        }

        public static void Extract(OriginalExeLocation exeLocation) => Extract(exeLocation.Relative ?
            Path.Combine(Environment.CurrentDirectory, exeLocation.Path) :
            exeLocation.Path
        );
        public static void Extract(string exeFilePath)
        {
            Console.WriteLine("Original executable path is " + exeFilePath);

            const uint VGA_DATA_ADDRESS = 0x120f0;
            const uint VGA_PAL_ADDRESS = 0x26b0a;

            byte[] outData;
            byte[] palette;
            uint finalLength;
            int i;
            using (var reader = new BinaryFileReader(exeFilePath).Open(VGA_DATA_ADDRESS))
            {
                finalLength = reader.ReadDword();
                int currentLength = 0;

                outData = new byte[150000];
                while (currentLength < finalLength)
                {
                    var byteBuffer = reader.ReadByte();
                    if ((byteBuffer & 0x80) != 0)
                    {
                        byteBuffer &= 0x7f;
                        byteBuffer++;
                        while (byteBuffer > 0)
                        {
                            outData[currentLength] = reader.ReadByte();
                            currentLength++;
                            byteBuffer--;
                        }
                    }
                    else
                    {
                        byteBuffer += 3;
                        var next = reader.ReadByte();
                        while (byteBuffer > 0)
                        {
                            outData[currentLength] = next;
                            currentLength++;
                            byteBuffer--;
                        }
                    }
                }

                reader.Seek(VGA_PAL_ADDRESS);

                palette = new byte[768];
                for (i = 0; i < 256; i++)
                    for (var j = 0; j < 3; j++)
                    {
                        palette[(i * 3) + j] = reader.ReadByte();
                        palette[(i * 3) + j] <<= 2;
                    }
            }

            ByteArrayReader outReader = new(outData);
            var tileCount = outReader.ReadDword();
            var tileIndex = new uint[500];
            for (i = 0; i < tileCount; i++) tileIndex[i] = outReader.Seek(i * 4 + 4).ReadDword();
            tileIndex[i] = finalLength;

            for (byte currentTile = 0; currentTile < tileCount; currentTile++)
            {
                uint currentTileByte = 0;
                uint currentByte = tileIndex[currentTile];

                ushort tileWidth = 16;
                ushort tileHeight = 16;

                if (currentByte > 0xff00) currentByte++;

                if (outData[currentByte + 1] == 0 && outData[currentByte + 3] == 0)
                    if ((outData[currentByte] is > 0 and < 0xbf) && (outData[currentByte + 2] is > 0 and < 0x64))
                    {
                        tileWidth = outData[currentByte];
                        tileHeight = outData[currentByte + 2];
                        currentByte += 4;
                    }

                Dictionary<uint, byte> dstBytes = [];
                for (; currentByte < tileIndex[currentTile + 1]; currentByte++)
                {
                    byte srcByte = outData[currentByte];
                    byte redP = palette[srcByte * 3];
                    byte greenP = palette[srcByte * 3 + 1];
                    byte blueP = palette[srcByte * 3 + 2];

                    dstBytes[currentTileByte * 4] = blueP;
                    dstBytes[currentTileByte * 4 + 1] = greenP;
                    dstBytes[currentTileByte * 4 + 2] = redP;
                    dstBytes[currentTileByte * 4 + 3] = 0xff;

                    currentTileByte++;
                }

                var path = "assets/tiles/";
                Directory.CreateDirectory(path);
                var filePath = Path.Combine(path, $"tile{currentTile}.bmp");
                var data = new BitmapEncoder(new BitmapEncoder.ImageData(ConvertDictionary(dstBytes), tileWidth, tileHeight), BitmapEncoder.Format.BGRA32).Encode();
                File.WriteAllBytes(filePath, data);
            }
        }
    }
}