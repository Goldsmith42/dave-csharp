namespace DaveCsharp.Common
{
    public class BinaryFileReader(string filePath) : IDisposable, IBinaryReader
    {
        private readonly string filePath = filePath;
        private FileStream? stream;

        public BinaryFileReader Open(long? offset = null)
        {
            stream = File.OpenRead(filePath);
            if (offset.HasValue) stream.Seek(offset.Value, SeekOrigin.Begin);
            return this;
        }

        public IBinaryReader Seek(long offset)
        {
            if (stream is null) return Open(offset);
            
            stream.Seek(offset, SeekOrigin.Begin);
            return this;
        }

        public uint ReadDword()
        {
            if (stream is null) Open();

            var bytes = new byte[4];
            stream!.ReadExactly(bytes, 0, 4);
            return BitConverter.ToUInt32(bytes);
        }

        public byte ReadByte()
        {
            if (stream is null) Open();
            return (byte)stream!.ReadByte();
        }


        public void Dispose()
        {
            GC.SuppressFinalize(this);
            stream?.Close();
        }
    }
}