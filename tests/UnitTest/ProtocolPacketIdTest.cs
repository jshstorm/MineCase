using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MineCase.Protocol;
using Xunit;

namespace MineCase.UnitTest
{
    /// <summary>
    /// Packet ids for Minecraft 1.15.2 (protocol 578), taken from minecraft-data's pc/1.15.2/protocol.json.
    /// </summary>
    public class ProtocolPacketIdTest
    {
        public static IEnumerable<object[]> Packets => new[]
        {
            new object[] { "handshaking", "toServer", "Handshake", 0x00u },
            new object[] { "status", "toClient", "Response", 0x00u },
            new object[] { "status", "toClient", "Pong", 0x01u },
            new object[] { "status", "toServer", "Request", 0x00u },
            new object[] { "status", "toServer", "Ping", 0x01u },
            new object[] { "login", "toClient", "LoginDisconnect", 0x00u },
            new object[] { "login", "toClient", "EncryptionRequest", 0x01u },
            new object[] { "login", "toClient", "LoginSuccess", 0x02u },
            new object[] { "login", "toClient", "SetCompression", 0x03u },
            new object[] { "login", "toServer", "LoginStart", 0x00u },
            new object[] { "login", "toServer", "EncryptionResponse", 0x01u },
            new object[] { "play", "toClient", "SpawnObject", 0x00u },
            new object[] { "play", "toClient", "SpawnMob", 0x03u },
            new object[] { "play", "toClient", "SpawnPlayer", 0x05u },
            new object[] { "play", "toClient", "ClientboundAnimation", 0x06u },
            new object[] { "play", "toClient", "BlockBreakAnimation", 0x09u },
            new object[] { "play", "toClient", "BlockChange", 0x0cu },
            new object[] { "play", "toClient", "ClientboundChatMessage", 0x0fu },
            new object[] { "play", "toClient", "DeclareCommands", 0x12u },
            new object[] { "play", "toClient", "ClientboundConfirmTransaction", 0x13u },
            new object[] { "play", "toClient", "ClientboundCloseWindow", 0x14u },
            new object[] { "play", "toClient", "WindowItems", 0x15u },
            new object[] { "play", "toClient", "WindowProperty", 0x16u },
            new object[] { "play", "toClient", "SetSlot", 0x17u },
            new object[] { "play", "toClient", "UnloadChunk", 0x1eu },
            new object[] { "play", "toClient", "ClientboundKeepAlive", 0x21u },
            new object[] { "play", "toClient", "ChunkData", 0x22u },
            new object[] { "play", "toClient", "JoinGame", 0x26u },
            new object[] { "play", "toClient", "EntityRelativeMove", 0x29u },
            new object[] { "play", "toClient", "EntityLookAndRelativeMove", 0x2au },
            new object[] { "play", "toClient", "EntityLook", 0x2bu },
            new object[] { "play", "toClient", "Entity", 0x2cu },
            new object[] { "play", "toClient", "OpenWindow", 0x2fu },
            new object[] { "play", "toClient", "PlayerInfo", 0x34u },
            new object[] { "play", "toClient", "ClientboundPositionAndLook", 0x36u },
            new object[] { "play", "toClient", "DestroyEntities", 0x38u },
            new object[] { "play", "toClient", "Respawn", 0x3bu },
            new object[] { "play", "toClient", "EntityHeadLook", 0x3cu },
            new object[] { "play", "toClient", "UpdateViewPosition", 0x41u },
            new object[] { "play", "toClient", "EntityMetadata", 0x44u },
            new object[] { "play", "toClient", "SetExperience", 0x48u },
            new object[] { "play", "toClient", "UpdateHealth", 0x49u },
            new object[] { "play", "toClient", "TimeUpdate", 0x4fu },
            new object[] { "play", "toClient", "CollectItem", 0x56u },
            new object[] { "play", "toClient", "EntityTeleport", 0x57u },
            new object[] { "play", "toServer", "TeleportConfirm", 0x00u },
            new object[] { "play", "toServer", "ServerboundChatMessage", 0x03u },
            new object[] { "play", "toServer", "ClientSettings", 0x05u },
            new object[] { "play", "toServer", "ServerboundConfirmTransaction", 0x07u },
            new object[] { "play", "toServer", "ClickWindow", 0x09u },
            new object[] { "play", "toServer", "ServerboundCloseWindow", 0x0au },
            new object[] { "play", "toServer", "ServerboundPluginMessage", 0x0bu },
            new object[] { "play", "toServer", "UseEntity", 0x0eu },
            new object[] { "play", "toServer", "ServerboundKeepAlive", 0x0fu },
            new object[] { "play", "toServer", "PlayerOnGround", 0x14u },
            new object[] { "play", "toServer", "PlayerPosition", 0x11u },
            new object[] { "play", "toServer", "ServerboundPositionAndLook", 0x12u },
            new object[] { "play", "toServer", "PlayerLook", 0x13u },
            new object[] { "play", "toServer", "PlayerDigging", 0x1au },
            new object[] { "play", "toServer", "EntityAction", 0x1bu },
            new object[] { "play", "toServer", "ServerboundHeldItemChange", 0x23u },
            new object[] { "play", "toServer", "ServerboundAnimation", 0x2au },
            new object[] { "play", "toServer", "PlayerBlockPlacement", 0x2cu },
            new object[] { "play", "toServer", "UseItem", 0x2du },
        };

        [Theory]
        [MemberData(nameof(Packets))]
        public void PacketIdMatchesProtocol(string state, string direction, string className, uint expectedId)
        {
            var type = typeof(IPacket).Assembly.GetTypes().Single(t => t.Name.Split('`')[0] == className && t.GetCustomAttribute<PacketAttribute>() != null);
            Assert.True(expectedId == type.GetCustomAttribute<PacketAttribute>().PacketId, $"{state} {direction} {className}: expected 0x{expectedId:X2}, got 0x{type.GetCustomAttribute<PacketAttribute>().PacketId:X2}");
        }

        [Fact]
        public void NoTwoPacketsShareAnIdInTheSameStateAndDirection()
        {
            var duplicates = Packets
                .Select(p => (State: (string)p[0], Direction: (string)p[1], Name: (string)p[2]))
                .Select(p => (p.State, p.Direction, p.Name, Id: typeof(IPacket).Assembly.GetTypes().Single(t => t.Name.Split('`')[0] == p.Name && t.GetCustomAttribute<PacketAttribute>() != null).GetCustomAttribute<PacketAttribute>().PacketId))
                .GroupBy(p => (p.State, p.Direction, p.Id))
                .Where(g => g.Count() > 1)
                .Select(g => $"{g.Key.State} {g.Key.Direction} 0x{g.Key.Id:X2}: {string.Join(", ", g.Select(p => p.Name))}")
                .ToList();

            Assert.True(duplicates.Count == 0, string.Join(Environment.NewLine, duplicates));
        }
    }
}
