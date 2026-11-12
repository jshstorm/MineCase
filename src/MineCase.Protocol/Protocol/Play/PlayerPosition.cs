using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x11)]
    [GeneratePacketSerializer]
    public sealed partial class PlayerPosition : IPacket
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
        [SerializeAs(DataType.Boolean)]
        public bool OnGround;
    }
}
