using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x3C)]
    [GeneratePacketSerializer]
    public sealed partial class EntityHeadLook : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint EID;

        [Id(1)]
        [SerializeAs(DataType.Angle)]
        public Angle Yaw;
    }
}
