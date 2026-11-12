using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MineCase.Client.Net.Rendering
{
    public enum RenderLayer
    {
        Opaque,
        Cutout,
        Translucent,
    }

    /// <summary>
    /// Where a texture sits in the atlas (U0, V0, U1, V1) and how it has to be blended.
    /// </summary>
    public readonly struct AtlasSprite
    {
        public AtlasSprite(Vector4 rect, RenderLayer layer)
        {
            Rect = rect;
            Layer = layer;
        }

        public Vector4 Rect { get; }

        public RenderLayer Layer { get; }

        public Vector2 Map(Vector2 local) =>
            new Vector2(Rect.X + ((Rect.Z - Rect.X) * local.X), Rect.Y + ((Rect.W - Rect.Y) * local.Y));
    }

    /// <summary>
    /// All block textures in one RGBA image: equal power-of-two cells on a power-of-two grid so mipmaps stay inside each cell.
    /// Animated textures keep their first frame.
    /// </summary>
    public sealed class TextureAtlas
    {
        public const string WhiteSprite = "#white";
        public const string MissingSprite = "#missing";

        // Keeps nearest sampling at a cell edge from picking up the neighbouring cell.
        private const float EdgeInset = 1f / 64;

        private readonly Dictionary<string, AtlasSprite> _sprites;

        private TextureAtlas(int width, int height, byte[] pixels, Dictionary<string, AtlasSprite> sprites)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
            _sprites = sprites;
        }

        public int Width { get; }

        public int Height { get; }

        public byte[] Pixels { get; }

        public int SpriteCount => _sprites.Count;

        public AtlasSprite White => _sprites[WhiteSprite];

        public AtlasSprite Missing => _sprites[MissingSprite];

        public bool TryGetSprite(string name, out AtlasSprite sprite) => _sprites.TryGetValue(name, out sprite);

        public AtlasSprite GetSprite(string name) => _sprites.TryGetValue(name, out var sprite) ? sprite : Missing;

        public static TextureAtlas Build(IReadOnlyDictionary<string, RgbaImage> textures)
        {
            var frames = textures.ToDictionary(t => t.Key, t => FirstFrame(t.Value), StringComparer.Ordinal);
            frames[WhiteSprite] = RgbaImage.Solid(16, 16, 255, 255, 255, 255);
            frames[MissingSprite] = Checkerboard();

            var cell = NextPowerOfTwo(frames.Values.Max(f => Math.Max(f.Width, f.Height)));
            var columns = NextPowerOfTwo((int)Math.Ceiling(Math.Sqrt(frames.Count)));
            var rows = NextPowerOfTwo((frames.Count + columns - 1) / columns);
            var width = columns * cell;
            var height = rows * cell;
            var pixels = new byte[width * height * 4];
            var sprites = new Dictionary<string, AtlasSprite>(StringComparer.Ordinal);

            var slot = 0;
            foreach (var (name, frame) in frames.OrderBy(f => f.Key, StringComparer.Ordinal))
            {
                var cellX = (slot % columns) * cell;
                var cellY = (slot / columns) * cell;
                slot++;
                for (var y = 0; y < cell; y++)
                {
                    for (var x = 0; x < cell; x++)
                    {
                        var source = frame.OffsetOf(x * frame.Width / cell, y * frame.Height / cell);
                        Array.Copy(frame.Pixels, source, pixels, (((cellY + y) * width) + cellX + x) * 4, 4);
                    }
                }

                var rect = new Vector4((cellX + EdgeInset) / width, (cellY + EdgeInset) / height, (cellX + cell - EdgeInset) / width, (cellY + cell - EdgeInset) / height);
                sprites[name] = new AtlasSprite(rect, Classify(frame));
            }

            return new TextureAtlas(width, height, pixels, sprites);
        }

        /// <summary>
        /// Fully opaque textures are drawn opaque, ones with only on/off alpha are alpha tested, and ones with partial alpha are blended.
        /// </summary>
        /// <param name="image">Texture frame.</param>
        /// <returns>The layer the texture needs.</returns>
        public static RenderLayer Classify(RgbaImage image)
        {
            var cleared = 0;
            var partial = 0;
            for (var i = 3; i < image.Pixels.Length; i += 4)
            {
                var alpha = image.Pixels[i];
                if (alpha == 0)
                    cleared++;
                else if (alpha < 255)
                    partial++;
            }

            if (partial > 0)
                return RenderLayer.Translucent;
            return cleared > 0 ? RenderLayer.Cutout : RenderLayer.Opaque;
        }

        private static RgbaImage FirstFrame(RgbaImage image)
        {
            if (image.Height <= image.Width || image.Height % image.Width != 0)
                return image;
            var pixels = new byte[image.Width * image.Width * 4];
            Array.Copy(image.Pixels, pixels, pixels.Length);
            return new RgbaImage(image.Width, image.Width, pixels);
        }

        private static RgbaImage Checkerboard()
        {
            var image = RgbaImage.Solid(16, 16, 0, 0, 0, 255);
            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    if ((x < 8) == (y < 8))
                    {
                        var o = image.OffsetOf(x, y);
                        image.Pixels[o] = 248;
                        image.Pixels[o + 2] = 248;
                    }
                }
            }

            return image;
        }

        private static int NextPowerOfTwo(int value)
        {
            var result = 1;
            while (result < value)
                result <<= 1;
            return result;
        }
    }
}
