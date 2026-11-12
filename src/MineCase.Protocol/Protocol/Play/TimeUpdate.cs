using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x4F)]
    [GeneratePacketSerializer]
    public sealed partial class TimeUpdate : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Long)]
        public long WorldAge;

        [Id(1)]
        [SerializeAs(DataType.Long)]
        public long TimeOfDay;
    }
}
