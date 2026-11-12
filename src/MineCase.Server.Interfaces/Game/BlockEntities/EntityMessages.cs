using System;
using System.Collections.Generic;
using System.Text;
using MineCase.Engine;
using MineCase.Server.Game.Entities;
using MineCase.Server.World;
using MineCase.World;
using Orleans;
using Orleans.Concurrency;

namespace MineCase.Server.Game.BlockEntities
{
    [GenerateSerializer]
    [Immutable]
    public sealed class SpawnBlockEntity : IEntityMessage
    {
        [Id(0)]
        public IWorld World { get; set; }

        [Id(1)]
        public BlockWorldPos Position { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class DestroyBlockEntity : IEntityMessage
    {
        public static readonly DestroyBlockEntity Default = new DestroyBlockEntity();
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class NeighborEntityChanged : IEntityMessage
    {
        public static readonly NeighborEntityChanged Empty = new NeighborEntityChanged();

        [Id(0)]
        public IBlockEntity Entity { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class UseBy : IEntityMessage
    {
        [Id(0)]
        public IEntity Entity { get; set; }
    }
}
