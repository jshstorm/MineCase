using System.Collections.Generic;
using System.Numerics;
using MineCase.Client.Net.Rendering;

namespace MineCase.Client.Net
{
    /// <summary>
    /// Plain vertex data for one surface; triangles wind counter-clockwise seen from the front.
    /// </summary>
    public sealed class MeshData
    {
        public List<Vector3> Positions { get; } = new List<Vector3>();

        public List<Vector3> Normals { get; } = new List<Vector3>();

        public List<Vector2> Uvs { get; } = new List<Vector2>();

        public List<Vector4> Colors { get; } = new List<Vector4>();

        public List<int> Indices { get; } = new List<int>();

        public bool IsEmpty => Indices.Count == 0;

        // Flipping splits the quad along the other diagonal so corner shading interpolates without a crease.
        internal void AddQuad(Vector3 origin, IReadOnlyList<Vector3> positions, IReadOnlyList<Vector2> uvs, Vector3 normal, IReadOnlyList<Vector4> colors, bool flip)
        {
            var start = Positions.Count;
            for (var i = 0; i < 4; i++)
            {
                Positions.Add(origin + positions[i]);
                Uvs.Add(uvs[i]);
                Normals.Add(normal);
                Colors.Add(colors[i]);
            }

            if (flip)
            {
                Indices.Add(start + 1);
                Indices.Add(start + 2);
                Indices.Add(start + 3);
                Indices.Add(start + 1);
                Indices.Add(start + 3);
                Indices.Add(start);
            }
            else
            {
                Indices.Add(start);
                Indices.Add(start + 1);
                Indices.Add(start + 2);
                Indices.Add(start);
                Indices.Add(start + 2);
                Indices.Add(start + 3);
            }
        }
    }

    /// <summary>
    /// Geometry of one 16x16x16 section in section-local coordinates, split by how it is blended.
    /// </summary>
    public sealed class SectionMesh
    {
        public SectionMesh(int chunkX, int sectionY, int chunkZ)
        {
            ChunkX = chunkX;
            SectionY = sectionY;
            ChunkZ = chunkZ;
        }

        public int ChunkX { get; }

        public int SectionY { get; }

        public int ChunkZ { get; }

        public MeshData Opaque { get; } = new MeshData();

        /// <summary>
        /// Gets the alpha-tested surfaces (leaves, plants, glass).
        /// </summary>
        public MeshData Cutout { get; } = new MeshData();

        public MeshData Translucent { get; } = new MeshData();

        /// <summary>
        /// Gets the faces of blocks with collision as a triangle list (three vertices per triangle).
        /// </summary>
        public List<Vector3> CollisionTriangles { get; } = new List<Vector3>();

        public bool IsEmpty => Opaque.IsEmpty && Cutout.IsEmpty && Translucent.IsEmpty;

        public MeshData Layer(RenderLayer layer) => layer switch
        {
            RenderLayer.Opaque => Opaque,
            RenderLayer.Cutout => Cutout,
            _ => Translucent,
        };
    }

    public static class ChunkMesher
    {
        // How much each fully opaque neighbour around a corner darkens it, like the game's smooth lighting.
        private const float AmbientOcclusionStep = 0.2f;

        public static SectionMesh BuildSection(ClientWorld world, ChunkColumn column, int sectionY) =>
            BuildSection(world, column, sectionY, FlatColorBlockModels.Instance);

