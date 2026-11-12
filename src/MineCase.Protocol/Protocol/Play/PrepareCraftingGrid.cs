using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x01)]
    [GeneratePacketSerializer]
    public sealed partial class PrepareCraftingGrid : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Byte)]
        public byte WindowId;

        [Id(1)]
        [SerializeAs(DataType.Short)]
        public short ActionNumber;
    }
}
