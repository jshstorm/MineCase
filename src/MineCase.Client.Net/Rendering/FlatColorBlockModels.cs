using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MineCase.Block;

namespace MineCase.Client.Net.Rendering
{
    /// <summary>
    /// Plain coloured cubes and crosses (<see cref="BlockAppearance"/>) for when no resource pack is loaded.
    /// </summary>
    public sealed class FlatColorBlockModels : IBlockModels
    {
        private static readonly Lazy<FlatColorBlockModels> LazyInstance = new Lazy<FlatColorBlockModels>(() => new FlatColorBlockModels());

        private readonly BlockRenderInfo[] _infos;

        private FlatColorBlockModels()
        {
            Atlas = TextureAtlas.Build(new Dictionary<string, RgbaImage>());
            var models = new Dictionary<ushort, BakedModel>();
            _infos = new BlockRenderInfo[BlockStateTable.StateCount];
            for (var id = 0; id < _infos.Length; id++)
            {
                var state = BlockStateTable.Get(id);
                var key = state.Block.FirstStateId;
                if (!models.TryGetValue(key, out var model))
                    models[key] = model = BlockRenderInfos.FlatModel((BlockId)key);
                _infos[id] = BlockRenderInfos.Create(state, new[] { model }, new[] { 1 }, Vector4.One);
            }

            BlockRenderInfos.Finish(_infos, Atlas);
            Water = Fluid(BlockId.Water);
            Lava = Fluid(BlockId.Lava);
        }

        public static FlatColorBlockModels Instance => LazyInstance.Value;

        public TextureAtlas Atlas { get; }

        public FluidAppearance Water { get; }

        public FluidAppearance Lava { get; }

        public BlockRenderInfo Get(int stateId) => (uint)stateId < (uint)_infos.Length ? _infos[stateId] : BlockRenderInfo.Empty;

        private FluidAppearance Fluid(BlockId id)
        {
            var color = BlockAppearance.Get(id).TopColor;
            return new FluidAppearance(Atlas.White.Rect, color, color.W < 1 ? RenderLayer.Translucent : RenderLayer.Opaque);
        }
    }

    /// <summary>
    /// Pieces shared by the flat and the resource pack models.
    /// </summary>
    internal static class BlockRenderInfos
    {
        public const float SourceFluidHeight = 8f / 9;

        private static readonly HashSet<string> AquaticPlants = new HashSet<string>(StringComparer.Ordinal) { "seagrass", "tall_seagrass", "kelp", "kelp_plant", "bubble_column" };

        public static BlockRenderInfo Create(BlockStateInfo state, IReadOnlyList<BakedModel> variants, IReadOnlyList<int> weights, Vector4 tint)
        {
            var fluid = FluidKind.None;
            var height = 0f;
            if (state.Name is "water" or "lava")
            {
                fluid = state.Name == "water" ? FluidKind.Water : FluidKind.Lava;
                var level = int.TryParse(state.GetProperty("level"), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var l) ? l : 0;
                height = level is > 0 and < 8 ? (8 - level) / 9f : SourceFluidHeight;
            }
            else if (state.GetProperty("waterlogged") == "true" || AquaticPlants.Contains(state.Name))
            {
                fluid = FluidKind.Water;
                height = SourceFluidHeight;
            }

            return new BlockRenderInfo(variants, weights, state.Block.FirstStateId, state.Block.HasCollision, tint, fluid, height);
        }

        /// <summary>
        /// Points every quad at the atlas (once per shared model) and works out what each state hides.
        /// </summary>
        /// <param name="infos">All block states.</param>
        /// <param name="atlas">Atlas holding the quads' textures.</param>
        public static void Finish(IEnumerable<BlockRenderInfo> infos, TextureAtlas atlas)
        {
            var resolved = new HashSet<BakedQuad>(ReferenceEqualityComparer.Instance);
            foreach (var info in infos)
            {
                foreach (var quad in info.Variants.SelectMany(v => v.Quads))
                {
                    if (resolved.Add(quad))
                        quad.Resolve(atlas);
                }

                info.ComputeOcclusion(!BlockStateTable.Get(info.BlockKey).Name.EndsWith("_leaves", StringComparison.Ordinal));
            }
        }

        public static BakedModel FlatModel(BlockId id)
        {
            if (id is BlockId.Water or BlockId.Lava)
                return BakedModel.Empty;

            var appearance = BlockAppearance.Get(id);
            switch (appearance.Shape)
            {
                case BlockShape.Cube:
                    return new BakedModel(
                        Directions.All.Select(d => Quad(ModelGeometry.FaceCorners(d, Vector3.Zero, Vector3.One), d, d == Direction.Up ? appearance.TopColor : appearance.SideColor)).ToList(),
                        true);
                case BlockShape.Cross:
                    const float Inset = 0.15f;
                    const float Outset = 1 - Inset;
                    var a0 = new Vector3(Inset, 0, Inset);
                    var a1 = new Vector3(Outset, 0, Outset);
                    var b0 = new Vector3(Outset, 0, Inset);
                    var b1 = new Vector3(Inset, 0, Outset);
                    var up = Vector3.UnitY;
                    var color = appearance.SideColor;

                    // Both windings so the planes show from either side.
                    return new BakedModel(
                        new[]
                        {
                            Quad(new[] { a0 + up, a0, a1, a1 + up }, null, color),
                            Quad(new[] { a1 + up, a1, a0, a0 + up }, null, color),
                            Quad(new[] { b0 + up, b0, b1, b1 + up }, null, color),
                            Quad(new[] { b1 + up, b1, b0, b0 + up }, null, color),
                        },
                        false);
                default:
                    return BakedModel.Empty;
            }
        }

        private static BakedQuad Quad(Vector3[] corners, Direction? cullFace, Vector4 color)
        {
            var uvs = Enumerable.Range(0, 4).Select(i => ModelGeometry.CornerUv(new Vector4(0, 0, 16, 16), 0, i)).ToArray();
            return new BakedQuad(corners, uvs, cullFace, -1, true, TextureAtlas.WhiteSprite, color);
        }
    }
}
