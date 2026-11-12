using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    // FIXME : 1.15.2 does not have this packet
    [GenerateSerializer]
    [Packet(0x29)]
    [GeneratePacketSerializer]
    public sealed partial class EntityRelativeMove : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint EID;

        [Id(1)]
        [SerializeAs(DataType.Short)]
        public short DeltaX;

        [Id(2)]
        [SerializeAs(DataType.Short)]
        public short DeltaY;

        [Id(3)]
        [SerializeAs(DataType.Short)]
        public short DeltaZ;

        [Id(4)]
        [SerializeAs(DataType.Boolean)]
        public bool OnGround;
    }
}
