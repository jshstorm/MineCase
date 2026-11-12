using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Login
{
    [GenerateSerializer]
    [Packet(0x01)]
    [GeneratePacketSerializer]
    public sealed partial class EncryptionRequest : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.String)]
        public string ServerID;

        [Id(1)]
        [SerializeAs(DataType.VarInt)]
        public uint PublicKeyLength;

        [Id(2)]
        [SerializeAs(DataType.ByteArray, ArrayLengthMember = nameof(PublicKeyLength))]
        public byte[] PublicKey;

        [Id(3)]
        [SerializeAs(DataType.VarInt)]
        public uint VerifyTokenLength;

        [Id(4)]
        [SerializeAs(DataType.ByteArray, ArrayLengthMember = nameof(VerifyTokenLength))]
        public byte[] VerifyToken;
    }

    [GenerateSerializer]
    [Packet(0x01)]
    [GeneratePacketSerializer]
    public sealed partial class EncryptionResponse : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint SharedSecretLength;

        [Id(1)]
        [SerializeAs(DataType.ByteArray, ArrayLengthMember = nameof(SharedSecretLength))]
        public byte[] SharedSecret;

        [Id(2)]
        [SerializeAs(DataType.VarInt)]
        public uint VerifyTokenLength;

        [Id(3)]
        [SerializeAs(DataType.ByteArray, ArrayLengthMember = nameof(VerifyTokenLength))]
        public byte[] VerifyToken;
    }
}
