using System;
using System.Collections.Generic;
using System.Text;

using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x09)]
    [GeneratePacketSerializer]
    public sealed partial class ClickWindow : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Byte)]
        public byte WindowId;

        [Id(1)]
        [SerializeAs(DataType.Short)]
        public short Slot;

        [Id(2)]
        [SerializeAs(DataType.Byte)]
        public byte Button;

        [Id(3)]
        [SerializeAs(DataType.Short)]
        public short ActionNumber;

        [Id(4)]
        [SerializeAs(DataType.VarInt)]
        public uint Mode;

        [Id(5)]
        [SerializeAs(DataType.Slot)]
        public Slot ClickedItem;
    }
}
