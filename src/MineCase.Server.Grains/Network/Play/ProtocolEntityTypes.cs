using System;
using System.Collections.Generic;
using MineCase.Server.Game.Entities;

namespace MineCase.Server.Network.Play
{
    /// <summary>
    /// Entity type ids of the 1.15.2 registry (minecraft-data pc/1.15.2/entities.json); <see cref="MobType"/> keeps the 1.12 numbering.
    /// </summary>
    internal static class ProtocolEntityTypes
    {
        public const byte Item = 35;

        private static readonly Dictionary<MobType, uint> MobTypes = new Dictionary<MobType, uint>
        {
            [MobType.Item] = 35,
            [MobType.XPOrb] = 24,
            [MobType.AreaEffectCloud] = 0,
            [MobType.ElderGuardian] = 17,
            [MobType.WitherSkeleton] = 92,
            [MobType.Stray] = 75,
            [MobType.Husk] = 33,
            [MobType.ZombieVillager] = 97,
            [MobType.SkeletonHorse] = 67,
            [MobType.ZombieHorse] = 96,
            [MobType.ArmorStand] = 1,
            [MobType.Donkey] = 13,
            [MobType.Mule] = 49,
            [MobType.EvocationIllager] = 23,
            [MobType.Vex] = 84,
            [MobType.VindicationIllager] = 87,
            [MobType.IllusionIllager] = 34,
            [MobType.Creeper] = 12,
            [MobType.Skeleton] = 66,
            [MobType.Spider] = 73,
            [MobType.Giant] = 30,
            [MobType.Zombie] = 95,
            [MobType.Slime] = 68,
            [MobType.Ghast] = 29,
            [MobType.PigZombie] = 57,
            [MobType.Enderman] = 20,
            [MobType.CaveSpider] = 8,
            [MobType.Silverfish] = 65,
            [MobType.Blaze] = 5,
            [MobType.LavaSlime] = 41,
            [MobType.EnderDragon] = 19,
            [MobType.WitherBoss] = 91,
            [MobType.Bat] = 3,
            [MobType.Witch] = 90,
            [MobType.Endermite] = 21,
            [MobType.Guardian] = 31,
            [MobType.Shulker] = 63,
            [MobType.Pig] = 55,
            [MobType.Sheep] = 62,
            [MobType.Cow] = 11,
            [MobType.Chicken] = 9,
            [MobType.Squid] = 74,
            [MobType.Wolf] = 94,
            [MobType.MushroomCow] = 50,
            [MobType.SnowMan] = 70,
            [MobType.Ozelot] = 51,
            [MobType.VillagerGolem] = 86,
            [MobType.Horse] = 32,
            [MobType.Rabbit] = 60,
            [MobType.PolarBear] = 58,
            [MobType.Llama] = 39,
            [MobType.Parrot] = 54,
            [MobType.Villager] = 85,
        };

        public static uint Of(MobType mobType) =>
            MobTypes.TryGetValue(mobType, out var id) ? id : throw new ArgumentOutOfRangeException(nameof(mobType), mobType, "No 1.15.2 entity type for this mob.");
    }
}
