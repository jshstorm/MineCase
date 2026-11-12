using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;

namespace MineCase.Client.Net.Rendering
{
    /// <summary>
    /// A model reference from a block state file: which model, how it is turned and whether textures stay world aligned.
    /// </summary>
    internal readonly record struct ModelReference(string Model, int X, int Y, bool UvLock, int Weight);

    /// <summary>
    /// Loads block model files (following "parent" links) and bakes them into quads, caching both.
    /// </summary>
    internal sealed class ModelLibrary
    {
        private const int MaxParentDepth = 32;
        private const float CoplanarOffset = 1f / 1024;

        private readonly ResourcePackStack _pack;
        private readonly Dictionary<string, RawModel> _raw = new Dictionary<string, RawModel>(StringComparer.Ordinal);
        private readonly Dictionary<ModelReference, BakedModel> _baked = new Dictionary<ModelReference, BakedModel>();

        public ModelLibrary(ResourcePackStack pack)
        {
            _pack = pack;
        }

        /// <summary>
        /// Gets the texture names the baked quads use, e.g. "minecraft:block/stone".
        /// </summary>
        public HashSet<string> Textures { get; } = new HashSet<string>(StringComparer.Ordinal);

        public HashSet<string> MissingModels { get; } = new HashSet<string>(StringComparer.Ordinal);

        public static string Normalize(string name) => name.Contains(':', StringComparison.Ordinal) ? name : "minecraft:" + name;

        public static string AssetPath(string kind, string name, string extension)
        {
            var normalized = Normalize(name);
            var separator = normalized.IndexOf(':', StringComparison.Ordinal);
            return $"assets/{normalized.Substring(0, separator)}/{kind}/{normalized.Substring(separator + 1)}{extension}";
        }

        public BakedModel Bake(ModelReference reference)
        {
            var key = reference with { Weight = 1 };
            if (!_baked.TryGetValue(key, out var model))
                _baked[key] = model = BakeUncached(key);
            return model;
        }

        /// <summary>
        /// Returns the "particle" texture of a model without elements (blocks the game draws as entities, like chests).
        /// </summary>
        /// <param name="model">Model name.</param>
        /// <returns>The texture name or null.</returns>
        public string GetParticleTexture(string model)
        {
            var chain = LoadChain(model);
            return chain == null ? null : ResolveTexture("#particle", MergeTextures(chain));
        }

        public BakedModel BakeBox(string texture, Vector3 from, Vector3 to)
        {
            Textures.Add(texture);
            var quads = Directions.All.Select(face =>
            {
                var uv = ModelGeometry.DefaultUv(face, from * 16, to * 16);
                var uvs = Enumerable.Range(0, 4).Select(i => ModelGeometry.CornerUv(uv, 0, i)).ToArray();
                Direction? cull = IsOnSide(face, from, to) ? face : null;
                return new BakedQuad(ModelGeometry.FaceCorners(face, from, to), uvs, cull, -1, true, texture, Vector4.One);
            }).ToList();
            return new BakedModel(quads, true);
        }

        private static bool IsOnSide(Direction face, Vector3 from, Vector3 to) => face switch
        {
            Direction.Down => from.Y == 0,
            Direction.Up => to.Y == 1,
            Direction.North => from.Z == 0,
            Direction.South => to.Z == 1,
            Direction.West => from.X == 0,
            _ => to.X == 1,
        };

        private static Dictionary<string, string> MergeTextures(List<RawModel> chain)
        {
            var textures = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = chain.Count - 1; i >= 0; i--)
            {
                foreach (var (name, value) in chain[i].Textures)
                    textures[name] = value;
            }

            return textures;
        }

        private static string ResolveTexture(string value, Dictionary<string, string> textures)
        {
            for (var depth = 0; value != null && value.StartsWith('#') && depth < MaxParentDepth; depth++)
                value = textures.TryGetValue(value.Substring(1), out var next) ? next : null;
            return value == null || value.StartsWith('#') ? null : Normalize(value);
        }

