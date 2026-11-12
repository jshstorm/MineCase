using System;
using System.Linq;
using System.Reflection;
using MineCase.Server.Game.Entities;
using Xunit;

namespace MineCase.UnitTest
{
    public class ProtocolEntityTypeTest
    {
        private static readonly MethodInfo Of = Type.GetType("MineCase.Server.Network.Play.ProtocolEntityTypes, MineCase.Server.Grains", true)
            .GetMethod("Of", BindingFlags.Public | BindingFlags.Static);

        [Fact]
        public void EveryMobTypeHasADistinct115EntityId()
        {
            var ids = Enum.GetValues(typeof(MobType)).Cast<MobType>()
                .Select(t => (Type: t, Id: (uint)Of.Invoke(null, new object[] { t })))
                .ToList();

            var duplicates = ids.GroupBy(x => x.Id).Where(g => g.Count() > 1).Select(g => string.Join("/", g.Select(x => x.Type))).ToList();
            Assert.True(duplicates.Count == 0, string.Join(", ", duplicates));
        }

        [Theory]
        [InlineData(MobType.Zombie, 95u)]
        [InlineData(MobType.Pig, 55u)]
        [InlineData(MobType.Cow, 11u)]
        [InlineData(MobType.Creeper, 12u)]
        public void CommonMobsUse115Ids(MobType mobType, uint expected)
        {
            Assert.Equal(expected, (uint)Of.Invoke(null, new object[] { mobType }));
        }
    }
}
