using System;
using System.Collections.Generic;
using System.Text;
using Orleans;

namespace MineCase.Graphics
{
    public enum ShapeType : uint
    {
        Cuboid = 0
    }

    [GenerateSerializer]
    [Serializable]
    public abstract class Shape
    {
        public abstract ShapeType Type { get; }

        public abstract bool CollideWith(Shape other);
    }
}
