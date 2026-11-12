using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x05)]
    [GeneratePacketSerializer]
    public sealed partial class SpawnPlayer : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint EntityId;

        [Id(1)]
        [SerializeAs(DataType.UUID)]
        public Guid PlayerUUID;

        [Id(2)]
        [SerializeAs(DataType.Double)]
        public double X;

        [Id(3)]
        [SerializeAs(DataType.Double)]
        public double Y;

        [Id(4)]
        [SerializeAs(DataType.Double)]
        public double Z;

        [Id(5)]
        [SerializeAs(DataType.Angle)]
        public Angle Yaw;

        [Id(6)]
        [SerializeAs(DataType.Angle)]
        public Angle Pitch;
    }
}
