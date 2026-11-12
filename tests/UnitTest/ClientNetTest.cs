using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using MineCase.Block;
using MineCase.Client.Net;
using MineCase.Protocol.Play;
using MineCase.Serialization;
using MineCase.World;
using Xunit;

namespace MineCase.UnitTest
{
    public class ClientNetTest
    {
        [Fact]
        public void ChunkColumnDecodesWhatTheServerSends()
        {
            var storage = new ChunkColumnCompactStorage();
            storage.Sections[0] = new ChunkSectionCompactStorage(true);
            storage.Sections[3] = new ChunkSectionCompactStorage(true);
            storage[1, 2, 3] = BlockStates.Stone();
            storage[15, 15, 15] = BlockStates.GrassBlock();
            storage[4, 50, 9] = BlockStates.Water();

            // Same packet the server builds in ClientPlayPacketGenerator.ChunkData.
            var packet = new ChunkData
            {
                ChunkX = -3,
                ChunkZ = 7,
                FullChunk = true,
                PrimaryBitMask = storage.SectionBitMask,
                Heightmaps = storage.Heightmaps,
                Biomes = storage.Biomes,
                Data = storage.Sections.Where(s => s != null).Select(s => new ChunkSection
                {
                    BlockCount = s.NonAirBlockCount,
                    BitsPerBlock = s.BitsPerBlock,
                    PaletteLength = 0,
                    DataArray = s.Data.Storage,
                }).ToArray(),
                NumberOfBlockEntities = 0,
                BlockEntities = new Nbt.Tags.NbtCompound[0],
            };

            byte[] bytes;
            using (var memory = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memory, Encoding.UTF8, true))
                    packet.Serialize(writer);
                bytes = memory.ToArray();
            }

            var reader = new SpanReader(bytes);
            var column = ChunkColumn.FromPacket(ChunkData.Deserialize(ref reader, true));

            Assert.Equal(-3, column.ChunkX);
            Assert.Equal(7, column.ChunkZ);
            Assert.Equal(BlockId.Stone, column.GetBlock(1, 2, 3));
            Assert.Equal(BlockId.GrassBlock, column.GetBlock(15, 15, 15));
            Assert.Equal(BlockId.Water, column.GetBlock(4, 50, 9));
            Assert.Equal(BlockId.Air, column.GetBlock(4, 51, 9));
            Assert.NotNull(column.GetSection(0));
            Assert.NotNull(column.GetSection(3));
            Assert.Null(column.GetSection(1));
        }

        [Fact]
        public void LoneCubeHasSixOutwardFaces()
        {
            var mesh = BuildSingleSection((1, 1, 1, BlockId.Stone));

            Assert.Equal(6 * 4, mesh.Opaque.Positions.Count);
            Assert.Equal(6 * 6, mesh.Opaque.Indices.Count);
            Assert.Equal(6 * 6, mesh.CollisionTriangles.Count);
            AssertCounterClockwiseOutward(mesh.Opaque);
        }

        [Fact]
        public void FacesBetweenNeighboursAreSkipped()
        {
            var mesh = BuildSingleSection((1, 1, 1, BlockId.Stone), (2, 1, 1, BlockId.Dirt));

            Assert.Equal(10 * 4, mesh.Opaque.Positions.Count);
        }

        [Fact]
        public void WaterIsTranslucentAndNotSolid()
        {
            var mesh = BuildSingleSection((1, 1, 1, BlockId.Water), (2, 1, 1, BlockId.Water));

            // Ten outer faces plus the underside of the two surface quads.
            Assert.True(mesh.Opaque.IsEmpty);
            Assert.Equal(12 * 4, mesh.Translucent.Positions.Count);
            Assert.Empty(mesh.CollisionTriangles);
            Assert.All(mesh.Translucent.Positions, p => Assert.True(p.Y <= 1 + (8f / 9) + 1e-5f));
        }

        [Fact]
        public void EdgeFacesLookIntoTheNeighbourColumn()
        {
            var world = new ClientWorld();
            var left = CreateColumn(0, 0, (15, 1, 1, BlockId.Stone));
            var right = CreateColumn(1, 0, (0, 1, 1, BlockId.Stone));
            world.SetColumn(left);

            Assert.Equal(6 * 4, ChunkMesher.BuildSection(world, left, 0).Opaque.Positions.Count);

            world.SetColumn(right);
            Assert.Equal(5 * 4, ChunkMesher.BuildSection(world, left, 0).Opaque.Positions.Count);
        }

        [Fact]
        public void PlantsAreCrossedAndNotSolid()
        {
            var mesh = BuildSingleSection((3, 3, 3, BlockId.Poppy));

            Assert.Equal(4 * 4, mesh.Opaque.Positions.Count);
            Assert.Empty(mesh.CollisionTriangles);
        }

        private static SectionMesh BuildSingleSection(params (int X, int Y, int Z, BlockId Id)[] blocks)
        {
            var world = new ClientWorld();
            var column = CreateColumn(0, 0, blocks);
            world.SetColumn(column);
            return ChunkMesher.BuildSection(world, column, 0);
        }

        private static ChunkColumn CreateColumn(int chunkX, int chunkZ, params (int X, int Y, int Z, BlockId Id)[] blocks)
        {
            var sections = new ushort[ChunkColumn.SectionCount][];
            sections[0] = new ushort[ChunkColumn.SectionVolume];
            foreach (var block in blocks)
                sections[0][ChunkColumn.Index(block.X, block.Y, block.Z)] = (ushort)block.Id;
            return new ChunkColumn(chunkX, chunkZ, sections);
        }

        private static void AssertCounterClockwiseOutward(MeshData mesh)
        {
            for (var i = 0; i < mesh.Indices.Count; i += 3)
            {
                var a = mesh.Positions[mesh.Indices[i]];
                var b = mesh.Positions[mesh.Indices[i + 1]];
                var c = mesh.Positions[mesh.Indices[i + 2]];
                var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
                Assert.Equal(mesh.Normals[mesh.Indices[i]], normal);
            }
        }
    }
}
