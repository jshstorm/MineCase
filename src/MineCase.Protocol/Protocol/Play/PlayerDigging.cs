using System;
using System.Collections.Generic;
using System.Text;

using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    public enum PlayerDiggingStatus : uint
    {
        StartedDigging = 0,
        CancelledDigging = 1,
        FinishedDigging = 2,
        DropItemStack = 3,
        DropItem = 4,
        ShootArrowOrFinishEating = 5,
        SwapItemInHand = 6
    }

    [GenerateSerializer]
    public enum PlayerDiggingFace : byte
    {
        Bottom = 0,
        Top = 1,
        North = 2,
        South = 3,
        West = 4,
        East = 5,
        Special = 255
    }

    [GenerateSerializer]
    [Packet(0x1A)]
    [GeneratePacketSerializer]
    public sealed partial class PlayerDigging : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public PlayerDiggingStatus Status;

        [Id(1)]
        [SerializeAs(DataType.Position)]
        public Position Location;

        [Id(2)]
        [SerializeAs(DataType.Byte)]
        public PlayerDiggingFace Face;
    }
}
