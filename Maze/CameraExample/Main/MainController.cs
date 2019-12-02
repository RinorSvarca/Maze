﻿using Maze.Model;
using Maze.View;
using System;
using Zenseless.Base;
using Zenseless.ExampleFramework;
using Zenseless.OpenGL;

namespace Maze.Controller
{
    public class MainController
    {
        [STAThread]
        private static void Main()
        {
            GameTime gameTime = new GameTime();
            var window = new ExampleWindow();

            
            var orbit = window.GameWindow.CreateOrbitingCameraController(50f, 90, 0.1f, 500f);
            orbit.View.Azimuth = 250;
            orbit.View.Elevation = 45;
            orbit.View.TargetY = 10;
            var visual = new MainView(window.RenderContext.RenderState, window.ContentLoader);
            var model = new MainModel();
            window.Update += (period) => model.Update(gameTime.DeltaTime);
            window.Render += () => visual.Render(model.Entities.ToViewEntities(), gameTime.AbsoluteTime, orbit);
            window.Resize += visual.Resize;
            window.GameWindow.KeyDown += (sender, e) =>
            {
                if (e.Key == OpenTK.Input.Key.Space)
                {
                    visual.Bloom = !visual.Bloom;
                }
            };
            window.Run();
        }
    }
}