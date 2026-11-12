using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x03)]
    [GeneratePacketSerializer]
    public sealed partial class SpawnMob : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint EID;

        [Id(1)]
        [SerializeAs(DataType.UUID)]
        public Guid EntityUUID;

        [Id(2)]
        [SerializeAs(DataType.Byte)]
        public byte Type;

        [Id(3)]
        [SerializeAs(DataType.Double)]
        public double X;

        [Id(4)]
        [SerializeAs(DataType.Double)]
        public double Y;

        [Id(5)]
        [SerializeAs(DataType.Double)]
        public double Z;

        [Id(6)]
        [SerializeAs(DataType.Angle)]
        public Angle Pitch;

        [Id(7)]
        [SerializeAs(DataType.Angle)]
        public Angle Yaw;

        [Id(8)]
        [SerializeAs(DataType.Angle)]
        public Angle HeadPitch;

        [Id(9)]
        [SerializeAs(DataType.Short)]
        public short VelocityX;

        [Id(10)]
        [SerializeAs(DataType.Short)]
        public short VelocityY;

        [Id(11)]
        [SerializeAs(DataType.Short)]
        public short VelocityZ;

        [Id(12)]
        [SerializeAs(DataType.ByteArray)]
        public byte[] Metadata;
    }
}
