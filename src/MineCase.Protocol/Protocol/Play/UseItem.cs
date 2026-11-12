using System;
using System.Collections.Generic;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x2D)]
    [GeneratePacketSerializer]
    public sealed partial class UseItem : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public Hand Hand;
    }
}
