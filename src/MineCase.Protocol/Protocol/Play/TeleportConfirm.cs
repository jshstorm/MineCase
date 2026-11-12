using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x00)]
    [GeneratePacketSerializer]
    public sealed partial class TeleportConfirm : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint TeleportId;
    }
}
