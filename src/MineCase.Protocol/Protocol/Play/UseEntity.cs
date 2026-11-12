using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x0A)]
    public sealed class UseEntity : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public int Target;

        [Id(1)]
        [SerializeAs(DataType.VarInt)]
        public int Type;

        [Id(2)]
        [SerializeAs(DataType.Float)]
        public float? TargetX;

        [Id(3)]
        [SerializeAs(DataType.Float)]
        public float? TargetY;

        [Id(4)]
        [SerializeAs(DataType.Float)]
        public float? TargetZ;

        [Id(5)]
        [SerializeAs(DataType.VarInt)]
        public int? Hand;

        public void Deserialize(ref SpanReader br)
        {
            Target = (int)br.ReadAsVarInt(out _);
            Type = (int)br.ReadAsVarInt(out _);

            // Only if Type is interact at
            if (Type == 2)
            {
                TargetX = br.ReadAsFloat();
                TargetY = br.ReadAsFloat();
                TargetZ = br.ReadAsFloat();
            }

            // Only if Type is interact or interact at
            if (Type == 0 || Type == 2)
                Hand = (int)br.ReadAsVarInt(out _);
        }

        public void Serialize(BinaryWriter bw)
        {
            throw new NotImplementedException();
        }
    }
}
