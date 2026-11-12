using System;
using System.Collections.Generic;
using System.Text;
using Orleans;

namespace MineCase
{
    [GenerateSerializer]
    public struct GameMode
    {
        [GenerateSerializer]
        public enum Class : byte
        {
            Survival = 0,
            Creative = 1,
            Adventure = 2,
            Spectator = 3
        }

        [Id(0)]
        public Class ModeClass { get; set; }

        [Id(1)]
        public bool IsHardcore { get; set; }
    }
}
