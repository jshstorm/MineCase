using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x06)]
    [GeneratePacketSerializer]
    public sealed partial class ServerboundConfirmTransaction : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Byte)]
        public byte WindowId;

        [Id(1)]
        [SerializeAs(DataType.Short)]
        public short ActionNumber;

        [Id(2)]
        [SerializeAs(DataType.Boolean)]
        public bool Accepted;
    }

    [GenerateSerializer]
    [Packet(0x11)]
    [GeneratePacketSerializer]
    public sealed partial class ClientboundConfirmTransaction : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Byte)]
        public byte WindowId;

        [Id(1)]
        [SerializeAs(DataType.Short)]
        public short ActionNumber;

        [Id(2)]
        [SerializeAs(DataType.Boolean)]
        public bool Accepted;
    }
}
