using System.IO;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x09)]
    [GeneratePacketSerializer]
    public sealed partial class BlockBreakAnimation : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint EntityID;

        [Id(1)]
        [SerializeAs(DataType.Position)]
        public Position BlockPosition;

        [Id(2)]
        [SerializeAs(DataType.Byte)]
        public byte DestoryStage;
    }
}
