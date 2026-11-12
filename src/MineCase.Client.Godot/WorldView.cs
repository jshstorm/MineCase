using System.Collections.Generic;
using System.Linq;
using Godot;
using MineCase.Client.Net;
using MineCase.Client.Net.Rendering;
using NVector3 = System.Numerics.Vector3;
using NVector4 = System.Numerics.Vector4;

namespace MineCase.GodotClient
{
    /// <summary>
    /// Turns chunk columns into section meshes and collision bodies, nearest dirty column first.
    /// </summary>
    public partial class WorldView : Node3D
    {
        private readonly Dictionary<(int X, int Z), Node3D> _columnNodes = new Dictionary<(int X, int Z), Node3D>();
        private readonly HashSet<(int X, int Z)> _dirty = new HashSet<(int X, int Z)>();
        private readonly Dictionary<(int X, int Z), int> _triangles = new Dictionary<(int X, int Z), int>();
        private IBlockModels _models = FlatColorBlockModels.Instance;
        private StandardMaterial3D _opaqueMaterial;
        private StandardMaterial3D _cutoutMaterial;
        private StandardMaterial3D _translucentMaterial;

        public ClientWorld World { get; } = new ClientWorld();

        public int TriangleCount { get; private set; }

        public int PendingColumns => _dirty.Count;

        public override void _Ready()
        {
            if (_opaqueMaterial == null)
                SetModels(_models);
        }

        /// <summary>
        /// Switches block appearance (flat colours or a resource pack) and rebuilds every loaded column.
        /// </summary>
        /// <param name="models">Block models and their texture atlas.</param>
        public void SetModels(IBlockModels models)
        {
            _models = models;
            var atlas = models.Atlas;
            var image = Image.CreateFromData(atlas.Width, atlas.Height, false, Image.Format.Rgba8, atlas.Pixels);
            image.GenerateMipmaps();
            var texture = ImageTexture.CreateFromImage(image);
            _opaqueMaterial = CreateMaterial(texture, BaseMaterial3D.TransparencyEnum.Disabled);
            _cutoutMaterial = CreateMaterial(texture, BaseMaterial3D.TransparencyEnum.AlphaScissor);
            _translucentMaterial = CreateMaterial(texture, BaseMaterial3D.TransparencyEnum.Alpha);
            foreach (var column in World.Columns)
                _dirty.Add((column.ChunkX, column.ChunkZ));
        }

        private static StandardMaterial3D CreateMaterial(Texture2D atlas, BaseMaterial3D.TransparencyEnum transparency) => new StandardMaterial3D
        {
            AlbedoTexture = atlas,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.NearestWithMipmaps,
            VertexColorUseAsAlbedo = true,
            VertexColorIsSrgb = true,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = transparency,
            AlphaScissorThreshold = 0.5f,
        };

        public void AddColumn(ChunkColumn column)
        {
            World.SetColumn(column);
            MarkDirtyWithNeighbours(column.ChunkX, column.ChunkZ);
        }

        public void RemoveColumn(int chunkX, int chunkZ)
        {
            if (!World.RemoveColumn(chunkX, chunkZ))
                return;
            _dirty.Remove((chunkX, chunkZ));
            FreeColumnNode(chunkX, chunkZ);
            MarkDirtyWithNeighbours(chunkX, chunkZ);
        }

        /// <summary>
        /// Drops columns the player has walked away from; the server re-sends them when needed.
        /// </summary>
        /// <param name="centerX">Player chunk X.</param>
        /// <param name="centerZ">Player chunk Z.</param>
        /// <param name="radius">Columns farther than this (in chunks) are removed.</param>
        public void ForgetFarColumns(int centerX, int centerZ, int radius)
        {
            foreach (var column in World.Columns.Where(c => System.Math.Max(System.Math.Abs(c.ChunkX - centerX), System.Math.Abs(c.ChunkZ - centerZ)) > radius).ToList())
                RemoveColumn(column.ChunkX, column.ChunkZ);
        }

        public bool HasColumnAt(Vector3 position) =>
            World.TryGetColumn(ClientWorld.ToChunkCoordinate(position.X), ClientWorld.ToChunkCoordinate(position.Z), out _);

