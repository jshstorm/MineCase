using System;
using System.Collections.Generic;
using System.Text;
using Orleans;

namespace MineCase
{
    [GenerateSerializer]
    public struct WorldTime
    {
        [Id(0)]
        public long WorldAge { get; set; }

        [Id(1)]
        public long TimeOfDay { get; set; }
    }
}
