using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;
using Orleans;

namespace MineCase.Protocol.Play
{
    // In 1.12, it is PlayerListItem. Now it is PlayerInfo
    [GenerateSerializer]
    [Packet(0x34)]
    [GeneratePacketSerializer]
    public sealed partial class PlayerInfo<TAction> : IPacket
        where TAction : PlayerInfoAction, new()
    {
        [Id(0)]
        [SerializeAs(DataType.VarInt)]
        public uint Action;

        [Id(1)]
        [SerializeAs(DataType.VarInt)]
        public uint NumberOfPlayers;

        [Id(2)]
        [SerializeAs(DataType.Array, ArrayLengthMember = nameof(NumberOfPlayers))]
        public TAction[] Players;
    }

    [GenerateSerializer]
    public abstract partial class PlayerInfoAction : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.UUID)]
        public Guid UUID;

        public virtual void Deserialize(ref SpanReader br)
        {
            UUID = br.ReadAsUUID();
        }

        public virtual void Serialize(BinaryWriter bw)
        {
            bw.WriteAsUUID(UUID);
        }
    }

    [GenerateSerializer]
    public sealed partial class PlayerInfoAddPlayerAction : PlayerInfoAction
    {
        [Id(0)]
        [SerializeAs(DataType.String)]
        public string Name;

        [Id(1)]
        [SerializeAs(DataType.VarInt)]
        public uint NumberOfProperties;

        [Id(2)]
        [SerializeAs(DataType.VarInt)]
        public uint GameMode;

        [Id(3)]
        [SerializeAs(DataType.VarInt)]
        public uint Ping;

        [Id(4)]
        [SerializeAs(DataType.Boolean)]
        public bool HasDisplayName;

        [Id(5)]
        [SerializeAs(DataType.Chat)]
        public string DisplayName;

        public override void Deserialize(ref SpanReader br)
        {
            base.Deserialize(ref br);
        }

        public override void Serialize(BinaryWriter bw)
        {
            base.Serialize(bw);

            bw.WriteAsString(Name);
            bw.WriteAsVarInt(NumberOfProperties, out _);
            bw.WriteAsVarInt(GameMode, out _);
            bw.WriteAsVarInt(Ping, out _);
            bw.WriteAsBoolean(HasDisplayName);
            if (HasDisplayName)
                throw new NotImplementedException();
        }
    }

    [GenerateSerializer]
    public sealed class PlayerInfoRemovePlayerAction : PlayerInfoAction
    {
    }
}
