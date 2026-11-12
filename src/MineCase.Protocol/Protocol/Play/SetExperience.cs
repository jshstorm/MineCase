using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x48)]
    [GeneratePacketSerializer]
    public sealed partial class SetExperience : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Float)]
        public float ExperienceBar;

        [Id(1)]
        [SerializeAs(DataType.VarInt)]
        public uint Level;

        [Id(2)]
        [SerializeAs(DataType.VarInt)]
        public uint TotalExperience;
    }
}
