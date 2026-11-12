using System.IO;

using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x06)]
    [GeneratePacketSerializer]
    public sealed partial class ClientboundAnimation : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint EntityID;

        [Id(1)]
        [SerializeAs(DataType.Byte)]
        public ClientboundAnimationId AnimationID;
    }
}
