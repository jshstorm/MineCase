using System.Linq;
using System.Numerics;
using MineCase.Client.Net.Rendering;
using Xunit;

namespace MineCase.UnitTest
{
    public class MobModelTest
    {
        [Theory]
        [InlineData(62u, "sheep")]
        [InlineData(11u, "cow")]
        [InlineData(55u, "pig")]
        [InlineData(9u, "chicken")]
        [InlineData(95u, "zombie")]
        [InlineData(66u, "skeleton")]
        [InlineData(73u, "spider")]
        [InlineData(101u, "player")]
        public void DesignedMobsHaveAnimatedHeadsAndLegsInsideTheirTexture(uint entityType, string name)
        {
            var type = EntityTypeTable.Get(entityType);
            Assert.Equal(name, type.Name);
            Assert.True(MobModels.HasDesign(name));

            var mob = MobModels.Build(type);
            Assert.Single(mob.Parts, p => p.Motion == PartMotion.Head);
            Assert.Contains(mob.Parts, p => p.Motion is PartMotion.Leg or PartMotion.SpiderLeg);
            AssertWellFormed(mob);
        }

        [Fact]
        public void OtherEntitiesAreBoxesOfTheirSize()
        {
            var creeper = EntityTypeTable.Get(12);
            var mob = MobModels.Build(creeper);

            Assert.Equal("creeper", creeper.Name);
            var part = Assert.Single(mob.Parts);
            var max = part.Mesh.Positions.Aggregate(Vector3.Max);
            var min = part.Mesh.Positions.Aggregate(Vector3.Min);
            Assert.Equal(creeper.Height, max.Y - min.Y, 3);
            Assert.Equal(creeper.Width, max.X - min.X, 3);
            AssertWellFormed(mob);
        }

        private static void AssertWellFormed(MobMesh mob)
        {
            Assert.True(mob.Texture.Width > 0 && mob.Texture.Height > 0);
            foreach (var part in mob.Parts)
            {
                Assert.All(part.Mesh.Uvs, uv => Assert.True(uv.X >= 0 && uv.X <= 1 && uv.Y >= 0 && uv.Y <= 1));

                // Every face points at painted texels.
                for (var q = 0; q < part.Mesh.Uvs.Count; q += 4)
                {
                    var center = (part.Mesh.Uvs[q] + part.Mesh.Uvs[q + 2]) / 2;
                    var x = (int)(center.X * mob.Texture.Width);
                    var y = (int)(center.Y * mob.Texture.Height);
                    Assert.Equal(255, mob.Texture.Pixels[mob.Texture.OffsetOf(x, y) + 3]);
                }

                for (var i = 0; i < part.Mesh.Indices.Count; i += 3)
                {
                    var a = part.Mesh.Positions[part.Mesh.Indices[i]];
                    var b = part.Mesh.Positions[part.Mesh.Indices[i + 1]];
                    var c = part.Mesh.Positions[part.Mesh.Indices[i + 2]];
                    var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
                    Assert.True(Vector3.Dot(normal, part.Mesh.Normals[part.Mesh.Indices[i]]) > 0.99f, "faces wind counter-clockwise outward");
                }
            }
        }
    }
}
