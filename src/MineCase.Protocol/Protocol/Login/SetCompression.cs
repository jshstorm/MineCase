using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Login
{
    [GenerateSerializer]
    [Packet(Protocol.SetCompressionPacketId)]
    [GeneratePacketSerializer]
    public sealed partial class SetCompression : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint Threshold;
    }
}
