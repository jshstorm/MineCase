using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x41)]
    [GeneratePacketSerializer]
    public sealed partial class UpdateViewPosition : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public int ChunkX;

        [Id(1)]
        [SerializeAs(DataType.VarInt)]
        public int ChunkZ;
    }
}
