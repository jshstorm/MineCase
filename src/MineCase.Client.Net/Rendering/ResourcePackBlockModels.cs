using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using MineCase.Block;

namespace MineCase.Client.Net.Rendering
{
    /// <summary>
    /// Block models, textures and tints read from resource packs in the game's format (blockstates, models, textures).
    /// Blocks the packs do not describe fall back to <see cref="BlockAppearance"/> colours.
    /// </summary>
    public sealed class ResourcePackBlockModels : IBlockModels
    {
        private const string WaterTexture = "minecraft:block/water_still";
        private const string LavaTexture = "minecraft:block/lava_still";

        // Plains biome, which the client uses everywhere because it does not read biomes.
        private const float Temperature = 0.8f;
        private const float Downfall = 0.4f;

        private static readonly Vector4 WaterColor = Rgb(0x3F76E4);

        private readonly BlockRenderInfo[] _infos;

        private ResourcePackBlockModels(BlockRenderInfo[] infos, TextureAtlas atlas, FluidAppearance water, FluidAppearance lava, IReadOnlyList<string> problems)
        {
            _infos = infos;
            Atlas = atlas;
            Water = water;
            Lava = lava;
            Problems = problems;
        }

        public TextureAtlas Atlas { get; }

        public FluidAppearance Water { get; }

        public FluidAppearance Lava { get; }

        /// <summary>
        /// Gets what could not be loaded (missing block state files, models and textures).
        /// </summary>
        public IReadOnlyList<string> Problems { get; }

        public BlockRenderInfo Get(int stateId) => (uint)stateId < (uint)_infos.Length ? _infos[stateId] : BlockRenderInfo.Empty;

        public static ResourcePackBlockModels Load(ResourcePackStack pack)
        {
            var library = new ModelLibrary(pack);
            var tints = new TintTable(pack);
            var problems = new List<string>();
            var missingBlockStates = new List<string>();
            var infos = new BlockRenderInfo[BlockStateTable.StateCount];

            foreach (var block in BlockStateTable.Blocks)
            {
                if (block.Name is "air" or "cave_air" or "void_air")
                {
                    for (var i = 0; i < block.StateCount; i++)
                        infos[block.FirstStateId + i] = BlockRenderInfo.Empty;
                    continue;
                }

                BlockStateFile file = null;
                if (pack.TryReadText($"assets/minecraft/blockstates/{block.Name}.json", out var json))
                    file = BlockStateFile.Parse(json);
                else
                    missingBlockStates.Add(block.Name);

                var flat = file == null ? BlockRenderInfos.FlatModel((BlockId)block.FirstStateId) : null;
                for (var i = 0; i < block.StateCount; i++)
                {
                    var state = BlockStateTable.Get(block.FirstStateId + i);
                    var (models, weights) = file == null ? (new[] { flat }, new[] { 1 }) : file.Select(state, library);
                    infos[state.StateId] = BlockRenderInfos.Create(state, models, weights, tints.For(state));
                }
            }

            var images = new Dictionary<string, RgbaImage>(StringComparer.Ordinal);
            var missingTextures = new List<string>();
            foreach (var texture in library.Textures.Append(WaterTexture).Append(LavaTexture).Distinct())
            {
                var image = ReadPng(pack, ModelLibrary.AssetPath("textures", texture, ".png"));
                if (image != null)
                    images[texture] = image;
                else
                    missingTextures.Add(texture);
            }

            var atlas = TextureAtlas.Build(images);
            BlockRenderInfos.Finish(infos, atlas);

            var waterSprite = atlas.GetSprite(WaterTexture);
            var lavaSprite = atlas.GetSprite(LavaTexture);
            var water = new FluidAppearance(waterSprite.Rect, WaterColor, waterSprite.Layer == RenderLayer.Opaque ? RenderLayer.Translucent : waterSprite.Layer);
            var lava = new FluidAppearance(lavaSprite.Rect, Vector4.One, RenderLayer.Opaque);

            if (missingBlockStates.Count > 0)
                problems.Add($"{missingBlockStates.Count} block state files missing, drawn as flat colours: {string.Join(", ", missingBlockStates.Take(10))}");
            if (library.MissingModels.Count > 0)
                problems.Add($"{library.MissingModels.Count} models missing: {string.Join(", ", library.MissingModels.Take(10))}");
            if (missingTextures.Count > 0)
                problems.Add($"{missingTextures.Count} textures missing: {string.Join(", ", missingTextures.Take(10))}");
            return new ResourcePackBlockModels(infos, atlas, water, lava, problems);
        }

