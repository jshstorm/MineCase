using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x1E)]
    [GeneratePacketSerializer]
    public sealed partial class UnloadChunk : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Int)]
        public int ChunkX;

        [Id(1)]
        [SerializeAs(DataType.Int)]
        public int ChunkZ;
    }
}
