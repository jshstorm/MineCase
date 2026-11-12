using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x0A)]
    [GeneratePacketSerializer]
    public sealed partial class ServerboundCloseWindow : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Byte)]
        public byte WindowId;
    }

    [GenerateSerializer]
    [Packet(0x14)]
    [GeneratePacketSerializer]
    public sealed partial class ClientboundCloseWindow : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Byte)]
        public byte WindowId;
    }
}
