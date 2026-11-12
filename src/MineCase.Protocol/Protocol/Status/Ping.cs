using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Status
{
    [GenerateSerializer]
    [Packet(0x01)]
    [GeneratePacketSerializer]
    public sealed partial class Ping : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Long)]
        public long Payload;
    }

    [GenerateSerializer]
    [Packet(0x01)]
    [GeneratePacketSerializer]
    public sealed partial class Pong : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Long)]
        public long Payload;
    }
}
