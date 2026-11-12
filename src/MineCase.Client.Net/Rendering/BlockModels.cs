using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MineCase.Client.Net.Rendering
{
    public enum Direction
    {
        Down,
        Up,
        North,
        South,
        West,
        East,
    }

    public enum FluidKind
    {
        None,
        Water,
        Lava,
    }

    public static class Directions
    {
        public static readonly Direction[] All = { Direction.Down, Direction.Up, Direction.North, Direction.South, Direction.West, Direction.East };

        public static Vector3 Normal(this Direction direction) => direction switch
        {
            Direction.Down => new Vector3(0, -1, 0),
            Direction.Up => new Vector3(0, 1, 0),
            Direction.North => new Vector3(0, 0, -1),
            Direction.South => new Vector3(0, 0, 1),
            Direction.West => new Vector3(-1, 0, 0),
            _ => new Vector3(1, 0, 0),
        };

        public static Direction Opposite(this Direction direction) => (Direction)((int)direction ^ 1);

        /// <summary>
        /// Brightness the game gives faces by direction so that the sides of a cube can be told apart.
        /// </summary>
        /// <param name="direction">Face direction.</param>
        /// <returns>Brightness multiplier.</returns>
        public static float Shade(this Direction direction) => direction switch
        {
            Direction.Down => 0.5f,
            Direction.Up => 1.0f,
            Direction.North or Direction.South => 0.8f,
            _ => 0.6f,
        };

        public static Direction Nearest(Vector3 normal)
        {
            var abs = Vector3.Abs(normal);
            if (abs.Y >= abs.X && abs.Y >= abs.Z)
                return normal.Y >= 0 ? Direction.Up : Direction.Down;
            if (abs.X >= abs.Z)
                return normal.X >= 0 ? Direction.East : Direction.West;
            return normal.Z >= 0 ? Direction.South : Direction.North;
        }

        public static bool TryParse(string name, out Direction direction)
        {
            switch (name)
            {
                case "down":
                case "bottom":
                    direction = Direction.Down;
                    return true;
                case "up":
                case "top":
                    direction = Direction.Up;
                    return true;
                case "north":
                    direction = Direction.North;
                    return true;
                case "south":
                    direction = Direction.South;
                    return true;
                case "west":
                    direction = Direction.West;
                    return true;
                case "east":
                    direction = Direction.East;
                    return true;
                default:
                    direction = default;
                    return false;
            }
        }
    }

    /// <summary>
    /// One textured face of a block model in block space (0..1), corners counter-clockwise seen from the front.
    /// </summary>
    public sealed class BakedQuad
    {
        public BakedQuad(Vector3[] positions, Vector2[] uvs, Direction? cullFace, int tintIndex, bool shade, string texture, Vector4 color)
        {
            Positions = positions;
            Uvs = uvs;
            CullFace = cullFace;
            TintIndex = tintIndex;
            Shade = shade;
            Texture = texture;
            Color = color;
            Normal = Vector3.Normalize(Vector3.Cross(positions[1] - positions[0], positions[2] - positions[0]));
            Facing = Directions.Nearest(Normal);
            IsAxisAligned = Vector3.Dot(Normal, Facing.Normal()) > 0.999f;
        }

        public Vector3[] Positions { get; }

        /// <summary>
        /// Gets the texture coordinates: inside the texture (0..1) until <see cref="Resolve"/>, in the atlas afterwards.
        /// </summary>
        public Vector2[] Uvs { get; private set; }

        public Vector3 Normal { get; }

        public Direction Facing { get; }

        public bool IsAxisAligned { get; }

        public Direction? CullFace { get; }

        public int TintIndex { get; }

        public bool Shade { get; }

        public string Texture { get; }

        public Vector4 Color { get; }

        public RenderLayer Layer { get; private set; }

        public bool IsDegenerate => float.IsNaN(Normal.X);

        internal void Resolve(TextureAtlas atlas)
        {
            var sprite = atlas.GetSprite(Texture);
            Uvs = Uvs.Select(sprite.Map).ToArray();
            Layer = Color.W < 1 ? RenderLayer.Translucent : sprite.Layer;
        }
    }

    public sealed class BakedModel
    {
        public static readonly BakedModel Empty = new BakedModel(Array.Empty<BakedQuad>(), false);

        public BakedModel(IReadOnlyList<BakedQuad> quads, bool ambientOcclusion)
        {
            Quads = quads;
            AmbientOcclusion = ambientOcclusion;
        }

        public IReadOnlyList<BakedQuad> Quads { get; }

        public bool AmbientOcclusion { get; }
    }

    /// <summary>
    /// What a fluid surface looks like: an atlas rectangle, a tint and the layer it is blended in.
    /// </summary>
    public readonly struct FluidAppearance
    {
        public FluidAppearance(Vector4 uvRect, Vector4 color, RenderLayer layer)
        {
            UvRect = uvRect;
            Color = color;
            Layer = layer;
        }

        public Vector4 UvRect { get; }

        public Vector4 Color { get; }

        public RenderLayer Layer { get; }
    }

    /// <summary>
    /// Everything the mesher needs to know about one block state.
    /// </summary>
    public sealed class BlockRenderInfo
    {
        public static readonly BlockRenderInfo Empty = new BlockRenderInfo(Array.Empty<BakedModel>(), Array.Empty<int>(), 0, false, Vector4.One, FluidKind.None, 0);

        private const byte AllFaces = 0x3F;

        private readonly int _totalWeight;

        public BlockRenderInfo(IReadOnlyList<BakedModel> variants, IReadOnlyList<int> weights, ushort blockKey, bool hasCollision, Vector4 tint, FluidKind fluid, float fluidHeight)
        {
            Variants = variants;
            Weights = weights;
            BlockKey = blockKey;
            HasCollision = hasCollision;
            Tint = tint;
            Fluid = fluid;
            FluidHeight = fluidHeight;
            _totalWeight = weights.Sum();
        }

        public IReadOnlyList<BakedModel> Variants { get; }

        public IReadOnlyList<int> Weights { get; }

        /// <summary>
        /// Gets the first state id of the block, the same for all its states.
        /// </summary>
        public ushort BlockKey { get; }

        public bool HasCollision { get; }

        public Vector4 Tint { get; }

        public FluidKind Fluid { get; }

        public float FluidHeight { get; }

        /// <summary>
        /// Gets the sides the block covers completely with opaque faces, one bit per <see cref="Direction"/>.
        /// </summary>
        public byte OccludedFaces { get; private set; }

        /// <summary>
        /// Gets a value indicating whether faces against the same block are hidden (glass next to glass).
        /// </summary>
        public bool CullsSameBlock { get; private set; }

        public bool IsFullOpaqueCube => OccludedFaces == AllFaces;

        public bool Occludes(Direction side) => (OccludedFaces & (1 << (int)side)) != 0;

        public BakedModel Select(int x, int y, int z)
        {
            if (Variants.Count == 0)
                return null;
            if (Variants.Count == 1)
                return Variants[0];

            // Same idea as the game: a stable pseudo-random pick per position.
            long seed = (x * 3129871L) ^ (z * 116129781L) ^ y;
            seed = (seed * seed * 42317861L) + (seed * 11L);
            var pick = (int)(((seed >> 16) & 0x7FFFFFFF) % _totalWeight);
            for (var i = 0; i < Variants.Count; i++)
            {
                pick -= Weights[i];
                if (pick < 0)
                    return Variants[i];
            }

            return Variants[0];
        }

        internal void ComputeOcclusion(bool cullsSameBlockWhenFullCube)
        {
            byte opaque = 0;
            byte covered = 0;
            if (Variants.Count > 0)
            {
                foreach (var quad in Variants[0].Quads)
                {
                    if (!quad.IsAxisAligned || !CoversSide(quad))
                        continue;
                    covered |= (byte)(1 << (int)quad.Facing);
                    if (quad.Layer == RenderLayer.Opaque)
                        opaque |= (byte)(1 << (int)quad.Facing);
                }
            }

            OccludedFaces = opaque;
            CullsSameBlock = cullsSameBlockWhenFullCube && covered == AllFaces && opaque != AllFaces;
        }

        private static bool CoversSide(BakedQuad quad)
        {
            const float Epsilon = 1e-3f;
            var (axis, positive) = quad.Facing switch
            {
                Direction.Down => (1, false),
                Direction.Up => (1, true),
                Direction.North => (2, false),
                Direction.South => (2, true),
                Direction.West => (0, false),
                _ => (0, true),
            };
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            foreach (var p in quad.Positions)
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            var plane = positive ? 1f : 0f;
            for (var i = 0; i < 3; i++)
            {
                if (i == axis)
                {
                    if (Math.Abs(min[i] - plane) > Epsilon || Math.Abs(max[i] - plane) > Epsilon)
                        return false;
                }
                else if (min[i] > Epsilon || max[i] < 1 - Epsilon)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Block appearance by global block state id, plus the atlas the quads point into.
    /// </summary>
    public interface IBlockModels
    {
        TextureAtlas Atlas { get; }

        FluidAppearance Water { get; }

        FluidAppearance Lava { get; }

        BlockRenderInfo Get(int stateId);
    }
}
