using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Command;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x12)]
    public sealed class DeclareCommands : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint Count;

        [Id(1)]
        [SerializeAs(DataType.Array)]
        public Node[] Nodes;

        [Id(2)]
        [SerializeAs(DataType.VarInt)]
        public uint RootIndex;

        // TODO : complete serialization and deserialization
        public void Serialize(BinaryWriter bw)
        {
            bw.WriteAsVarInt(Count, out _);
        }

        public void Deserialize(ref SpanReader br)
        {
            Count = br.ReadAsVarInt(out _);
        }
    }
}
