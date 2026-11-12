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
    [Packet(0x2B)]
    [GeneratePacketSerializer]
    public sealed partial class EntityLook : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint EID;

        [Id(1)]
        [SerializeAs(DataType.Angle)]
        public Angle Yaw;

        [Id(2)]
        [SerializeAs(DataType.Angle)]
        public Angle Pitch;

        [Id(3)]
        [SerializeAs(DataType.Boolean)]
        public bool OnGround;
    }
}
