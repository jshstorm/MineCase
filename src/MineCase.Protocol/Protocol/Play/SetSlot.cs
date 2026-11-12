using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x17)]
    [GeneratePacketSerializer]
    public sealed partial class SetSlot : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Byte)]
        public byte WindowId;

        [Id(1)]
        [SerializeAs(DataType.Short)]
        public short Slot;

        [Id(2)]
        [SerializeAs(DataType.Slot)]
        public Slot SlotData;
    }
}
