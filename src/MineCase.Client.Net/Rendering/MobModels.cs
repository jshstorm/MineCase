using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MineCase.Client.Net.Rendering
{
    public enum PartMotion
    {
        None,
        Head,
        Leg,
        Arm,
        Wing,
        SpiderLeg,
    }

    /// <summary>
    /// One animated piece of a mob, ready to draw: geometry around its pivot (in blocks), the pivot itself and its rest pose.
    /// </summary>
    public sealed class MobMeshPart
    {
        public MobMeshPart(Vector3 pivot, Vector3 restRotation, PartMotion motion, int phase, MeshData mesh)
        {
            Pivot = pivot;
            RestRotation = restRotation;
            Motion = motion;
            Phase = phase;
            Mesh = mesh;
        }

        public Vector3 Pivot { get; }

        /// <summary>
        /// Gets the rest pose in degrees (Godot's YXZ order: Z first, then X, then Y).
        /// </summary>
        public Vector3 RestRotation { get; }

        public PartMotion Motion { get; }

        /// <summary>
        /// Gets 0 or 1: parts with different phases swing in opposite directions.
        /// </summary>
        public int Phase { get; }

        public MeshData Mesh { get; }
    }

    /// <summary>
    /// A built mob: its own small texture and the parts that use it. The front of the model faces +Z.
    /// </summary>
    public sealed class MobMesh
    {
        public MobMesh(string name, RgbaImage texture, IReadOnlyList<MobMeshPart> parts)
        {
            Name = name;
            Texture = texture;
            Parts = parts;
        }

        public string Name { get; }

        public RgbaImage Texture { get; }

        public IReadOnlyList<MobMeshPart> Parts { get; }
    }

    /// <summary>
    /// Blocky mob models designed for this client (sizes in 1/16 block, feet at y = 0, facing +Z) with
    /// procedurally painted pixel textures. Entity types without a design become a plain box of their size.
    /// </summary>
    public static class MobModels
    {
        private static readonly Dictionary<string, Func<MobDesign>> Designs = new Dictionary<string, Func<MobDesign>>(StringComparer.Ordinal)
        {
            ["sheep"] = Sheep,
            ["cow"] = Cow,
            ["pig"] = Pig,
            ["chicken"] = Chicken,
            ["zombie"] = () => Humanoid("zombie", Rgb(0x6E8B4E), Rgb(0x5A4A3A), Rgb(0x3B4252), null, -80, zombieFace: true),
            ["skeleton"] = Skeleton,
            ["player"] = () => Humanoid("player", Rgb(0xD9A982), Rgb(0xC89B3C), Rgb(0x4E3B2A), Rgb(0x3A2716), 0, zombieFace: false),
            ["spider"] = Spider,
        };

        public static bool HasDesign(string entityName) => Designs.ContainsKey(entityName);

        public static MobMesh Build(EntityTypeInfo type)
        {
            var design = Designs.TryGetValue(type.Name, out var create) ? create() : Fallback(type);
            return design.Build();
        }

        private static MobDesign Sheep()
        {
            var wool = Skin.Noise(Rgb(0xF2EEE6), Rgb(0xE6E1D6), Rgb(0xDAD4C8), Rgb(0xFFFFFF));
            var face = Skin.Noise(Rgb(0xD8C2A8), Rgb(0xCDB59A));
            var faceFront = face.WithDecals((1, 2, 1, 1, Rgb(0x2B2420)), (5, 2, 1, 1, Rgb(0x2B2420)), (3, 4, 1, 1, Rgb(0xB88F80)));
            var leg = Skin.Noise(Rgb(0xD8C2A8), Rgb(0xCDB59A)).WithBottomBand(1, Rgb(0x6B5848));
            var design = new MobDesign("sheep");
            design.Part(Vector3.Zero).Box(new Vector3(-6, 7, -9), new Vector3(6, 17, 8), wool);
            design.Part(new Vector3(0, 15, 8), PartMotion.Head)
                .Box(new Vector3(-3.5f, -4, 0), new Vector3(3.5f, 3, 6), face, faceFront)
                .Box(new Vector3(-4, 1, 0.5f), new Vector3(4, 4, 4.5f), wool)
                .Box(new Vector3(-5, -0.5f, 1.5f), new Vector3(-3.5f, 0.5f, 3.5f), face)
                .Box(new Vector3(3.5f, -0.5f, 1.5f), new Vector3(5, 0.5f, 3.5f), face);
            Legs(design, 3.5f, 5.5f, 8, 3, leg);
            return design;
        }

        private static MobDesign Cow()
        {
            var hide = Skin.Patches(Rgb(0xF4F1EA), Rgb(0x1E1B1A), 0.45f);
            var head = Skin.Patches(Rgb(0xF4F1EA), Rgb(0x1E1B1A), 0.5f);
            var muzzle = Rgb(0xE3A7A0);
            var headFront = head.WithDecals((1, 3, 1, 1, Rgb(0x111111)), (6, 3, 1, 1, Rgb(0x111111)), (2, 5, 4, 3, muzzle), (2, 6, 1, 1, Rgb(0x8C5550)), (5, 6, 1, 1, Rgb(0x8C5550)));
            var horn = Skin.Noise(Rgb(0xE8E0C8), Rgb(0xD8CDB0));
            var leg = hide.WithBottomBand(2, Rgb(0x3A2E26));
            var design = new MobDesign("cow");
            design.Part(Vector3.Zero)
                .Box(new Vector3(-6, 10, -9), new Vector3(6, 20, 9), hide)
                .Box(new Vector3(-2, 8.5f, -5), new Vector3(2, 10, -1), Skin.Noise(muzzle, Rgb(0xD9988F)));
            design.Part(new Vector3(0, 19, 9), PartMotion.Head)
                .Box(new Vector3(-4, -6, 0), new Vector3(4, 2, 6), head, headFront)
                .Box(new Vector3(-5.5f, 0, 2), new Vector3(-4, 3, 3), horn)
                .Box(new Vector3(4, 0, 2), new Vector3(5.5f, 3, 3), horn);
            Legs(design, 4, 6.5f, 10, 4, leg);
            return design;
        }

        private static MobDesign Pig()
        {
            var skin = Skin.Noise(Rgb(0xF0A7A4), Rgb(0xE89A98), Rgb(0xF5B3AF));
            var front = skin.WithDecals((1, 2, 1, 1, Rgb(0x2A1A1A)), (6, 2, 1, 1, Rgb(0x2A1A1A)));
            var snout = Skin.Noise(Rgb(0xE08B8A)).WithDecals((1, 1, 1, 1, Rgb(0x8A4A4A)), (3, 1, 1, 1, Rgb(0x8A4A4A)));
            var design = new MobDesign("pig");
            design.Part(Vector3.Zero).Box(new Vector3(-5, 6, -8), new Vector3(5, 14, 8), skin);
            design.Part(new Vector3(0, 12, 8), PartMotion.Head)
                .Box(new Vector3(-4, -4, 0), new Vector3(4, 3, 6), skin, front)
                .Box(new Vector3(-2.5f, -3, 6), new Vector3(2.5f, 0, 7), snout, snout)
                .Box(new Vector3(-4, 3, 1), new Vector3(-2, 4, 3), skin)
                .Box(new Vector3(2, 3, 1), new Vector3(4, 4, 3), skin);
            Legs(design, 3, 5, 6, 3, skin.WithBottomBand(1, Rgb(0xB06A6A)));
            return design;
        }

        private static MobDesign Chicken()
        {
            var feathers = Skin.Noise(Rgb(0xFAFAF5), Rgb(0xEDEDE6), Rgb(0xE2E2DA));
            var head = feathers.WithDecals((0, 1, 1, 1, Rgb(0x111111)), (3, 1, 1, 1, Rgb(0x111111)));
            var beak = Skin.Noise(Rgb(0xF2B233), Rgb(0xE3A12A));
            var wattle = Skin.Noise(Rgb(0xD8352A));
            var design = new MobDesign("chicken");
            design.Part(Vector3.Zero)
                .Box(new Vector3(-3, 4, -4), new Vector3(3, 10, 3), feathers)
                .Box(new Vector3(-2, 8, -5.5f), new Vector3(2, 11, -4), feathers);
            design.Part(new Vector3(0, 9, 2), PartMotion.Head)
                .Box(new Vector3(-2, 0, -1), new Vector3(2, 5, 2), feathers, head)
                .Box(new Vector3(-1.5f, 2, 2), new Vector3(1.5f, 3.5f, 4), beak)
                .Box(new Vector3(-1, 0.5f, 2), new Vector3(1, 2, 3), wattle);
            design.Part(new Vector3(3, 9, 0), PartMotion.Wing, 0).Box(new Vector3(0, -4, -3), new Vector3(1, 0, 3), feathers);
            design.Part(new Vector3(-3, 9, 0), PartMotion.Wing, 1).Box(new Vector3(-1, -4, -3), new Vector3(0, 0, 3), feathers);
            foreach (var (x, phase) in new[] { (1.5f, 0), (-1.5f, 1) })
            {
                design.Part(new Vector3(x, 4, 0), PartMotion.Leg, phase)
                    .Box(new Vector3(-0.5f, -4, -0.5f), new Vector3(0.5f, 0, 0.5f), beak)
                    .Box(new Vector3(-1, -4, -1), new Vector3(1, -3.5f, 1.5f), beak);
            }

            return design;
        }

        private static MobDesign Skeleton()
        {
            var bone = Skin.Noise(Rgb(0xDCDCD4), Rgb(0xCFCFC6), Rgb(0xC2C2B8));
            var dark = Rgb(0x2B2B2B);
            var skull = bone.WithDecals((1, 2, 2, 2, dark), (4, 2, 2, 2, dark), (3, 4, 1, 1, Rgb(0x6B6B66)), (1, 5, 5, 1, Rgb(0x8A8A84)));
            var ribs = bone.WithDecals((0, 2, 6, 1, Rgb(0x8A8A84)), (0, 4, 6, 1, Rgb(0x8A8A84)), (0, 6, 6, 1, Rgb(0x8A8A84)));
            var design = new MobDesign("skeleton");
            design.Part(Vector3.Zero).Box(new Vector3(-3, 12, -1.5f), new Vector3(3, 23, 1.5f), bone, ribs);
            design.Part(new Vector3(0, 23, 0), PartMotion.Head).Box(new Vector3(-3.5f, 0, -3.5f), new Vector3(3.5f, 7, 3.5f), bone, skull);
            foreach (var (x, phase) in new[] { (4.5f, 0), (-4.5f, 1) })
                design.Part(new Vector3(x, 22, 0), PartMotion.Arm, phase).Box(new Vector3(-1, -11, -1), new Vector3(1, 0, 1), bone);
            foreach (var (x, phase) in new[] { (1.5f, 1), (-1.5f, 0) })
                design.Part(new Vector3(x, 12, 0), PartMotion.Leg, phase).Box(new Vector3(-1, -12, -1), new Vector3(1, 0, 1), bone);
            return design;
        }

        private static MobDesign Humanoid(string name, Vector4 skinColor, Vector4 shirt, Vector4 pants, Vector4? hair, float armRest, bool zombieFace)
        {
            var skin = Skin.Noise(skinColor, Shade(skinColor, 0.93f), Shade(skinColor, 1.05f));
            var eye = zombieFace ? Rgb(0x1C2418) : Rgb(0x3B5BA0);
            var mouth = Shade(skinColor, 0.7f);
            var face = skin.WithDecals((1, 3, 2, 1, eye), (4, 3, 2, 1, eye), (2, 5, 3, 1, mouth));
            var cloth = Skin.Noise(shirt, Shade(shirt, 0.9f), Shade(shirt, 1.08f));
            var trousers = Skin.Noise(pants, Shade(pants, 0.9f)).WithBottomBand(2, Shade(pants, 0.6f));
            var arm = skin.WithTopBand(4, shirt);

            var design = new MobDesign(name);
            design.Part(Vector3.Zero).Box(new Vector3(-4, 12, -2.5f), new Vector3(4, 23, 2.5f), cloth);
            var head = design.Part(new Vector3(0, 23, 0), PartMotion.Head).Box(new Vector3(-3.5f, 0, -3.5f), new Vector3(3.5f, 7, 3.5f), skin, face);
            if (hair is Vector4 hairColor)
            {
                var hairSkin = Skin.Noise(hairColor, Shade(hairColor, 0.85f));
                head.Box(new Vector3(-3.75f, 5.5f, -3.75f), new Vector3(3.75f, 7.5f, 3.75f), hairSkin)
                    .Box(new Vector3(-3.75f, 2, -3.75f), new Vector3(3.75f, 5.5f, -3), hairSkin);
            }

            foreach (var (x, phase) in new[] { (5.5f, 0), (-5.5f, 1) })
                design.Part(new Vector3(x, 22.5f, 0), PartMotion.Arm, phase, new Vector3(armRest, 0, 0)).Box(new Vector3(-1.5f, -11, -1.5f), new Vector3(1.5f, 0.5f, 1.5f), arm);
            foreach (var (x, phase) in new[] { (2, 1), (-2, 0) })
                design.Part(new Vector3(x, 12, 0), PartMotion.Leg, phase).Box(new Vector3(-1.75f, -12, -1.75f), new Vector3(1.75f, 0, 1.75f), trousers);
            return design;
        }

        private static MobDesign Spider()
        {
            var shell = Skin.Noise(Rgb(0x2E2622), Rgb(0x3A302A), Rgb(0x241E1B));
            var back = Skin.Patches(Rgb(0x2E2622), Rgb(0x5A4636), 0.3f);
            var eyes = shell.WithDecals((1, 1, 1, 1, Rgb(0xD62A2A)), (5, 1, 1, 1, Rgb(0xD62A2A)), (2, 2, 1, 1, Rgb(0xB01E1E)), (4, 2, 1, 1, Rgb(0xB01E1E)), (3, 3, 1, 1, Rgb(0xE84444)));
            var design = new MobDesign("spider");
            design.Part(Vector3.Zero)
                .Box(new Vector3(-5, 4, -14), new Vector3(5, 12, -2), back)
                .Box(new Vector3(-3.5f, 4, -2), new Vector3(3.5f, 9, 4), shell);
            design.Part(new Vector3(0, 7, 4), PartMotion.Head).Box(new Vector3(-3.5f, -3, 0), new Vector3(3.5f, 3, 6), shell, eyes);
            var spread = new[] { 35f, 12f, -12f, -35f };
            var z = new[] { 3f, 1f, -1f, -3f };
            for (var i = 0; i < 4; i++)
            {
                // Right legs point along +X, left ones along -X; tilt down first, then fan out.
                design.Part(new Vector3(3.5f, 7, z[i]), PartMotion.SpiderLeg, i % 2, new Vector3(0, -spread[i], -25))
                    .Box(new Vector3(0, -0.75f, -0.75f), new Vector3(13, 0.75f, 0.75f), shell);
                design.Part(new Vector3(-3.5f, 7, z[i]), PartMotion.SpiderLeg, (i + 1) % 2, new Vector3(0, spread[i], 25))
                    .Box(new Vector3(-13, -0.75f, -0.75f), new Vector3(0, 0.75f, 0.75f), shell);
            }

            return design;
        }

        // A box the size of the entity's hit box, coloured from its name so different kinds can be told apart.
        private static MobDesign Fallback(EntityTypeInfo type)
        {
            var hash = (uint)type.Name.Aggregate(17, (h, c) => (h * 31) + c);
            var color = new Vector4(0.35f + ((hash & 0xFF) / 255f * 0.5f), 0.35f + (((hash >> 8) & 0xFF) / 255f * 0.5f), 0.35f + (((hash >> 16) & 0xFF) / 255f * 0.5f), 1);
            var half = Math.Max(1, type.Width * 8);
            var height = Math.Max(1, type.Height * 16);
            var design = new MobDesign(type.Name);
            design.Part(Vector3.Zero).Box(new Vector3(-half, 0, -half), new Vector3(half, height, half), Skin.Noise(color, Shade(color, 0.9f), Shade(color, 1.08f)));
            return design;
        }

        private static void Legs(MobDesign design, float x, float z, float height, float thickness, Skin skin)
        {
            var h = thickness / 2;
            foreach (var (px, pz, phase) in new[] { (x, z, 0), (-x, z, 1), (x, -z, 1), (-x, -z, 0) })
            {
                design.Part(new Vector3(px, height, pz), PartMotion.Leg, phase)
                    .Box(new Vector3(-h, -height, -h), new Vector3(h, 0, h), skin);
            }
        }

        private static Vector4 Rgb(int rgb) => new Vector4(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1);

        private static Vector4 Shade(Vector4 color, float factor) =>
            new Vector4(Math.Min(1, color.X * factor), Math.Min(1, color.Y * factor), Math.Min(1, color.Z * factor), color.W);

        /// <summary>
        /// How a box face is painted: a few shades scattered per pixel or two colours in blotches, plus optional bands and pixel decals.
        /// </summary>
        private sealed class Skin
        {
            private Vector4[] _palette;
            private Vector4 _patchColor;
            private float _patchAmount;
            private (int Rows, Vector4 Color)? _bottomBand;
            private (int Rows, Vector4 Color)? _topBand;
            private (int X, int Y, int W, int H, Vector4 Color)[] _decals = Array.Empty<(int, int, int, int, Vector4)>();

            public static Skin Noise(params Vector4[] palette) => new Skin { _palette = palette };

            public static Skin Patches(Vector4 baseColor, Vector4 patchColor, float amount) =>
                new Skin { _palette = new[] { baseColor, Shade(baseColor, 0.95f) }, _patchColor = patchColor, _patchAmount = amount };

            public Skin WithDecals(params (int X, int Y, int W, int H, Vector4 Color)[] decals)
            {
                var copy = (Skin)MemberwiseClone();
                copy._decals = decals;
                return copy;
            }

            public Skin WithBottomBand(int rows, Vector4 color)
            {
                var copy = (Skin)MemberwiseClone();
                copy._bottomBand = (rows, color);
                return copy;
            }

            public Skin WithTopBand(int rows, Vector4 color)
            {
                var copy = (Skin)MemberwiseClone();
                copy._topBand = (rows, color);
                return copy;
            }

            public void Paint(RgbaImage image, int x0, int y0, int width, int height, bool isSide, int seed)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var color = _palette[(int)(Hash(x, y, seed) % (uint)_palette.Length)];
                        if (_patchAmount > 0 && Blotch(x, y, seed) < _patchAmount)
                            color = _patchColor;
                        if (isSide && _bottomBand is var (bottomRows, bottomColor) && y >= height - bottomRows)
                            color = bottomColor;
                        if (isSide && _topBand is var (topRows, topColor) && y < topRows)
                            color = topColor;
                        Set(image, x0 + x, y0 + y, color);
                    }
                }
            }

            public void PaintDecals(RgbaImage image, int x0, int y0, int width, int height)
            {
                foreach (var (x, y, w, h, color) in _decals)
                {
                    for (var dy = 0; dy < h; dy++)
                    {
                        for (var dx = 0; dx < w; dx++)
                        {
                            if (x + dx < width && y + dy < height)
                                Set(image, x0 + x + dx, y0 + y + dy, color);
                        }
                    }
                }
            }

            private static uint Hash(int x, int y, int seed)
            {
                var h = (uint)((x * 374761393) + (y * 668265263) + (seed * 2246822519u));
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }

            // Smooth noise on a 3-pixel grid so patches come out as blobs rather than speckles.
            private static float Blotch(int x, int y, int seed)
            {
                const int Cell = 3;
                int cx = x / Cell, cy = y / Cell;
                float fx = (x % Cell) / (float)Cell, fy = (y % Cell) / (float)Cell;
                float Corner(int i, int j) => (Hash(cx + i, cy + j, seed + 7) & 0xFFFF) / 65535f;
                var top = Corner(0, 0) + ((Corner(1, 0) - Corner(0, 0)) * fx);
                var bottom = Corner(0, 1) + ((Corner(1, 1) - Corner(0, 1)) * fx);
                return top + ((bottom - top) * fy);
            }

            private static void Set(RgbaImage image, int x, int y, Vector4 color)
            {
                var o = image.OffsetOf(x, y);
                image.Pixels[o] = (byte)(color.X * 255);
                image.Pixels[o + 1] = (byte)(color.Y * 255);
                image.Pixels[o + 2] = (byte)(color.Z * 255);
                image.Pixels[o + 3] = 255;
            }
        }

        private sealed class MobDesign
        {
            private const int AtlasWidth = 64;

            private readonly List<PartDesign> _parts = new List<PartDesign>();

            public MobDesign(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public PartDesign Part(Vector3 pivot, PartMotion motion = PartMotion.None, int phase = 0, Vector3 restRotation = default)
            {
                var part = new PartDesign(pivot, motion, phase, restRotation);
                _parts.Add(part);
                return part;
            }

            public MobMesh Build()
            {
                // Every face gets its own texels at one texel per model pixel, packed in rows.
                var faces = new List<(PartDesign Part, BoxDesign Box, Direction Face, int W, int H, int X, int Y)>();
                var cursorX = 0;
                var cursorY = 0;
                var rowHeight = 0;
                foreach (var part in _parts)
                {
                    foreach (var box in part.Boxes)
                    {
                        foreach (var face in Directions.All)
                        {
                            var (w, h) = FaceSize(face, box.To - box.From);
                            if (cursorX + w > AtlasWidth)
                            {
                                cursorX = 0;
                                cursorY += rowHeight;
                                rowHeight = 0;
                            }

                            faces.Add((part, box, face, w, h, cursorX, cursorY));
                            cursorX += w;
                            rowHeight = Math.Max(rowHeight, h);
                        }
                    }
                }

                var texture = new RgbaImage(AtlasWidth, cursorY + rowHeight, new byte[AtlasWidth * (cursorY + rowHeight) * 4]);
                var seed = 1;
                foreach (var (_, box, face, w, h, x, y) in faces)
                {
                    var skin = face == Direction.South ? box.Front : box.Skin;
                    skin.Paint(texture, x, y, w, h, face is not (Direction.Up or Direction.Down), seed++);
                    if (face == Direction.South)
                        skin.PaintDecals(texture, x, y, w, h);
                }

                var meshes = _parts.ToDictionary(p => p, _ => new MeshData());
                var colors = new Vector4[4];
                var uvs = new Vector2[4];
                foreach (var (part, box, face, w, h, x, y) in faces)
                {
                    var corners = ModelGeometry.FaceCorners(face, box.From / 16, box.To / 16);
                    uvs[0] = new Vector2(x, y) / new Vector2(texture.Width, texture.Height);
                    uvs[1] = new Vector2(x, y + h) / new Vector2(texture.Width, texture.Height);
                    uvs[2] = new Vector2(x + w, y + h) / new Vector2(texture.Width, texture.Height);
                    uvs[3] = new Vector2(x + w, y) / new Vector2(texture.Width, texture.Height);
                    var shade = face switch
                    {
                        Direction.Up => 1f,
                        Direction.Down => 0.55f,
                        Direction.North or Direction.South => 0.85f,
                        _ => 0.7f,
                    };
                    for (var i = 0; i < 4; i++)
                        colors[i] = new Vector4(shade, shade, shade, 1);
                    meshes[part].AddQuad(Vector3.Zero, corners, uvs, face.Normal(), colors, false);
                }

                var parts = _parts.Select(p => new MobMeshPart(p.Pivot / 16, p.RestRotation, p.Motion, p.Phase, meshes[p])).ToList();
                return new MobMesh(Name, texture, parts);
            }

            // Texels along the face's horizontal and vertical texture directions, at least one each.
            private static (int W, int H) FaceSize(Direction face, Vector3 size)
            {
                var (u, v) = face switch
                {
                    Direction.Up or Direction.Down => (size.X, size.Z),
                    Direction.North or Direction.South => (size.X, size.Y),
                    _ => (size.Z, size.Y),
                };
                return (Math.Max(1, (int)Math.Ceiling(u)), Math.Max(1, (int)Math.Ceiling(v)));
            }
        }

        private sealed class PartDesign
        {
            public PartDesign(Vector3 pivot, PartMotion motion, int phase, Vector3 restRotation)
            {
                Pivot = pivot;
                Motion = motion;
                Phase = phase;
                RestRotation = restRotation;
            }

            public Vector3 Pivot { get; }

            public PartMotion Motion { get; }

            public int Phase { get; }

            public Vector3 RestRotation { get; }

            public List<BoxDesign> Boxes { get; } = new List<BoxDesign>();

            public PartDesign Box(Vector3 from, Vector3 to, Skin skin, Skin front = null)
            {
                Boxes.Add(new BoxDesign(from, to, skin, front ?? skin));
                return this;
            }
        }

        private sealed record BoxDesign(Vector3 From, Vector3 To, Skin Skin, Skin Front);
    }
}
