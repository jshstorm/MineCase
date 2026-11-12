using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x3B)]
    [GeneratePacketSerializer]
    public sealed partial class Respawn : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Int)]
        public int Dimension;

        [Id(1)]
        [SerializeAs(DataType.Long)]
        public long HashedSeed;

        [Id(2)]
        [SerializeAs(DataType.Byte)]
        public byte Gamemode;

        [Id(3)]
        [SerializeAs(DataType.String)]
        public string LevelType;
    }
}
