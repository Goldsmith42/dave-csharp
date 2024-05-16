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
            var result = BitConverter.ToUInt32(bytes.Skip(offset).Take(4).ToArray());
            offset += 4;
            return result;
        }

        public IBinaryReader Seek(long offset)
        {
            this.offset = (int)offset;
            return this;
        }
    }
}