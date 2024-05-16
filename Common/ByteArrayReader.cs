using System.Text;

namespace DaveCsharp.Common
{
    public class ByteArrayReader(byte[] bytes) : IBinaryReader
    {
        private int offset = 0;

        public byte ReadByte()
        {
            byte result = bytes[offset];
            offset++;
            return result;
        }

        public uint ReadDword()
        {
            var result = BitConverter.ToUInt32(bytes.Skip(offset).Take(Size.INT32).ToArray());
            offset += Size.INT32;
            return result;
        }

        public string ReadString(uint maxLength)
        {
            var result = Encoding.ASCII.GetString(bytes.Skip(offset).Take((int)maxLength).ToArray());
            offset += (int)maxLength;
            return result;
        }

        public IBinaryReader Seek(long offset)
        {
            this.offset = (int)offset;
            return this;
        }
    }
}