        internal static RgbaImage ReadPng(ResourcePackStack pack, string path)
        {
            if (!pack.TryRead(path, out var data))
                return null;
            try
            {
                return PngDecoder.Decode(data);
            }
            catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or IndexOutOfRangeException or ArgumentException)
            {
                return null;
            }
        }

        private static Vector4 Rgb(int rgb) => new Vector4(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1);

        /// <summary>
        /// A parsed blockstates file: either variants keyed by property values or multipart pieces with conditions.
        /// </summary>
        private sealed class BlockStateFile
        {
            private readonly List<(Dictionary<string, string> When, List<ModelReference> Models)> _variants = new List<(Dictionary<string, string>, List<ModelReference>)>();
            private readonly List<(Func<BlockStateInfo, bool> When, List<ModelReference> Models)> _parts = new List<(Func<BlockStateInfo, bool>, List<ModelReference>)>();

            public static BlockStateFile Parse(string json)
            {
                var file = new BlockStateFile();
                using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                var root = document.RootElement;
                if (root.TryGetProperty("variants", out var variants) && variants.ValueKind == JsonValueKind.Object)
                {
                    foreach (var variant in variants.EnumerateObject())
                    {
                        var when = variant.Name.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(pair => pair.Split('='))
                            .Where(pair => pair.Length == 2)
                            .ToDictionary(pair => pair[0], pair => pair[1], StringComparer.Ordinal);
                        file._variants.Add((when, ReadModels(variant.Value)));
                    }
                }
                else if (root.TryGetProperty("multipart", out var multipart) && multipart.ValueKind == JsonValueKind.Array)
                {
                    foreach (var part in multipart.EnumerateArray())
                    {
                        var when = part.TryGetProperty("when", out var condition) ? Condition(condition) : _ => true;
                        file._parts.Add((when, part.TryGetProperty("apply", out var apply) ? ReadModels(apply) : new List<ModelReference>()));
                    }
                }

                return file;
            }

            public (BakedModel[] Models, int[] Weights) Select(BlockStateInfo state, ModelLibrary library)
            {
                if (_parts.Count > 0)
                {
                    // Every matching piece adds its first model; together they make one model.
                    var pieces = _parts.Where(p => p.When(state) && p.Models.Count > 0).Select(p => library.Bake(p.Models[0])).ToList();
                    var combined = new BakedModel(pieces.SelectMany(p => p.Quads).ToList(), pieces.Count == 0 || pieces[0].AmbientOcclusion);
                    return (new[] { WithEntityFallback(combined, state, library, _parts.SelectMany(p => p.Models).FirstOrDefault()) }, new[] { 1 });
                }

                foreach (var (when, models) in _variants)
                {
                    if (models.Count == 0 || !when.All(p => state.GetProperty(p.Key) == p.Value))
                        continue;
                    var baked = models.Select(m => WithEntityFallback(library.Bake(m), state, library, m)).ToArray();
                    return (baked, models.Select(m => Math.Max(1, m.Weight)).ToArray());
                }

                return (Array.Empty<BakedModel>(), Array.Empty<int>());
            }

            // Chests and shulker boxes have no model elements (the game draws them as entities); show a box with their particle texture.
            private static BakedModel WithEntityFallback(BakedModel model, BlockStateInfo state, ModelLibrary library, ModelReference reference)
            {
                if (model.Quads.Count > 0 || reference.Model == null)
                    return model;
                var texture = library.GetParticleTexture(reference.Model);
                if (texture == null)
                    return model;
                if (state.Name.EndsWith("chest", StringComparison.Ordinal))
                    return library.BakeBox(texture, new Vector3(1, 0, 1) / 16, new Vector3(15, 14, 15) / 16);
                if (state.Name.EndsWith("shulker_box", StringComparison.Ordinal))
                    return library.BakeBox(texture, Vector3.Zero, Vector3.One);
                return model;
            }

