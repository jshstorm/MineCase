using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x2F)]
    public sealed class OpenWindow : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint WindowId;

        /// <summary>
        /// Menu type id from the 1.14+ registry (generic_9x3 = 2, crafting = 11, furnace = 13, ...).
        /// </summary>
        [Id(1)]
        [SerializeAs(DataType.VarInt)]
        public uint WindowType;

        [Id(2)]
        [SerializeAs(DataType.Chat)]
        public Chat WindowTitle;

        public void Deserialize(ref SpanReader br)
        {
            throw new NotImplementedException();
        }

        public void Serialize(BinaryWriter bw)
        {
            bw.WriteAsVarInt(WindowId, out _);
            bw.WriteAsVarInt(WindowType, out _);
            bw.WriteAsChat(WindowTitle);
        }
    }
}
