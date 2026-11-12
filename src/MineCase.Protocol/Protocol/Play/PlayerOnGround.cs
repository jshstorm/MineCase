using System;
using System.Collections.Generic;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    // FIXME : 1.15.2 does not have this packet
    [GenerateSerializer]
    [Packet(0x14)]
    [GeneratePacketSerializer]
    public sealed partial class PlayerOnGround : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Boolean)]
        public bool OnGround;
    }
}
