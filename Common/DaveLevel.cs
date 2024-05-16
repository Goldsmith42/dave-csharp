namespace DaveCsharp.Common
{
    readonly struct DaveLevel
    {
        public DaveLevel() {}

        public byte[] Path { get; } = new byte[256];
        public byte[] Tiles { get; } = new byte[1000];
        public byte[] Padding { get; } = new byte[24];
    }
}