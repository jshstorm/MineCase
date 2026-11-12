using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;
using MineCase.Block;
using MineCase.Client.Net;
using MineCase.Client.Net.Rendering;
using Xunit;

namespace MineCase.UnitTest
{
    public sealed class ResourcePackTest : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "minecase-pack-" + Guid.NewGuid().ToString("N"));

        public ResourcePackTest()
        {
            // A tiny hand-written pack in the game's layout.
            WriteTexture("test_stone", Fill(16, 16, (x, y) => (120, 120, 120, 255)));
            WriteTexture("test_glass", Fill(16, 16, (x, y) => x == 0 || y == 0 ? ((byte)200, (byte)220, (byte)255, (byte)255) : ((byte)0, (byte)0, (byte)0, (byte)0)));
            WriteTexture("test_wood", Fill(16, 16, (x, y) => (150, 110, 60, 255)));
            WriteText("models/block/test_cube.json", "{\"elements\":[{\"from\":[0,0,0],\"to\":[16,16,16],\"faces\":{" +
                string.Join(",", new[] { "down", "up", "north", "south", "west", "east" }.Select(d => $"\"{d}\":{{\"texture\":\"#all\",\"cullface\":\"{d}\"}}")) + "}}]}");
            WriteText("models/block/stone.json", "{\"parent\":\"block/test_cube\",\"textures\":{\"all\":\"block/test_stone\"}}");
            WriteText("models/block/glass.json", "{\"parent\":\"minecraft:block/test_cube\",\"textures\":{\"all\":\"block/test_glass\"}}");
            WriteText("models/block/test_front.json", "{\"textures\":{\"all\":\"block/test_wood\"},\"elements\":[{\"from\":[0,0,0],\"to\":[16,16,16],\"faces\":{\"north\":{\"texture\":\"#all\",\"cullface\":\"north\"}}}]}");
            WriteText("models/block/test_post.json", "{\"textures\":{\"all\":\"block/test_wood\"},\"elements\":[{\"from\":[6,0,6],\"to\":[10,16,10],\"faces\":{\"north\":{\"texture\":\"#all\"},\"south\":{\"texture\":\"#all\"}}}]}");
            WriteText("models/block/test_side.json", "{\"textures\":{\"all\":\"block/test_wood\"},\"elements\":[{\"from\":[7,12,0],\"to\":[9,15,6],\"faces\":{\"west\":{\"texture\":\"#all\"},\"east\":{\"texture\":\"#all\"}}}]}");
            WriteText("blockstates/stone.json", "{\"variants\":{\"\":{\"model\":\"block/stone\"}}}");
            WriteText("blockstates/glass.json", "{\"variants\":{\"\":{\"model\":\"block/glass\"}}}");
            WriteText("blockstates/furnace.json", "{\"variants\":{\"facing=north\":{\"model\":\"block/test_front\"},\"facing=east\":{\"model\":\"block/test_front\",\"y\":90}}}");
            WriteText("blockstates/oak_fence.json", "{\"multipart\":[{\"apply\":{\"model\":\"block/test_post\"}},{\"when\":{\"north\":\"true\"},\"apply\":{\"model\":\"block/test_side\"}}]}");
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Fact]
        public void BlockStateIdsDecodeIntoProperties()
        {
            Assert.Equal(11337, BlockStateTable.StateCount);
            Assert.Equal("grass_block", BlockStateTable.Get(9).Name);
            Assert.Equal("false", BlockStateTable.Get(9).GetProperty("snowy"));

            var stairs = BlockStateTable.Get(1963);
            Assert.Equal("oak_stairs", stairs.Name);
            Assert.Equal("north", stairs.GetProperty("facing"));
            Assert.Equal("bottom", stairs.GetProperty("half"));
            Assert.Equal("straight", stairs.GetProperty("shape"));
            Assert.Equal("false", stairs.GetProperty("waterlogged"));

            // The server's BlockId values are the first state id of each block.
            Assert.All(BlockStateTable.Blocks, b => Assert.True(Enum.IsDefined(typeof(BlockId), (uint)b.FirstStateId), b.Name));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void PngDecoderUndoesEveryFilter(byte filter)
        {
            var image = Fill(5, 3, (x, y) => ((byte)(x * 40), (byte)(y * 70), (byte)((x + y) * 20), (byte)(255 - (x * 10))));
            var decoded = PngDecoder.Decode(EncodePng(image, filter));

            Assert.Equal(5, decoded.Width);
            Assert.Equal(3, decoded.Height);
            Assert.Equal(image.Pixels, decoded.Pixels);
        }

        [Fact]
        public void PngDecoderReadsPalettesWithTransparency()
        {
            // 4-bit palette: index 0 is transparent red, index 1 opaque blue.
            var rows = new byte[] { 0, 0x01, 0, 0x10 };
            var png = Png(2, 2, 4, 3, Compress(rows), new byte[] { 255, 0, 0, 0, 0, 255 }, new byte[] { 0 });
            var decoded = PngDecoder.Decode(png);

            Assert.Equal(new byte[] { 255, 0, 0, 0, 0, 0, 255, 255, 0, 0, 255, 255, 255, 0, 0, 0 }, decoded.Pixels);
        }

        [Fact]
        public void LoneTexturedCubeHasSixFacesInItsSprite()
        {
            var models = Load();
            var mesh = Build(models, (1, 1, 1, State("stone")));

            Assert.Equal(6 * 4, mesh.Opaque.Positions.Count);
            Assert.True(models.Atlas.TryGetSprite("minecraft:block/test_stone", out var sprite));
            Assert.All(mesh.Opaque.Uvs, uv => Assert.InRange(uv.X, sprite.Rect.X, sprite.Rect.Z));
            Assert.All(mesh.Opaque.Uvs, uv => Assert.InRange(uv.Y, sprite.Rect.Y, sprite.Rect.W));

            // Seen from the north the texture's top-left corner is at the block's top east edge.
            var topEast = mesh.Opaque.Positions.Select((p, i) => (p, i)).First(v => v.p == new Vector3(2, 2, 1) && mesh.Opaque.Normals[v.i] == -Vector3.UnitZ).i;
            Assert.Equal(new Vector2(sprite.Rect.X, sprite.Rect.Y), mesh.Opaque.Uvs[topEast]);
        }

        [Fact]
        public void CutoutBlocksDoNotHideNeighboursButHideEachOther()
        {
            var models = Load();
            var glass = State("glass");

            var mixed = Build(models, (1, 1, 1, State("stone")), (2, 1, 1, glass));
            Assert.Equal(6 * 4, mixed.Opaque.Positions.Count);
            Assert.Equal(5 * 4, mixed.Cutout.Positions.Count);

            var panes = Build(models, (1, 1, 1, glass), (2, 1, 1, glass));
            Assert.Equal(10 * 4, panes.Cutout.Positions.Count);
        }

        [Fact]
        public void VariantRotationTurnsFacesAndCullFaces()
        {
            var models = Load();
            var east = models.Get(State("furnace", ("facing", "east"), ("lit", "false"))).Variants.Single().Quads.Single();

            Assert.Equal(Direction.East, east.Facing);
            Assert.Equal(Direction.East, east.CullFace);
        }

        [Fact]
        public void MultipartAddsThePiecesWhoseConditionsHold()
        {
            var models = Load();
            var post = models.Get(State("oak_fence", ("north", "false"), ("east", "false"), ("south", "false"), ("west", "false"), ("waterlogged", "false")));
            var withSide = models.Get(State("oak_fence", ("north", "true"), ("east", "false"), ("south", "false"), ("west", "false"), ("waterlogged", "false")));

            Assert.Equal(2, post.Variants.Single().Quads.Count);
            Assert.Equal(4, withSide.Variants.Single().Quads.Count);
        }

        [Fact]
        public void BlocksThePackDoesNotDescribeFallBackToFlatColours()
        {
            var models = Load();
            var dirt = models.Get(State("dirt")).Variants.Single();

            Assert.Equal(6, dirt.Quads.Count);
            Assert.All(dirt.Quads, q => Assert.Equal(TextureAtlas.WhiteSprite, q.Texture));
            Assert.Contains(models.Problems, p => p.Contains("block state files missing", StringComparison.Ordinal));
        }

        private static ushort State(string name, params (string Name, string Value)[] properties)
        {
            var block = BlockStateTable.Blocks.Single(b => b.Name == name);
            return Enumerable.Range(block.FirstStateId, block.StateCount)
                .Select(id => BlockStateTable.Get(id))
                .First(s => properties.All(p => s.GetProperty(p.Name) == p.Value))
                .StateId;
        }

        private static SectionMesh Build(IBlockModels models, params (int X, int Y, int Z, ushort State)[] blocks)
        {
            var sections = new ushort[ChunkColumn.SectionCount][];
            sections[0] = new ushort[ChunkColumn.SectionVolume];
            foreach (var block in blocks)
                sections[0][ChunkColumn.Index(block.X, block.Y, block.Z)] = block.State;
            var column = new ChunkColumn(0, 0, sections);
            var world = new ClientWorld();
            world.SetColumn(column);
            return ChunkMesher.BuildSection(world, column, 0, models);
        }

        private static RgbaImage Fill(int width, int height, Func<int, int, (byte R, byte G, byte B, byte A)> color)
        {
            var image = new RgbaImage(width, height, new byte[width * height * 4]);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var (r, g, b, a) = color(x, y);
                    var o = image.OffsetOf(x, y);
                    image.Pixels[o] = r;
                    image.Pixels[o + 1] = g;
                    image.Pixels[o + 2] = b;
                    image.Pixels[o + 3] = a;
                }
            }

            return image;
        }

        private static byte[] EncodePng(RgbaImage image, byte filter)
        {
            var stride = image.Width * 4;
            var raw = new List<byte>();
            for (var y = 0; y < image.Height; y++)
            {
                raw.Add(filter);
                for (var i = 0; i < stride; i++)
                {
                    int Get(int yy, int ii) => yy < 0 || ii < 0 ? 0 : image.Pixels[(yy * stride) + ii];
                    int a = Get(y, i - 4), b = Get(y - 1, i), c = Get(y - 1, i - 4);
                    var predictor = filter switch
                    {
                        1 => a,
                        2 => b,
                        3 => (a + b) / 2,
                        4 => Paeth(a, b, c),
                        _ => 0,
                    };
                    raw.Add((byte)(Get(y, i) - predictor));
                }
            }

            return Png(image.Width, image.Height, 8, 6, Compress(raw.ToArray()), null, null);
        }

        private static int Paeth(int a, int b, int c)
        {
            var p = a + b - c;
            int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
            return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
        }

        private static byte[] Compress(byte[] data)
        {
            using var memory = new MemoryStream();
            using (var zlib = new ZLibStream(memory, CompressionLevel.Optimal, true))
                zlib.Write(data);
            return memory.ToArray();
        }

        // The decoder does not check CRCs, so they are left as zero.
        private static byte[] Png(int width, int height, byte bitDepth, byte colorType, byte[] idat, byte[] palette, byte[] transparency)
        {
            using var memory = new MemoryStream();
            memory.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            var header = new byte[13];
            BinaryPrimitives.WriteUInt32BigEndian(header, (uint)width);
            BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), (uint)height);
            header[8] = bitDepth;
            header[9] = colorType;
            Chunk(memory, "IHDR", header);
            if (palette != null)
                Chunk(memory, "PLTE", palette);
            if (transparency != null)
                Chunk(memory, "tRNS", transparency);
            Chunk(memory, "IDAT", idat);
            Chunk(memory, "IEND", Array.Empty<byte>());
            return memory.ToArray();
        }

        private static void Chunk(Stream stream, string type, byte[] data)
        {
            var length = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
            stream.Write(length);
            stream.Write(Encoding.ASCII.GetBytes(type));
            stream.Write(data);
            stream.Write(new byte[4]);
        }

        private ResourcePackBlockModels Load()
        {
            using var pack = ResourcePackStack.Open(new[] { _root });
            return ResourcePackBlockModels.Load(pack);
        }

        private void WriteTexture(string name, RgbaImage image) =>
            WriteBytes($"textures/block/{name}.png", EncodePng(image, 4));

        private void WriteText(string path, string text) => WriteBytes(path, Encoding.UTF8.GetBytes(text));

        private void WriteBytes(string path, byte[] data)
        {
            var file = Path.Combine(_root, "assets", "minecraft", path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllBytes(file, data);
        }
    }
}
