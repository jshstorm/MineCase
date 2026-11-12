using System;
using System.Collections.Generic;
using System.Numerics;
using MineCase.Block;

namespace MineCase.Client.Net
{
    /// <summary>
    /// Plain vertex data for one surface; triangles wind counter-clockwise seen from the front.
    /// </summary>
    public sealed class MeshData
    {
        public List<Vector3> Positions { get; } = new List<Vector3>();

        public List<Vector3> Normals { get; } = new List<Vector3>();

        public List<Vector4> Colors { get; } = new List<Vector4>();

        public List<int> Indices { get; } = new List<int>();

        public bool IsEmpty => Indices.Count == 0;

        internal void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal, Vector4 color)
        {
            var start = Positions.Count;
            Positions.Add(v0);
            Positions.Add(v1);
            Positions.Add(v2);
            Positions.Add(v3);
            for (var i = 0; i < 4; i++)
            {
                Normals.Add(normal);
                Colors.Add(color);
            }

            Indices.Add(start);
            Indices.Add(start + 1);
            Indices.Add(start + 2);
            Indices.Add(start);
            Indices.Add(start + 2);
            Indices.Add(start + 3);
        }
    }

    /// <summary>
    /// Geometry of one 16x16x16 section in section-local coordinates.
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

        public MeshData Translucent { get; } = new MeshData();

        /// <summary>
        /// Gets the solid faces as a triangle list (three vertices per triangle) for collision.
        /// </summary>
        public List<Vector3> CollisionTriangles { get; } = new List<Vector3>();

        public bool IsEmpty => Opaque.IsEmpty && Translucent.IsEmpty;
    }

    public static class ChunkMesher
    {
        private const float LiquidSurfaceHeight = 0.875f;

        private static readonly Face[] Faces =
        {
            new Face(new Vector3(0, 1, 0), 1.0f, new Vector3(0, 1, 0), new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0)),
            new Face(new Vector3(0, -1, 0), 0.5f, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1)),
            new Face(new Vector3(1, 0, 0), 0.8f, new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1)),
            new Face(new Vector3(-1, 0, 0), 0.8f, new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0)),
            new Face(new Vector3(0, 0, 1), 0.65f, new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1)),
            new Face(new Vector3(0, 0, -1), 0.65f, new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0)),
        };

        /// <summary>
        /// Builds the visible faces of one section, looking into neighbouring columns of <paramref name="world"/> at the edges.
        /// </summary>
        /// <param name="world">World holding the column and its neighbours.</param>
        /// <param name="column">Column the section belongs to.</param>
        /// <param name="sectionY">Section index from the bottom.</param>
        /// <returns>The section geometry.</returns>
        public static SectionMesh BuildSection(ClientWorld world, ChunkColumn column, int sectionY)
        {
            var mesh = new SectionMesh(column.ChunkX, sectionY, column.ChunkZ);
            var blocks = column.GetSection(sectionY);
            if (blocks == null)
                return mesh;

            var baseX = column.ChunkX * ChunkColumn.EdgeWidth;
            var baseY = sectionY * ChunkColumn.EdgeWidth;
            var baseZ = column.ChunkZ * ChunkColumn.EdgeWidth;

            for (var y = 0; y < ChunkColumn.EdgeWidth; y++)
            {
                for (var z = 0; z < ChunkColumn.EdgeWidth; z++)
                {
                    for (var x = 0; x < ChunkColumn.EdgeWidth; x++)
                    {
                        var id = blocks[ChunkColumn.Index(x, y, z)];
                        if (id == BlockId.Air)
                            continue;

                        var appearance = BlockAppearance.Get(id);
                        var origin = new Vector3(x, y, z);
                        if (appearance.Shape == BlockShape.Cross)
                        {
                            AddCross(appearance.IsTranslucent ? mesh.Translucent : mesh.Opaque, origin, appearance.SideColor);
                            continue;
                        }

                        if (appearance.Shape != BlockShape.Cube)
                            continue;

                        var target = appearance.IsTranslucent ? mesh.Translucent : mesh.Opaque;
                        var isLiquid = !appearance.Solid;
                        foreach (var face in Faces)
                        {
                            var nx = x + (int)face.Normal.X;
                            var ny = y + (int)face.Normal.Y;
                            var nz = z + (int)face.Normal.Z;
                            var neighbor = GetNeighbor(world, column, blocks, nx, ny, nz, baseX, baseY, baseZ);
                            if (neighbor == id || BlockAppearance.Get(neighbor).Occludes)
                                continue;

                            var color = face.Normal.Y > 0 ? appearance.TopColor : appearance.SideColor;
                            color = new Vector4(color.X * face.Shade, color.Y * face.Shade, color.Z * face.Shade, color.W);
                            var v0 = origin + face.V0;
                            var v1 = origin + face.V1;
                            var v2 = origin + face.V2;
                            var v3 = origin + face.V3;
                            if (isLiquid && GetNeighbor(world, column, blocks, x, y + 1, z, baseX, baseY, baseZ) != id)
                            {
                                v0 = LowerTop(v0, origin);
                                v1 = LowerTop(v1, origin);
                                v2 = LowerTop(v2, origin);
                                v3 = LowerTop(v3, origin);
                            }

                            target.AddQuad(v0, v1, v2, v3, face.Normal, color);
                            if (appearance.Solid)
                            {
                                mesh.CollisionTriangles.Add(v0);
                                mesh.CollisionTriangles.Add(v1);
                                mesh.CollisionTriangles.Add(v2);
                                mesh.CollisionTriangles.Add(v0);
                                mesh.CollisionTriangles.Add(v2);
                                mesh.CollisionTriangles.Add(v3);
                            }
                        }
                    }
                }
            }

            return mesh;
        }

        private static BlockId GetNeighbor(ClientWorld world, ChunkColumn column, BlockId[] blocks, int x, int y, int z, int baseX, int baseY, int baseZ)
        {
            if (x >= 0 && x < ChunkColumn.EdgeWidth && y >= 0 && y < ChunkColumn.EdgeWidth && z >= 0 && z < ChunkColumn.EdgeWidth)
                return blocks[ChunkColumn.Index(x, y, z)];
            if (x >= 0 && x < ChunkColumn.EdgeWidth && z >= 0 && z < ChunkColumn.EdgeWidth)
                return column.GetBlock(x, baseY + y, z);
            return world.GetBlock(baseX + x, baseY + y, baseZ + z);
        }

        private static Vector3 LowerTop(Vector3 vertex, Vector3 origin) =>
            vertex.Y > origin.Y ? new Vector3(vertex.X, origin.Y + LiquidSurfaceHeight, vertex.Z) : vertex;

        private static void AddCross(MeshData target, Vector3 origin, Vector4 color)
        {
            const float Inset = 0.15f;
            const float Outset = 1 - Inset;
            var a0 = origin + new Vector3(Inset, 0, Inset);
            var a1 = origin + new Vector3(Outset, 0, Outset);
            var b0 = origin + new Vector3(Outset, 0, Inset);
            var b1 = origin + new Vector3(Inset, 0, Outset);
            var up = new Vector3(0, 1, 0);
            var normal = new Vector3(0, 1, 0);

            // Both windings so the planes are visible from either side without a double-sided material.
            target.AddQuad(a0, a1, a1 + up, a0 + up, normal, color);
            target.AddQuad(a1, a0, a0 + up, a1 + up, normal, color);
            target.AddQuad(b0, b1, b1 + up, b0 + up, normal, color);
            target.AddQuad(b1, b0, b0 + up, b1 + up, normal, color);
        }

        private readonly struct Face
        {
            public Face(Vector3 normal, float shade, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
            {
                Normal = normal;
                Shade = shade;
                V0 = v0;
                V1 = v1;
                V2 = v2;
                V3 = v3;
            }

            public Vector3 Normal { get; }

            public float Shade { get; }

            public Vector3 V0 { get; }

            public Vector3 V1 { get; }

            public Vector3 V2 { get; }

            public Vector3 V3 { get; }
        }
    }
}
