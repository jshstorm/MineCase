using System;
using System.Collections.Generic;
using System.Text;
using Orleans;

namespace MineCase.World
{
    [GenerateSerializer]
    public sealed class GameTickArgs
    {
        [Id(0)]
        public TimeSpan DeltaTime { get; set; }

        [Id(1)]
        public long WorldAge { get; set; }

        [Id(2)]
        public long TimeOfDay { get; set; }
    }
}
