using System;
using System.Collections.Generic;
using System.Text;
using Orleans;

namespace MineCase.Server.Game.Entities
{
    [GenerateSerializer]
    public sealed class PlayerDescription
    {
        [Id(0)]
        public Guid UUID { get; set; }

        [Id(1)]
        public string Name { get; set; }

        [Id(2)]
        public GameMode GameMode { get; set; }

        [Id(3)]
        public uint Ping { get; set; }

        [Id(4)]
        public string DisplayName { get; set; }
    }
}
