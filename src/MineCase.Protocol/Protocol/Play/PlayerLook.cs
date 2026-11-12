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
    [Packet(0x10)]
    [GeneratePacketSerializer]
    public sealed partial class PlayerLook : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Float)]
        public float Yaw;

        [Id(1)]
        [SerializeAs(DataType.Float)]
        public float Pitch;

        [Id(2)]
        [SerializeAs(DataType.Boolean)]
        public bool OnGround;
    }
}
