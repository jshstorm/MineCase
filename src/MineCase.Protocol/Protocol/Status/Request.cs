using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Status
{
    [GenerateSerializer]
    [Packet(0x00)]
    [GeneratePacketSerializer]
    public sealed partial class Request : IPacket
    {
        public static readonly Request Empty = new Request();
    }

    [GenerateSerializer]
    [Packet(0x00)]
    [GeneratePacketSerializer]
    public sealed partial class Response : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.String)]
        public string JsonResponse;
    }
}
