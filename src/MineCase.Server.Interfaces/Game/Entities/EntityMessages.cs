using System;
using System.Collections.Generic;
using System.Text;
using MineCase.Engine;
using MineCase.Server.Game.Windows;
using MineCase.Server.User;
using MineCase.Server.World;
using MineCase.World;
using Orleans;
using Orleans.Concurrency;

namespace MineCase.Server.Game.Entities.Components
{
    [GenerateSerializer]
    [Immutable]
    public sealed class PlayerLoggedIn : IEntityMessage
    {
        public static readonly PlayerLoggedIn Default = new PlayerLoggedIn();
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class PlayerListAdd : IEntityMessage
    {
        [Id(0)]
        public IReadOnlyList<IPlayer> Players { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class PlayerListRemove : IEntityMessage
    {
        [Id(0)]
        public IReadOnlyList<IPlayer> Players { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class KickPlayer : IEntityMessage
    {
        [Id(0)]
        public Chat Reason { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class BindToUser : IEntityMessage
    {
        [Id(0)]
        public IUser User { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class SetHeldItemIndex : IEntityMessage
    {
        [Id(0)]
        public int Index { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class SetHeldItem : IEntityMessage
    {
        [Id(0)]
        public Slot Slot { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class AskHeldItem : IEntityMessage<(int Index, Slot Slot)>
    {
        public static readonly AskHeldItem Default = new AskHeldItem();
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class SetDraggedSlot : IEntityMessage
    {
        [Id(0)]
        public Slot Slot { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class AskDraggedSlot : IEntityMessage<Slot>
    {
        public static readonly AskDraggedSlot Default = new AskDraggedSlot();
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class SetDraggedPath : IEntityMessage
    {
        [Id(0)]
        public List<int> Path { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class AskDraggedPath : IEntityMessage<List<int>>
    {
        public static readonly AskDraggedPath Default = new AskDraggedPath();
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class AskPlayerDescription : IEntityMessage<PlayerDescription>
    {
        public static readonly AskPlayerDescription Default = new AskPlayerDescription();
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class BeginLogin : IEntityMessage
    {
        public static readonly BeginLogin Default = new BeginLogin();
    }

    [GenerateSerializer]
    [Immutable]
    public class SpawnEntity : IEntityMessage
    {
        [Id(0)]
        public IWorld World { get; set; }

        [Id(1)]
        public uint EntityId { get; set; }

        [Id(2)]
        public EntityWorldPos Position { get; set; }

        [Id(3)]
        public float Pitch { get; set; }

        [Id(4)]
        public float Yaw { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public class SpawnMob : SpawnEntity
    {
        [Id(0)]
        public MobType MobType { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public class SpawnPlayer : SpawnEntity
    {
    }

    [GenerateSerializer]
    [Immutable]
    public class EntityLook : IEntityMessage
    {
        [Id(0)]
        public float Yaw { get; set; }

        [Id(1)]
        public float Pitch { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public class EntityMove : IEntityMessage
    {
        // 实际的相对位移，并非mc协议中位移/(32*128)
        [Id(0)]
        public float DeltaX { get; set; }

        [Id(1)]
        public float DeltaY { get; set; }

        [Id(2)]
        public float DeltaZ { get; set; }

        [Id(3)]
        public bool OnGround { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class DestroyEntity : IEntityMessage
    {
        public static readonly DestroyEntity Default = new DestroyEntity();
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class SetSlot : IEntityMessage
    {
        [Id(0)]
        public int Index { get; set; }

        [Id(1)]
        public Slot Slot { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class AskSlot : IEntityMessage<Slot>
    {
        [Id(0)]
        public int Index { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class AskWindowId : IEntityMessage<byte>
    {
        [Id(0)]
        public IWindow Window { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class OpenWindow : IEntityMessage
    {
        [Id(0)]
        public IWindow Window { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class AskInventoryWindow : IEntityMessage<IInventoryWindow>
    {
        public static readonly AskInventoryWindow Default = new AskInventoryWindow();
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class TossPickup : IEntityMessage
    {
        [Id(0)]
        public Slot[] Slots { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class DiscoveredByPlayer : IEntityMessage
    {
        [Id(0)]
        public IPlayer Player { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class BroadcastDiscovered : IEntityMessage
    {
        public static readonly BroadcastDiscovered Default = new BroadcastDiscovered();
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class CollectBy : IEntityMessage
    {
        [Id(0)]
        public IEntity Entity { get; set; }
    }

    [GenerateSerializer]
    [Immutable]
    public sealed class AskCollectionResult : IEntityMessage<Slot>
    {
        [Id(0)]
        public IEntity Source { get; set; }

        [Id(1)]
        public Slot Slot { get; set; }
    }
}