        private BakedModel BakeUncached(ModelReference reference)
        {
            var chain = LoadChain(reference.Model);
            if (chain == null)
                return BakedModel.Empty;

            var textures = MergeTextures(chain);
            var elements = chain.Select(m => m.Elements).FirstOrDefault(e => e != null) ?? new List<RawElement>();
            var ambientOcclusion = chain.Select(m => m.AmbientOcclusion).FirstOrDefault(a => a.HasValue) ?? true;
            var rotated = reference.X != 0 || reference.Y != 0;

            var quads = new List<BakedQuad>();
            foreach (var element in elements)
            {
                foreach (var (face, raw) in element.Faces)
                {
                    var texture = ResolveTexture(raw.Texture, textures) ?? TextureAtlas.MissingSprite;
                    if (texture != TextureAtlas.MissingSprite)
                        Textures.Add(texture);

                    var uv = raw.Uv ?? ModelGeometry.DefaultUv(face, element.From, element.To);
                    var uvs = Enumerable.Range(0, 4).Select(i => ModelGeometry.CornerUv(uv, raw.Rotation, i)).ToArray();
                    var positions = ModelGeometry.FaceCorners(face, element.From, element.To)
                        .Select(c => element.RotationAxis == '\0' ? c : ModelGeometry.RotateElement(c, element.RotationOrigin, element.RotationAxis, element.RotationAngle, element.Rescale))
                        .Select(c => ModelGeometry.RotateVariant(c / 16, reference.X, reference.Y))
                        .ToArray();
                    Direction? cull = raw.CullFace.HasValue ? ModelGeometry.RotateVariant(raw.CullFace.Value, reference.X, reference.Y) : null;

                    var quad = new BakedQuad(positions, uvs, cull, raw.TintIndex, element.Shade, texture, Vector4.One);
                    if (quad.IsDegenerate)
                        continue;
                    if (reference.UvLock && rotated && quad.IsAxisAligned)
                        quad = new BakedQuad(positions, positions.Select(p => ModelGeometry.LockedUv(quad.Facing, p)).ToArray(), cull, raw.TintIndex, element.Shade, texture, Vector4.One);

                    quads.Add(LiftIfCoplanar(quad, quads));
                }
            }

            return new BakedModel(quads, ambientOcclusion);
        }

        // An overlay drawn on top of another face of the same model (grass block sides) is moved out a little so it wins the depth test.
        private static BakedQuad LiftIfCoplanar(BakedQuad quad, List<BakedQuad> earlier)
        {
            if (!quad.IsAxisAligned)
                return quad;

            const float Epsilon = 1e-4f;
            var axis = quad.Facing switch
            {
                Direction.West or Direction.East => 0,
                Direction.Down or Direction.Up => 1,
                _ => 2,
            };
            var (min, max) = Bounds(quad);
            var overlaps = earlier.Count(other =>
            {
                if (!other.IsAxisAligned || other.Facing != quad.Facing)
                    return false;
                var (otherMin, otherMax) = Bounds(other);
                if (Math.Abs(min[axis] - otherMin[axis]) > Epsilon)
                    return false;
                var overlap = Vector3.Min(max, otherMax) - Vector3.Max(min, otherMin);
                return Enumerable.Range(0, 3).All(i => i == axis || overlap[i] > Epsilon);
            });
            if (overlaps == 0)
                return quad;

            var lift = quad.Normal * (CoplanarOffset * overlaps);
            return new BakedQuad(quad.Positions.Select(p => p + lift).ToArray(), quad.Uvs, quad.CullFace, quad.TintIndex, quad.Shade, quad.Texture, quad.Color);
        }

        private static (Vector3 Min, Vector3 Max) Bounds(BakedQuad quad)
        {
            var min = quad.Positions.Aggregate(Vector3.Min);
            var max = quad.Positions.Aggregate(Vector3.Max);
            return (min, max);
        }

        // Model first, then its parents; null when the model itself is missing.
        private List<RawModel> LoadChain(string name)
        {
            var chain = new List<RawModel>();
            var current = name;
            while (current != null && chain.Count < MaxParentDepth)
            {
                var normalized = Normalize(current);
                if (normalized.StartsWith("minecraft:builtin/", StringComparison.Ordinal))
                    break;
                var model = LoadRaw(normalized);
                if (model == null)
                {
                    if (chain.Count == 0)
                        return null;
                    break;
                }

                chain.Add(model);
                current = model.Parent;
            }

            return chain;
        }

        private RawModel LoadRaw(string normalized)
        {
            if (_raw.TryGetValue(normalized, out var cached))
                return cached;

            RawModel model = null;
            if (_pack.TryReadText(AssetPath("models", normalized, ".json"), out var json))
            {
                using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                model = RawModel.Parse(document.RootElement);
            }
            else
            {
                MissingModels.Add(normalized);
            }

            _raw[normalized] = model;
            return model;
        }

