using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Login
{
    [GenerateSerializer]
    [Packet(0x00)]
    [GeneratePacketSerializer]
    public sealed partial class LoginStart : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.String)]
        public string Name;
    }

    [GenerateSerializer]
    [Packet(0x00)]
    [GeneratePacketSerializer]
    public sealed partial class LoginDisconnect : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.String)]
        public string Reason;
    }

    [GenerateSerializer]
    [Packet(0x02)]
    [GeneratePacketSerializer]
    public sealed partial class LoginSuccess : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.String)]
        public string UUID;

        [Id(1)]
        [SerializeAs(DataType.String)]
        public string Username;
    }
}