            private static List<ModelReference> ReadModels(JsonElement json)
            {
                var entries = json.ValueKind == JsonValueKind.Array ? json.EnumerateArray().ToList() : new List<JsonElement> { json };
                return entries
                    .Where(e => e.ValueKind == JsonValueKind.Object && e.TryGetProperty("model", out _))
                    .Select(e => new ModelReference(
                        e.GetProperty("model").GetString(),
                        e.TryGetProperty("x", out var x) ? x.GetInt32() : 0,
                        e.TryGetProperty("y", out var y) ? y.GetInt32() : 0,
                        e.TryGetProperty("uvlock", out var uvLock) && uvLock.ValueKind == JsonValueKind.True,
                        e.TryGetProperty("weight", out var weight) ? weight.GetInt32() : 1))
                    .ToList();
            }

            private static Func<BlockStateInfo, bool> Condition(JsonElement json)
            {
                if (json.TryGetProperty("OR", out var any) && any.ValueKind == JsonValueKind.Array)
                {
                    var conditions = any.EnumerateArray().Select(Condition).ToList();
                    return state => conditions.Any(c => c(state));
                }

                if (json.TryGetProperty("AND", out var all) && all.ValueKind == JsonValueKind.Array)
                {
                    var conditions = all.EnumerateArray().Select(Condition).ToList();
                    return state => conditions.All(c => c(state));
                }

                var tests = json.EnumerateObject()
                    .Select(p => (Name: p.Name, Values: ValueText(p.Value).Split('|')))
                    .ToList();
                return state => tests.All(t => t.Values.Contains(state.GetProperty(t.Name), StringComparer.Ordinal));
            }

            private static string ValueText(JsonElement value) => value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => value.GetRawText(),
            };
        }

        /// <summary>
        /// Colours for faces with a tint index: grass and foliage from the pack's colour maps, the rest fixed like the game.
        /// </summary>
        private sealed class TintTable
        {
            private static readonly HashSet<string> GrassTinted = new HashSet<string>(StringComparer.Ordinal) { "grass_block", "grass", "tall_grass", "fern", "large_fern", "potted_fern", "sugar_cane" };
            private static readonly HashSet<string> FoliageTinted = new HashSet<string>(StringComparer.Ordinal) { "oak_leaves", "jungle_leaves", "acacia_leaves", "dark_oak_leaves", "vine" };

            private readonly Vector4 _grass;
            private readonly Vector4 _foliage;

            public TintTable(ResourcePackStack pack)
            {
                _grass = SampleColorMap(pack, "grass") ?? Rgb(0x91BD59);
                _foliage = SampleColorMap(pack, "foliage") ?? Rgb(0x77AB2F);
            }

            public Vector4 For(BlockStateInfo state)
            {
                if (GrassTinted.Contains(state.Name))
                    return _grass;
                if (FoliageTinted.Contains(state.Name))
                    return _foliage;
                switch (state.Name)
                {
                    case "spruce_leaves":
                        return Rgb(0x619961);
                    case "birch_leaves":
                        return Rgb(0x80A755);
                    case "lily_pad":
                        return Rgb(0x208030);
                    case "water":
                    case "bubble_column":
                        return WaterColor;
                    case "attached_melon_stem":
                    case "attached_pumpkin_stem":
                        return Rgb(0xE0C71C);
                    case "melon_stem":
                    case "pumpkin_stem":
                        {
                            var age = int.Parse(state.GetProperty("age") ?? "0", System.Globalization.CultureInfo.InvariantCulture);
                            return new Vector4(age * 32 / 255f, (255 - (age * 8)) / 255f, age * 4 / 255f, 1);
                        }

                    case "redstone_wire":
                        {
                            var power = int.Parse(state.GetProperty("power") ?? "0", System.Globalization.CultureInfo.InvariantCulture) / 15f;
                            var red = power == 0 ? 0.3f : (power * 0.6f) + 0.4f;
                            return new Vector4(red, Math.Max(0, (power * power * 0.7f) - 0.5f), Math.Max(0, (power * power * 0.6f) - 0.7f), 1);
                        }

                    default:
                        return Vector4.One;
                }
            }

            private static Vector4? SampleColorMap(ResourcePackStack pack, string name)
            {
                var map = ReadPng(pack, $"assets/minecraft/textures/colormap/{name}.png");
                if (map == null || map.Width < 256 || map.Height < 256)
                    return null;
                var x = (int)((1 - Temperature) * 255);
                var y = (int)((1 - (Downfall * Temperature)) * 255);
                var o = map.OffsetOf(x, y);
                return new Vector4(map.Pixels[o] / 255f, map.Pixels[o + 1] / 255f, map.Pixels[o + 2] / 255f, 1);
            }
        }
    }
}
