using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x2C)]
    [GeneratePacketSerializer]
    public sealed partial class PlayerBlockPlacement : IPacket
    {
        [Id(2)]
        [SerializeAs(DataType.VarInt)]
        public Hand Hand;

        [Id(0)]
        [SerializeAs(DataType.Position)]
        public Position Location;

        [Id(1)]
        [SerializeAs(DataType.VarInt)]
        public PlayerDiggingFace Face;

        [Id(3)]
        [SerializeAs(DataType.Float)]
        public float CursorPositionX;

        [Id(4)]
        [SerializeAs(DataType.Float)]
        public float CursorPositionY;

        [Id(5)]
        [SerializeAs(DataType.Float)]
        public float CursorPositionZ;

        [Id(6)]
        [SerializeAs(DataType.Boolean)]
        public bool InsideBlock;
    }
}