        public void RebuildDirty(Vector3 near, int maxColumns)
        {
            var nearX = ClientWorld.ToChunkCoordinate(near.X);
            var nearZ = ClientWorld.ToChunkCoordinate(near.Z);
            var batch = _dirty
                .OrderBy(c => ((c.X - nearX) * (c.X - nearX)) + ((c.Z - nearZ) * (c.Z - nearZ)))
                .Take(maxColumns)
                .ToList();
            foreach (var key in batch)
            {
                _dirty.Remove(key);
                if (World.TryGetColumn(key.X, key.Z, out var column))
                    BuildColumn(column);
            }
        }

        private void MarkDirtyWithNeighbours(int chunkX, int chunkZ)
        {
            foreach (var (dx, dz) in new[] { (0, 0), (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                if (World.TryGetColumn(chunkX + dx, chunkZ + dz, out _))
                    _dirty.Add((chunkX + dx, chunkZ + dz));
            }
        }

        private void BuildColumn(ChunkColumn column)
        {
            FreeColumnNode(column.ChunkX, column.ChunkZ);
            var triangles = 0;
            var node = new Node3D
            {
                Name = $"Chunk_{column.ChunkX}_{column.ChunkZ}",
                Position = new Vector3(column.ChunkX * ChunkColumn.EdgeWidth, 0, column.ChunkZ * ChunkColumn.EdgeWidth),
            };
            for (var sectionY = 0; sectionY < ChunkColumn.SectionCount; sectionY++)
            {
                var mesh = ChunkMesher.BuildSection(World, column, sectionY, _models);
                if (mesh.IsEmpty)
                    continue;

                triangles += (mesh.Opaque.Indices.Count + mesh.Cutout.Indices.Count + mesh.Translucent.Indices.Count) / 3;

                var sectionNode = new Node3D { Position = new Vector3(0, sectionY * ChunkColumn.EdgeWidth, 0) };
                var arrayMesh = new ArrayMesh();
                AddSurface(arrayMesh, mesh.Opaque, _opaqueMaterial);
                AddSurface(arrayMesh, mesh.Cutout, _cutoutMaterial);
                AddSurface(arrayMesh, mesh.Translucent, _translucentMaterial);
                sectionNode.AddChild(new MeshInstance3D { Mesh = arrayMesh });

                if (mesh.CollisionTriangles.Count > 0)
                {
                    var shape = new ConcavePolygonShape3D { BackfaceCollision = true };
                    shape.SetFaces(mesh.CollisionTriangles.Select(ToGodot).ToArray());
                    var body = new StaticBody3D();
                    body.AddChild(new CollisionShape3D { Shape = shape });
                    sectionNode.AddChild(body);
                }

                node.AddChild(sectionNode);
            }

            _columnNodes[(column.ChunkX, column.ChunkZ)] = node;
            _triangles[(column.ChunkX, column.ChunkZ)] = triangles;
            TriangleCount = _triangles.Values.Sum();
            AddChild(node);
        }

        private void AddSurface(ArrayMesh target, MeshData data, Material material)
        {
            if (data.IsEmpty)
                return;

            // The mesher winds counter-clockwise; Godot treats clockwise triangles as front faces.
            var indices = data.Indices.ToArray();
            for (var i = 0; i < indices.Length; i += 3)
                (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);

            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = data.Positions.Select(ToGodot).ToArray();
            arrays[(int)Mesh.ArrayType.Normal] = data.Normals.Select(ToGodot).ToArray();
            arrays[(int)Mesh.ArrayType.TexUV] = data.Uvs.Select(uv => new Vector2(uv.X, uv.Y)).ToArray();
            arrays[(int)Mesh.ArrayType.Color] = data.Colors.Select(ToGodot).ToArray();
            arrays[(int)Mesh.ArrayType.Index] = indices;
            target.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            target.SurfaceSetMaterial(target.GetSurfaceCount() - 1, material);
        }

        private void FreeColumnNode(int chunkX, int chunkZ)
        {
            if (_columnNodes.Remove((chunkX, chunkZ), out var node))
                node.QueueFree();
            if (_triangles.Remove((chunkX, chunkZ)))
                TriangleCount = _triangles.Values.Sum();
        }


        private static Vector3 ToGodot(NVector3 v) => new Vector3(v.X, v.Y, v.Z);

        private static Color ToGodot(NVector4 c) => new Color(c.X, c.Y, c.Z, c.W);
    }
}
