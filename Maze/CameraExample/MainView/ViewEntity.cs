using Maze.Shared;
using System.Numerics;

namespace Maze.View
{
    public struct ViewEntity
    {
        public Enums.EntityType Type { get; }
        public Matrix4x4 Transform { get; }

        public ViewEntity(Enums.EntityType type, Matrix4x4 transformation)
        {
            Type = type;
            Transform = transformation;
        }
    }
}