        /// <summary>
        /// Builds the visible faces of one section, looking into neighbouring columns of <paramref name="world"/> at the edges.
        /// </summary>
        /// <param name="world">World holding the column and its neighbours.</param>
        /// <param name="column">Column the section belongs to.</param>
        /// <param name="sectionY">Section index from the bottom.</param>
        /// <param name="models">Block appearance.</param>
        /// <returns>The section geometry.</returns>
        public static SectionMesh BuildSection(ClientWorld world, ChunkColumn column, int sectionY, IBlockModels models)
        {
            var mesh = new SectionMesh(column.ChunkX, sectionY, column.ChunkZ);
            var states = column.GetSection(sectionY);
            if (states == null)
                return mesh;

            var area = new Neighbourhood(world, column, states, sectionY, models);
            var shading = new float[4];
            var colors = new Vector4[4];
            for (var y = 0; y < ChunkColumn.EdgeWidth; y++)
            {
                for (var z = 0; z < ChunkColumn.EdgeWidth; z++)
                {
                    for (var x = 0; x < ChunkColumn.EdgeWidth; x++)
                    {
                        var state = states[ChunkColumn.Index(x, y, z)];
                        if (state == 0)
                            continue;

                        var info = models.Get(state);
                        if (info.Fluid != FluidKind.None)
                            AddFluid(mesh, area, info, x, y, z, colors);

                        var model = info.Select(area.BaseX + x, area.BaseY + y, area.BaseZ + z);
                        if (model == null)
                            continue;

                        var origin = new Vector3(x, y, z);
                        foreach (var quad in model.Quads)
                        {
                            if (quad.CullFace is Direction cull)
                            {
                                var neighbour = area.Info(x, y, z, cull);
                                if (neighbour.Occludes(cull.Opposite()) || (info.CullsSameBlock && neighbour.BlockKey == info.BlockKey))
                                    continue;
                            }

                            var color = quad.TintIndex >= 0 ? quad.Color * info.Tint : quad.Color;
                            var shade = quad.Shade ? quad.Facing.Shade() : 1f;
                            if (model.AmbientOcclusion && quad.IsAxisAligned)
                                AmbientOcclusion(area, quad, x, y, z, shading);
                            else
                                shading[0] = shading[1] = shading[2] = shading[3] = 1;

                            for (var i = 0; i < 4; i++)
                            {
                                var light = shade * shading[i];
                                colors[i] = new Vector4(color.X * light, color.Y * light, color.Z * light, color.W);
                            }

                            var flip = shading[0] + shading[2] > shading[1] + shading[3];
                            mesh.Layer(quad.Layer).AddQuad(origin, quad.Positions, quad.Uvs, quad.Normal, colors, flip);
                            if (info.HasCollision)
                                AddCollision(mesh, origin, quad.Positions);
                        }
                    }
                }
            }

            return mesh;
        }

        private static void AmbientOcclusion(Neighbourhood area, BakedQuad quad, int x, int y, int z, float[] shading)
        {
            var normal = quad.Facing.Normal();
            var (axis, tangent1, tangent2) = quad.Facing switch
            {
                Direction.Down or Direction.Up => (1, 0, 2),
                Direction.North or Direction.South => (2, 0, 1),
                _ => (0, 2, 1),
            };

            // Faces on the block boundary are lit from the next block; inner faces (a slab top) from the block's own layer.
            var plane = quad.Positions[0][axis];
            var onBoundary = normal[axis] > 0 ? plane > 0.999f : plane < 0.001f;
            var layer = new Vector3(x, y, z) + (onBoundary ? normal : Vector3.Zero);

            for (var i = 0; i < 4; i++)
            {
                var corner = quad.Positions[i];
                var step1 = Unit(tangent1) * (corner[tangent1] < 0.5f ? -1 : 1);
                var step2 = Unit(tangent2) * (corner[tangent2] < 0.5f ? -1 : 1);
                var side1 = area.IsOccluder(layer + step1);
                var side2 = area.IsOccluder(layer + step2);
                var diagonal = (side1 && side2) || area.IsOccluder(layer + step1 + step2);
                shading[i] = 1 - (AmbientOcclusionStep * ((side1 ? 1 : 0) + (side2 ? 1 : 0) + (diagonal ? 1 : 0)));
            }
        }

        private static Vector3 Unit(int axis) => axis switch
        {
            0 => Vector3.UnitX,
            1 => Vector3.UnitY,
            _ => Vector3.UnitZ,
        };

