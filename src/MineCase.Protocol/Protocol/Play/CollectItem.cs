using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x56)]
    [GeneratePacketSerializer]
    public sealed partial class CollectItem : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint CollectedEntityId;

        [Id(1)]
        [SerializeAs(DataType.VarInt)]
        public uint CollectorEntityId;

        [Id(2)]
        [SerializeAs(DataType.VarInt)]
        public uint PickupItemCount;
    }
}
