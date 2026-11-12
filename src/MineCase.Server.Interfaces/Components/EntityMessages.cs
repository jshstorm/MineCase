using System;
using System.Collections.Generic;
using System.Text;
using MineCase.Engine;
using MineCase.Server.Game.Entities;
using MineCase.World;
using Orleans;
using Orleans.Concurrency;

namespace MineCase.Server.Components
{
    [GenerateSerializer]
    [Immutable]
    public sealed class GameTick : IEntityMessage
    {
        [Id(0)]
        public GameTickArgs Args { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class Disable : IEntityMessage
    {
        public static readonly Disable Default = new Disable();
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class Enable : IEntityMessage
    {
        public static readonly Enable Default = new Enable();
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class CollisionWith : IEntityMessage
    {
        [Id(0)]
        public IReadOnlyCollection<IDependencyObject> Entities { get; set; }
    }
}
