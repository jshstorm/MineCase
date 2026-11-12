using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x36)]
    [GeneratePacketSerializer]
    public sealed partial class ClientboundPositionAndLook : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Double)]
        public double X;

        [Id(1)]
        [SerializeAs(DataType.Double)]
        public double Y;

        [Id(2)]
        [SerializeAs(DataType.Double)]
        public double Z;

        [Id(3)]
        [SerializeAs(DataType.Float)]
        public float Yaw;

        [Id(4)]
        [SerializeAs(DataType.Float)]
        public float Pitch;

        [Id(5)]
        [SerializeAs(DataType.Byte)]
        public byte Flags;

        [Id(6)]
        [SerializeAs(DataType.VarInt)]
        public uint TeleportId;
    }

    [GenerateSerializer]
    [Packet(0x12)]
    [GeneratePacketSerializer]
    public sealed partial class ServerboundPositionAndLook : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Double)]
        public double X;

        [Id(1)]
        [SerializeAs(DataType.Double)]
        public double FeetY;

        [Id(2)]
        [SerializeAs(DataType.Double)]
        public double Z;

        [Id(3)]
        [SerializeAs(DataType.Float)]
        public float Yaw;

        [Id(4)]
        [SerializeAs(DataType.Float)]
        public float Pitch;

        [Id(5)]
        [SerializeAs(DataType.Boolean)]
        public bool OnGround;
    }
}
