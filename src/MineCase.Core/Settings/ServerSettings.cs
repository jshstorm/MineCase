using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using MineCase.Server.Game;
using Newtonsoft.Json;
using Orleans;

namespace MineCase.Server.Settings
{
    [GenerateSerializer]
    [JsonObject(MemberSerialization.OptOut)]
    public class ServerSettings
    {
        // server
        [Id(0)]
        [JsonProperty(PropertyName = "motd")]
        [DefaultValue("A Minecraft Server")]
        public string Motd { get; set; } // this is the message that is displayed in the server list of the client

        [Id(1)]
        [JsonProperty(PropertyName = "server-ip")]
        [DefaultValue("127.0.0.1")]
        public string ServerIp { get; set; }

        [Id(2)]
        [JsonProperty(PropertyName = "server-port")]
        [DefaultValue(25565)]
        public uint ServerPort { get; set; }

        [Id(3)]
        [JsonProperty(PropertyName = "max-tick-time")]
        [DefaultValue(60000)]
        public uint MaxTickTime { get; set; }

        [Id(4)]
        [JsonProperty(PropertyName = "online-mode")]
        [DefaultValue(false)]
        public bool OnlineMode { get; set; }

        [Id(5)]
        [JsonProperty(PropertyName = "enable-rcon")]
        [DefaultValue(false)]
        public bool EnableRcon { get; set; } // Enables remote access to the server console.

        /// <summary>
        /// Gets or sets by default it allows packets that are n-1 bytes big to go normally, but a packet that n bytes or more will be compressed down. So, lower number means more compression but compressing small amounts of bytes might actually end up with a larger result than what went in.
        /// -1 - disable compression entirely
        /// 0 - compress everything.
        /// </summary>
        [Id(6)]
        [JsonProperty(PropertyName = "network-compression-threshold")]
        [DefaultValue(256)]
        public uint NetworkCompressionThreshold { get; set; }

        [Id(7)]
        [JsonProperty(PropertyName = "resource-pack")]
        [DefaultValue("")]
        public string ResourcePack { get; set; }

        [Id(8)]
        [JsonProperty(PropertyName = "resource-pack-hash")]
        [DefaultValue("")]
        public string ResourcePackHash { get; set; }

        [Id(9)]
        [JsonProperty(PropertyName = "snooper-enabled")]
        [DefaultValue(true)]
        public bool SnooperEnabled { get; set; }

        [Id(10)]
        [JsonProperty(PropertyName = "enable-query")]
        [DefaultValue(false)]
        public bool EnableQuery { get; set; }

        // spawn
        [Id(11)]
        [JsonProperty(PropertyName = "spawn-protection")]
        [DefaultValue(16)]
        public uint SpawnProtection { get; set; }

        [Id(12)]
        [JsonProperty(PropertyName = "spawn-monsters")]
        [DefaultValue(true)]
        public bool SpawnMonsters { get; set; }

        [Id(13)]
        [JsonProperty(PropertyName = "spawn-npcs")]
        [DefaultValue(true)]
        public bool SpawnNpcs { get; set; }

        [Id(14)]
        [JsonProperty(PropertyName = "spawn-animals")]
        [DefaultValue(true)]
        public bool SpawnAnimals { get; set; }

        // world
        [Id(15)]
        [JsonProperty(PropertyName = "max-world-size")]
        [DefaultValue(29999984)]
        public uint MaxWorldSize { get; set; }

        [Id(16)]
        [JsonProperty(PropertyName = "allow-nether")]
        [DefaultValue(true)]
        public bool AllowNether { get; set; }

        [Id(17)]
        [JsonProperty(PropertyName = "generate-structures")]
        [DefaultValue(true)]
        public bool GenerateStructures { get; set; }

        [Id(18)]
        [JsonProperty(PropertyName = "generate-settings")]
        [DefaultValue("")]
        public string GenerateSettings { get; set; }

        [Id(19)]
        [JsonProperty(PropertyName = "max-build-height")]
        [DefaultValue(256)]
        public uint MaxBuildHeight { get; set; }

        [Id(20)]
        [JsonProperty(PropertyName = "level-seed")]
        [DefaultValue("")]
        public string LevelSeed { get; set; } // seed for you world

        [Id(21)]
        [JsonProperty(PropertyName = "level-type")]
        [DefaultValue("DEFAULT")]
        public string LevelType { get; set; } // type of you world

        [Id(22)]
        [JsonProperty(PropertyName = "enable-command-block")]
        [DefaultValue(false)]
        public bool EnableCommandBlock { get; set; } // type of you world

        [Id(23)]
        [JsonProperty(PropertyName = "hardcore")]
        [DefaultValue(false)]
        public bool Hardcore { get; set; } // If set to true, players will be set to spectator mode if they die.

        // user
        [Id(24)]
        [JsonProperty(PropertyName = "max-players")]
        [DefaultValue(100)]
        public uint MaxPlayers { get; set; }

        [Id(25)]
        [JsonProperty(PropertyName = "view-distance")]
        [DefaultValue(10)]
        public uint ViewDistance { get; set; }

        [Id(26)]
        [JsonProperty(PropertyName = "allow-flight")]
        [DefaultValue(false)]
        public bool AllowFlight { get; set; }

        [Id(27)]
        [JsonProperty(PropertyName = "player-idle-timeout")]
        [DefaultValue(0)]
        public uint PlayerIdleTimeout { get; set; } // If non-zero, players are kicked from the server if they are idle for more than that many minutes.

        [Id(28)]
        [JsonProperty(PropertyName = "difficulty")]
        [DefaultValue(1)]
        public uint Difficulty { get; set; }

        [Id(29)]
        [JsonProperty(PropertyName = "force-gamemode")]
        [DefaultValue(false)]
        public bool ForceGamemode { get; set; }

        [Id(30)]
        [JsonProperty(PropertyName = "gamemode")]
        [DefaultValue(0)]
        public uint Gamemode { get; set; }

        [Id(31)]
        [JsonProperty(PropertyName = "pvp")]
        [DefaultValue(true)]
        public bool Pvp { get; set; }

        [Id(32)]
        [JsonProperty(PropertyName = "announce-player-achievements")]
        [DefaultValue(true)]
        public bool AnnouncePlayerAchievements { get; set; }

        // op
        [Id(33)]
        [JsonProperty(PropertyName = "white-list")]
        [DefaultValue(false)]
        public bool WhiteList { get; set; }

        [Id(34)]
        [JsonProperty(PropertyName = "op-permission-level")]
        [DefaultValue(4)]
        public uint OpPermissionLevel { get; set; }
    }
}
