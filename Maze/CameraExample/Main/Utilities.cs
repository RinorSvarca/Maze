using Maze.Model;
using Maze.View;
using System.Collections.Generic;
using System.Linq;

namespace Maze.Controller
{
    public static class Utilities
    {
        public static List<ViewEntity> ToViewEntities(this IEnumerable<Entity> entities)
        {
            return entities.Select(entity => new ViewEntity(entity.Type, entity.Transformation)).ToList();
        }
    }
}
