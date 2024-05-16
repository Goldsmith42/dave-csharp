namespace DaveCsharp.Common
{
    public interface IBinaryReader
    {
        public IBinaryReader Seek(long offset);
        public uint ReadDword();
        public byte ReadByte();
        public string ReadString(uint maxLength);
    }
}