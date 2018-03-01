using System;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace GraphicsTutorial
{
    class Program
    {
        static void Main(string[] args)
        {
            var windowCI = new WindowCreateInfo()
            {
                X = 100,
                Y = 100,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowTitle = "Veldrid Tutorial"
            };

            Sdl2Window window = VeldridStartup.CreateWindow(ref windowCI);
            GraphicsDevice graphicsDevice = VeldridStartup.CreateGraphicsDevice(window);

            while (window.Exists)
            {
                window.PumpEvents();
            }
        }
    }
}
