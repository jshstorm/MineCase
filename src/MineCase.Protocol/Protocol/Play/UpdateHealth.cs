using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x49)]
    [GeneratePacketSerializer]
    public sealed partial class UpdateHealth : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Float)]
        public float Health;

        [Id(1)]
        [SerializeAs(DataType.VarInt)]
        public uint Food;

        [Id(2)]
        [SerializeAs(DataType.Float)]
        public float FoodSaturation;
    }
}
