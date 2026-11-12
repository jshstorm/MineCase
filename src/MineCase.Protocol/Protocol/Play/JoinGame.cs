using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x26)]
    [GeneratePacketSerializer]
    public sealed partial class JoinGame : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Int)]
        public int EID;

        [Id(1)]
        [SerializeAs(DataType.Byte)]
        public byte GameMode;

        [Id(2)]
        [SerializeAs(DataType.Int)]
        public int Dimension;

        [Id(3)]
        [SerializeAs(DataType.Long)]
        public long HashedSeed;

        [Id(4)]
        [SerializeAs(DataType.Byte)]
        public byte MaxPlayers;

        [Id(5)]
        [SerializeAs(DataType.String)]
        public string LevelType;

        [Id(6)]
        [SerializeAs(DataType.VarInt)]
        public uint ViewDistance;

        [Id(7)]
        [SerializeAs(DataType.Boolean)]
        public bool ReducedDebugInfo;

        [Id(8)]
        [SerializeAs(DataType.Boolean)]
        public bool EnableRespawnScreen;
    }
}