        private sealed class RawModel
        {
            public string Parent { get; private set; }

            public Dictionary<string, string> Textures { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

            public List<RawElement> Elements { get; private set; }

            public bool? AmbientOcclusion { get; private set; }

            public static RawModel Parse(JsonElement root)
            {
                var model = new RawModel();
                if (root.TryGetProperty("parent", out var parent) && parent.ValueKind == JsonValueKind.String)
                    model.Parent = parent.GetString();
                if (root.TryGetProperty("ambientocclusion", out var ao) && ao.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    model.AmbientOcclusion = ao.GetBoolean();
                if (root.TryGetProperty("textures", out var textures) && textures.ValueKind == JsonValueKind.Object)
                {
                    foreach (var texture in textures.EnumerateObject())
                    {
                        if (texture.Value.ValueKind == JsonValueKind.String)
                            model.Textures[texture.Name] = texture.Value.GetString();
                    }
                }

                if (root.TryGetProperty("elements", out var elements) && elements.ValueKind == JsonValueKind.Array)
                    model.Elements = elements.EnumerateArray().Select(RawElement.Parse).ToList();
                return model;
            }
        }

        private sealed class RawElement
        {
            public Vector3 From { get; private set; }

            public Vector3 To { get; private set; }

            public char RotationAxis { get; private set; }

            public Vector3 RotationOrigin { get; private set; }

            public float RotationAngle { get; private set; }

            public bool Rescale { get; private set; }

            public bool Shade { get; private set; } = true;

            public List<(Direction Face, RawFace Raw)> Faces { get; } = new List<(Direction, RawFace)>();

            public static RawElement Parse(JsonElement json)
            {
                var element = new RawElement
                {
                    From = ReadVector(json, "from"),
                    To = ReadVector(json, "to"),
                };
                if (json.TryGetProperty("shade", out var shade) && shade.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    element.Shade = shade.GetBoolean();
                if (json.TryGetProperty("rotation", out var rotation) && rotation.ValueKind == JsonValueKind.Object)
                {
                    element.RotationOrigin = ReadVector(rotation, "origin", new Vector3(8));
                    element.RotationAxis = rotation.TryGetProperty("axis", out var axis) && axis.GetString() is { Length: 1 } a ? a[0] : 'y';
                    element.RotationAngle = rotation.TryGetProperty("angle", out var angle) ? angle.GetSingle() : 0;
                    element.Rescale = rotation.TryGetProperty("rescale", out var rescale) && rescale.ValueKind == JsonValueKind.True;
                }

                if (json.TryGetProperty("faces", out var faces) && faces.ValueKind == JsonValueKind.Object)
                {
                    foreach (var face in faces.EnumerateObject())
                    {
                        if (Directions.TryParse(face.Name, out var direction))
                            element.Faces.Add((direction, RawFace.Parse(face.Value)));
                    }
                }

                return element;
            }

            private static Vector3 ReadVector(JsonElement json, string name, Vector3 fallback = default)
            {
                if (!json.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array || array.GetArrayLength() < 3)
                    return fallback;
                return new Vector3(array[0].GetSingle(), array[1].GetSingle(), array[2].GetSingle());
            }
        }

        private sealed class RawFace
        {
            public Vector4? Uv { get; private set; }

            public string Texture { get; private set; }

            public Direction? CullFace { get; private set; }

            public int Rotation { get; private set; }

            public int TintIndex { get; private set; } = -1;

            public static RawFace Parse(JsonElement json)
            {
                var face = new RawFace
                {
                    Texture = json.TryGetProperty("texture", out var texture) ? texture.GetString() : null,
                };
                if (json.TryGetProperty("uv", out var uv) && uv.ValueKind == JsonValueKind.Array && uv.GetArrayLength() >= 4)
                    face.Uv = new Vector4(uv[0].GetSingle(), uv[1].GetSingle(), uv[2].GetSingle(), uv[3].GetSingle());
                if (json.TryGetProperty("cullface", out var cull) && cull.ValueKind == JsonValueKind.String && Directions.TryParse(cull.GetString(), out var direction))
                    face.CullFace = direction;
                if (json.TryGetProperty("rotation", out var rotation) && rotation.ValueKind == JsonValueKind.Number)
                    face.Rotation = rotation.GetInt32();
                if (json.TryGetProperty("tintindex", out var tint) && tint.ValueKind == JsonValueKind.Number)
                    face.TintIndex = tint.GetInt32();
                return face;
            }
        }
    }
}
