using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x23)]
    [GeneratePacketSerializer]
    public sealed partial class ServerboundHeldItemChange : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Short)]
        public short Slot;
    }
}
