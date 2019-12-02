using System.Numerics;

namespace Maze.View
{
    public class LightSource
    {
        public Vector3 Position;
        public Vector3 Direction;
        public Vector3 Color;

        public LightSource(Vector3 position, Vector3 direction, Vector3 color)
        {
            Position = position;
            Direction = direction;
            Color = color;
        }

        public static LightSource DefaultLightSource = new LightSource(Vector3.Zero, Vector3.Zero, Vector3.Zero);
    }
}