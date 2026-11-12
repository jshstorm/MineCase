using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x16)]
    [GeneratePacketSerializer]
    public sealed partial class WindowProperty : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Byte)]
        public byte WindowId;

        [Id(1)]
        [SerializeAs(DataType.Short)]
        public short Property;

        [Id(2)]
        [SerializeAs(DataType.Short)]
        public short Value;
    }
}