        private static void AddFluid(SectionMesh mesh, Neighbourhood area, BlockRenderInfo info, int x, int y, int z, Vector4[] colors)
        {
            var appearance = info.Fluid == FluidKind.Water ? area.Models.Water : area.Models.Lava;
            var target = mesh.Layer(appearance.Layer);
            var height = area.Info(x, y, z, Direction.Up).Fluid == info.Fluid ? 1f : info.FluidHeight;
            var origin = new Vector3(x, y, z);
            var uvs = new Vector2[4];
            for (var i = 0; i < 4; i++)
            {
                var local = ModelGeometry.CornerUv(new Vector4(0, 0, 16, 16), 0, i);
                uvs[i] = new Vector2(appearance.UvRect.X + ((appearance.UvRect.Z - appearance.UvRect.X) * local.X), appearance.UvRect.Y + ((appearance.UvRect.W - appearance.UvRect.Y) * local.Y));
            }

            foreach (var face in Directions.All)
            {
                var neighbour = area.Info(x, y, z, face);
                if (neighbour.Fluid == info.Fluid || (face != Direction.Up && neighbour.Occludes(face.Opposite())))
                    continue;

                var corners = ModelGeometry.FaceCorners(face, Vector3.Zero, new Vector3(1, height, 1));
                var shade = face.Shade();
                for (var i = 0; i < 4; i++)
                    colors[i] = new Vector4(appearance.Color.X * shade, appearance.Color.Y * shade, appearance.Color.Z * shade, appearance.Color.W);
                target.AddQuad(origin, corners, uvs, face.Normal(), colors, false);

                // The surface is also seen from below, when swimming.
                if (face == Direction.Up)
                    target.AddQuad(origin, new[] { corners[3], corners[2], corners[1], corners[0] }, new[] { uvs[3], uvs[2], uvs[1], uvs[0] }, -face.Normal(), colors, false);
            }
        }

        private static void AddCollision(SectionMesh mesh, Vector3 origin, Vector3[] positions)
        {
            mesh.CollisionTriangles.Add(origin + positions[0]);
            mesh.CollisionTriangles.Add(origin + positions[1]);
            mesh.CollisionTriangles.Add(origin + positions[2]);
            mesh.CollisionTriangles.Add(origin + positions[0]);
            mesh.CollisionTriangles.Add(origin + positions[2]);
            mesh.CollisionTriangles.Add(origin + positions[3]);
        }

        /// <summary>
        /// Block lookups around a section in section-local coordinates, reaching into the column and the world at the edges.
        /// </summary>
        private sealed class Neighbourhood
        {
            private readonly ClientWorld _world;
            private readonly ChunkColumn _column;
            private readonly ushort[] _states;

            public Neighbourhood(ClientWorld world, ChunkColumn column, ushort[] states, int sectionY, IBlockModels models)
            {
                _world = world;
                _column = column;
                _states = states;
                Models = models;
                BaseX = column.ChunkX * ChunkColumn.EdgeWidth;
                BaseY = sectionY * ChunkColumn.EdgeWidth;
                BaseZ = column.ChunkZ * ChunkColumn.EdgeWidth;
            }

            public IBlockModels Models { get; }

            public int BaseX { get; }

            public int BaseY { get; }

            public int BaseZ { get; }

            public BlockRenderInfo Info(int x, int y, int z, Direction direction)
            {
                var n = direction.Normal();
                return Models.Get(State(x + (int)n.X, y + (int)n.Y, z + (int)n.Z));
            }

            public bool IsOccluder(Vector3 p) => Models.Get(State((int)p.X, (int)p.Y, (int)p.Z)).IsFullOpaqueCube;

            private ushort State(int x, int y, int z)
            {
                const int W = ChunkColumn.EdgeWidth;
                if (x >= 0 && x < W && z >= 0 && z < W)
                {
                    if (y >= 0 && y < W)
                        return _states[ChunkColumn.Index(x, y, z)];
                    return _column.GetState(x, BaseY + y, z);
                }

                return _world.GetState(BaseX + x, BaseY + y, BaseZ + z);
            }
        }
    }
}
