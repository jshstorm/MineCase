using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x0C)]
    [GeneratePacketSerializer]
    public sealed partial class BlockChange : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Position)]
        public Position Location;

        [Id(1)]
        [SerializeAs(DataType.VarInt)]
        public uint BlockId;
    }
}
