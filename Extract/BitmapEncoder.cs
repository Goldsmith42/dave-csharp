using System.Collections;
using System.Text;

namespace DaveCsharp.Extract
{
    class BitmapEncoder
    {
        public record ImageData(IEnumerable<byte> Buffer, uint Width, uint Height);

        public class Format : IEnumerable<char>
        {
            public static Format BGRA32 = new("bgra", 32);

            private readonly string orderString;
            private readonly uint bits;

            private Format(string order, uint bits)
            {
                orderString = order;
                this.bits = bits;
            }

            public IEnumerator<char> GetEnumerator() => orderString.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => orderString.GetEnumerator();

            public override string ToString() => orderString + bits;
        }

        class ByteArrayWriter
        {
            private readonly byte[] bytes;

            public ByteArrayWriter(byte[] bytes)
            {
                if (bytes is null) throw new ArgumentException("Byte array cannot be null", nameof(bytes));
                this.bytes = bytes;
            }
            public ByteArrayWriter(uint length) : this(new byte[length]) { }

            public int Position { private set; get; } = 0;

            public byte[] Bytes
            {
                get
                {
                    var result = new byte[bytes.Length];
                    bytes.CopyTo(result, 0);
                    return result;
                }
            }

            private void Write(byte[] newBytes)
            {
                Array.Copy(newBytes, 0, bytes, Position, newBytes.Length);
                Position += newBytes.Length;
            }

            public void Write(string str)
            {
                var strBytes = Encoding.ASCII.GetBytes(str) ?? throw new ArgumentException("Invalid parameter for byte array string", nameof(str));
                Write(strBytes);
            }
            internal void Write(uint value) => Write(BitConverter.GetBytes(value));
            internal void Write(int value) => Write(BitConverter.GetBytes(value));
            internal void Write(ushort value) => Write(BitConverter.GetBytes(value));

            internal void Set(long position, byte value) => bytes[position] = value;

            internal void Fill(byte value, long start, long end)
            {
                var newBytes = new byte[end - start];
                Array.Fill(newBytes, value);
                Array.Copy(newBytes, 0, bytes, start, newBytes.Length);
            }
        }

        const uint OFFSET = 54;

        private readonly byte[] buffer;
        private readonly uint width;
        private readonly uint height;
        private readonly Format format;
        private readonly uint extraBytes;
        private readonly uint rgbSize;
        private readonly uint fileSize;

        public BitmapEncoder(ImageData imageData, Format format)
        {
            buffer = imageData.Buffer.ToArray();
            width = imageData.Width;
            height = imageData.Height;
            this.format = format;

            extraBytes = width % 4;
            rgbSize = height * ((3 * width) + extraBytes);
            fileSize = rgbSize + OFFSET;
        }

        public byte[] Encode()
        {
            ByteArrayWriter tempBuffer = new(fileSize);
            tempBuffer.Write("BM");         // flag
            tempBuffer.Write(fileSize);
            tempBuffer.Write((uint)0);      // reserved
            tempBuffer.Write(OFFSET);

            tempBuffer.Write((uint)40);     // header info size
            tempBuffer.Write(width);
            tempBuffer.Write((int)-height);
            tempBuffer.Write((ushort)1);    // planes

            tempBuffer.Write((ushort)24);
            tempBuffer.Write((uint)0);
            tempBuffer.Write(rgbSize);
            tempBuffer.Write(0);            // hr
            tempBuffer.Write(0);            // vr
            tempBuffer.Write(0);            // colors
            tempBuffer.Write(0);            // important colors

            var i = 0;
            var rowBytes = 3 * width + extraBytes;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var p = tempBuffer.Position + y * rowBytes + x * 3;
                    foreach (var channel in format)
                    {
                        switch (channel)
                        {
                            case 'b': 
                                if (i < buffer.Length) tempBuffer.Set(p, buffer[i]);
                                i++;
                                break;
                            case 'g':
                                if (i < buffer.Length) tempBuffer.Set(p + 1, buffer[i]);
                                i++;
                                break;
                            case 'r':
                                if (i < buffer.Length) tempBuffer.Set(p + 2, buffer[i]);
                                i++;
                                break;
                            case 'a': i++; break;
                        }
                    }
                }
                if (extraBytes > 0)
                {
                    var fillOffset = tempBuffer.Position + y * rowBytes + width * 3;
                    tempBuffer.Fill(0, fillOffset, fillOffset + extraBytes);
                }
            }

            return tempBuffer.Bytes;
        }
    }
}