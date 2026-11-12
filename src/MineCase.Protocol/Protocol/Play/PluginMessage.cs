using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x0B)]
    [GeneratePacketSerializer]
    public sealed partial class ServerboundPluginMessage : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.String)]
        public string Channel;

        [Id(1)]
        [SerializeAs(DataType.ByteArray)]
        public byte[] Data;
    }
}
