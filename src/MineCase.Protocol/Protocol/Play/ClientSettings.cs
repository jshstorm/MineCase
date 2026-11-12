using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x05)]
    [GeneratePacketSerializer]
    public sealed partial class ClientSettings : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.String)]
        public string Locale;

        [Id(1)]
        [SerializeAs(DataType.Byte)]
        public byte ViewDistance;

        [Id(2)]
        [SerializeAs(DataType.VarInt)]
        public uint ChatMode;

        [Id(3)]
        [SerializeAs(DataType.Boolean)]
        public bool ChatColors;

        [Id(4)]
        [SerializeAs(DataType.Byte)]
        public byte DisplayedSkinParts;

        [Id(5)]
        [SerializeAs(DataType.VarInt)]
        public uint MainHand;
    }
}
