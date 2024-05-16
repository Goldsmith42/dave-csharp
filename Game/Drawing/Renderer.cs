using SDL2;

namespace DaveCsharp.Game.Drawing
{
    public class Renderer
    {
        public record Color(byte R, byte G, byte B, byte A);

        private readonly nint renderer;

        public Renderer(int displayScale)
        {
            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) != 0)
                SDL.SDL_Log("SDL error: " + SDL.SDL_GetError());

            if (SDL.SDL_CreateWindowAndRenderer(320 * displayScale, 200 * displayScale, 0, out nint window, out nint renderer) != 0)
                SDL.SDL_Log("Window/renderer error: " + SDL.SDL_GetError());
            this.renderer = renderer;

            _ = SDL.SDL_RenderSetScale(renderer, displayScale, displayScale);
        }

        public nint LoadTexture(Surface surface) => LoadTexture(surface.Pointer);
        public nint LoadTexture(nint surface) => SDL.SDL_CreateTextureFromSurface(renderer, surface);
        public nint LoadTextureFromFile(string file) => LoadTexture(SDL.SDL_LoadBMP(file));


        public void RenderTexture(nint tile, SDL.SDL_Rect dest) => _ = SDL.SDL_RenderCopy(renderer, tile, 0, ref dest);

        public void RenderColor(Color color, SDL.SDL_Rect dest)
        {
            _ = SDL.SDL_SetRenderDrawColor(renderer, color.R, color.G, color.B, color.A);
            _ = SDL.SDL_RenderFillRect(renderer, ref dest);
        }

        public void Clear()
        {
            _ = SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 0);
            _ = SDL.SDL_RenderClear(renderer);
        }

        internal void RenderScreen() => SDL.SDL_RenderPresent(renderer);
    }
}