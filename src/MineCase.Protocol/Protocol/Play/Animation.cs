using System;
using System.Collections.Generic;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    public enum Hand : uint
    {
        Main = 0,
        Off = 1
    }

    [GenerateSerializer]
    [Packet(0x2A)]
    [GeneratePacketSerializer]
    public sealed partial class ServerboundAnimation : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public Hand Hand;
    }
}
