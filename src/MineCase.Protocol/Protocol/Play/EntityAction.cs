using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    public enum ActionId : uint
    {
        StartSneaking = 0,
        StopSneaking = 1,
        LeaveBed = 2,
        StartSprinting = 3,
        StopSprinting = 4,
        StartJumpWithHorse = 5,
        StopJumpWithHorse = 6,
        OpenHorseInventory = 7,
        StartFlyingWithElytra = 8
    }

    [GenerateSerializer]
    [Packet(0x1B)]
    [GeneratePacketSerializer]
    public sealed partial class EntityAction : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint EntityId;

        [Id(1)]
        [SerializeAs(DataType.VarInt)]
        public ActionId ActionId;

        [Id(2)]
        [SerializeAs(DataType.VarInt)]
        public uint JumpBoost;
    }
}
