using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Handshaking
{
    [GenerateSerializer]
    [Packet(0x00)]
    [GeneratePacketSerializer]
    public sealed partial class Handshake : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint ProtocolVersion;

        [Id(1)]
        [SerializeAs(DataType.String)]
        public string ServerAddress;

        [Id(2)]
        [SerializeAs(DataType.UnsignedShort)]
        public ushort ServerPort;

        [Id(3)]
        [SerializeAs(DataType.VarInt)]
        public uint NextState;
    }
}
