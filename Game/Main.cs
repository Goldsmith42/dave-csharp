using DaveCsharp.Common.Configuration;
using DaveCsharp.Game.Drawing;

using SDL2;

namespace DaveCsharp.Game
{
    public static class Main
    {
        public static void Start(OriginalExeLocation? originalExeLocation)
        {
            GameAssets.Verify(originalExeLocation);

            const byte DISPLAY_SCALE = 3;
            uint timerBegin;
            uint timerEnd;
            uint delay;

            GameAssets assets = new();
            Game game = new();
            game.Init();

            Renderer renderer = new(DISPLAY_SCALE);
            assets.Init(renderer);

            game.StartLevel();

            while (!game.Quit)
            {
                timerBegin = SDL.SDL_GetTicks();

                game.CheckInput();
                game.Update();
                game.Render(renderer, assets);

                timerEnd = SDL.SDL_GetTicks();

                delay = 33 - (timerEnd - timerBegin);
                delay = delay > 33 ? 0 : delay;
                SDL.SDL_Delay(delay);
            }

            SDL.SDL_Quit();
        }
    }
}