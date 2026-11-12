using System;
using System.Collections.Generic;
using System.Text;
using Orleans;

namespace MineCase.Server.Statistics
{
    [GenerateSerializer]
    public class ServerVersion
    {
        [Id(0)]
        public string Name { get; set; }

        [Id(1)]
        public uint Protocol { get; set; }
    }
}
