using System.Collections.Generic;
using System.Numerics;
using Maze.Shared;

namespace Maze.View.Rendering.Management
{
    public interface IUpdateTransforms : IRenderInstance
    {
        void UpdateTransforms(Dictionary<Enums.EntityType, Matrix4x4[]> transforms);
    }
}
