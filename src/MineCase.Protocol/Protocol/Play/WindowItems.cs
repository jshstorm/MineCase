using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x15)]
    [GeneratePacketSerializer]
    public sealed partial class WindowItems : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Byte)]
        public byte WindowId;

        [Id(1)]
        [SerializeAs(DataType.Short)]
        public short Count;

        [Id(2)]
        [SerializeAs(DataType.SlotArray, ArrayLengthMember = nameof(Count))]
        public Slot[] Slots;
    }
}
