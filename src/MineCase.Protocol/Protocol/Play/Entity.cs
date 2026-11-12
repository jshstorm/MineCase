using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    // FIXME: 1.15.2 no longer has this packet
    [GenerateSerializer]
    [Packet(0x2C)]
    [GeneratePacketSerializer]
    public sealed partial class Entity : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint EID;
    }
}
