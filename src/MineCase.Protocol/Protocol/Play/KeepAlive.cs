using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x0F)]
    [GeneratePacketSerializer]
    public sealed partial class ServerboundKeepAlive : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Long)]
        public long KeepAliveId;
    }

    [GenerateSerializer]
    [Packet(0x21)]
    [GeneratePacketSerializer]
    public sealed partial class ClientboundKeepAlive : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Long)]
        public long KeepAliveId;
    }
}
