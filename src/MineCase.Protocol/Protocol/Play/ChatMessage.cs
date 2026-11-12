using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x03)]
    [GeneratePacketSerializer]
    public sealed partial class ServerboundChatMessage : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.String)]
        public string Message;
    }

    // TODO
    [GenerateSerializer]
    [Packet(0x0F)]
    [GeneratePacketSerializer]
    public sealed partial class ClientboundChatMessage : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Chat)]
        public Chat JSONData;

        [Id(1)]
        [SerializeAs(DataType.Byte)]
        public byte Position; // 0: chat (chat box), 1: system message (chat box), 2: game info (above hotbar).
    }
}
