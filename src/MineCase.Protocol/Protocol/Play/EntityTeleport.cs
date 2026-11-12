using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x57)]
    [GeneratePacketSerializer]
    public sealed partial class EntityTeleport : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint EID;

        [Id(1)]
        [SerializeAs(DataType.Double)]
        public double X;

        [Id(2)]
        [SerializeAs(DataType.Double)]
        public double Y;

        [Id(3)]
        [SerializeAs(DataType.Double)]
        public double Z;

        [Id(4)]
        [SerializeAs(DataType.Angle)]
        public Angle Yaw;

        [Id(5)]
        [SerializeAs(DataType.Angle)]
        public Angle Pitch;

        [Id(6)]
        [SerializeAs(DataType.Boolean)]
        public bool OnGround;
    }
}
