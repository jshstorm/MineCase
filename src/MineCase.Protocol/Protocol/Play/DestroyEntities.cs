using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x38)]
    [GeneratePacketSerializer]
    public sealed partial class DestroyEntities : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint Count;

        [Id(1)]
        [SerializeAs(DataType.VarIntArray, ArrayLengthMember = nameof(Count))]
        public uint[] EntityIds;
    }
}
