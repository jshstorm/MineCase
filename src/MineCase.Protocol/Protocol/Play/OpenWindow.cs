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
        [SerializeAs(DataType.Byte)]
        public byte WindowId;

        [Id(1)]
        [SerializeAs(DataType.String)]
        public string WindowType;

        [Id(2)]
        [SerializeAs(DataType.Chat)]
        public Chat WindowTitle;

        [Id(3)]
        [SerializeAs(DataType.Byte)]
        public byte NumberOfSlots;

        [Id(4)]
        [SerializeAs(DataType.Byte)]
        public byte? EntityId;

        public void Deserialize(ref SpanReader br)
        {
            throw new NotImplementedException();
        }

        public void Serialize(BinaryWriter bw)
        {
            bw.WriteAsByte(WindowId);
            bw.WriteAsString(WindowType);
            bw.WriteAsChat(WindowTitle);
            bw.WriteAsByte(NumberOfSlots);
            if (EntityId.HasValue)
                bw.WriteAsByte(EntityId.Value);
        }
    }
}
