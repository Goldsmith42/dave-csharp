using SDL2;

namespace DaveCsharp.Game.Drawing
{
    public unsafe class Surface : IDisposable
    {
        private readonly SDL.SDL_Surface* surface;

        private Surface(SDL.SDL_Surface* surface) => this.surface = surface;
        private Surface(nint surface) : this((SDL.SDL_Surface*)surface) { }

        public static Surface FromFile(string file) => new(SDL.SDL_LoadBMP(file));

        public unsafe byte* Pixels => (byte*)surface->pixels;
        public int NumberOfValues => surface->pitch * surface->h;
        public nint Pointer => (nint)surface;

        public void SetColorKey(int flag, byte r, byte g, byte b) => _ = SDL.SDL_SetColorKey(Pointer, flag, SDL.SDL_MapRGB(surface->format, r, g, b));

        public void Dispose() => SDL.SDL_FreeSurface(Pointer);
    }
}