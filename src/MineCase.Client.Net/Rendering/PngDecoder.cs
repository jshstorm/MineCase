using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

namespace MineCase.Client.Net.Rendering
{
    /// <summary>
    /// 8-bit RGBA pixels, rows from the top.
    /// </summary>
    public sealed class RgbaImage
    {
        public RgbaImage(int width, int height, byte[] pixels)
        {
            if (pixels.Length != width * height * 4)
                throw new ArgumentException("Pixel data does not match the size.", nameof(pixels));
            Width = width;
            Height = height;
            Pixels = pixels;
        }

        public int Width { get; }

        public int Height { get; }

        public byte[] Pixels { get; }

        public static RgbaImage Solid(int width, int height, byte r, byte g, byte b, byte a)
        {
            var pixels = new byte[width * height * 4];
            for (var i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = r;
                pixels[i + 1] = g;
                pixels[i + 2] = b;
                pixels[i + 3] = a;
            }

            return new RgbaImage(width, height, pixels);
        }

        public int OffsetOf(int x, int y) => ((y * Width) + x) * 4;
    }

    /// <summary>
    /// Decodes the non-interlaced PNG files resource packs use (grey, RGB, palette, grey+alpha, RGBA).
    /// </summary>
    public static class PngDecoder
    {
        private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

        public static RgbaImage Decode(byte[] png)
        {
            if (png.Length < Signature.Length || !png.AsSpan(0, Signature.Length).SequenceEqual(Signature))
                throw new InvalidDataException("Not a PNG file.");

            int width = 0, height = 0, bitDepth = 0, colorType = 0;
            byte[] palette = null;
            byte[] transparency = null;
            using var compressed = new MemoryStream();
            var position = Signature.Length;
            while (position + 8 <= png.Length)
            {
                var length = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(position));
                var type = System.Text.Encoding.ASCII.GetString(png, position + 4, 4);
                var data = png.AsSpan(position + 8, length);
                position += 12 + length;
                switch (type)
                {
                    case "IHDR":
                        width = (int)BinaryPrimitives.ReadUInt32BigEndian(data);
                        height = (int)BinaryPrimitives.ReadUInt32BigEndian(data.Slice(4));
                        bitDepth = data[8];
                        colorType = data[9];
                        if (data[12] != 0)
                            throw new NotSupportedException("Interlaced PNG files are not supported.");
                        break;
                    case "PLTE":
                        palette = data.ToArray();
                        break;
                    case "tRNS":
                        transparency = data.ToArray();
                        break;
                    case "IDAT":
                        compressed.Write(data);
                        break;
                    case "IEND":
                        position = png.Length;
                        break;
                }
            }

            var channels = colorType switch
            {
                0 => 1,
                2 => 3,
                3 => 1,
                4 => 2,
                6 => 4,
                _ => throw new NotSupportedException($"PNG colour type {colorType} is not supported."),
            };
            var bitsPerPixel = channels * bitDepth;
            var stride = ((width * bitsPerPixel) + 7) / 8;
            var filterUnit = Math.Max(1, bitsPerPixel / 8);
            var raw = new byte[(stride + 1) * height];
            compressed.Position = 0;
            using (var inflater = new ZLibStream(compressed, CompressionMode.Decompress))
                inflater.ReadExactly(raw);

            var rows = Unfilter(raw, stride, height, filterUnit);
            var pixels = new byte[width * height * 4];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var o = ((y * width) + x) * 4;
                    var rowStart = y * stride;
                    switch (colorType)
                    {
                        case 0:
                            {
                                var grey = Sample(rows, rowStart, x, bitDepth);
                                var value = Scale(grey, bitDepth);
                                pixels[o] = pixels[o + 1] = pixels[o + 2] = value;
                                pixels[o + 3] = transparency != null && transparency.Length >= 2 && grey == BinaryPrimitives.ReadUInt16BigEndian(transparency) ? (byte)0 : (byte)255;
                                break;
                            }

                        case 2:
                            {
                                var r = Sample(rows, rowStart, x * 3, bitDepth);
                                var g = Sample(rows, rowStart, (x * 3) + 1, bitDepth);
                                var b = Sample(rows, rowStart, (x * 3) + 2, bitDepth);
                                pixels[o] = Scale(r, bitDepth);
                                pixels[o + 1] = Scale(g, bitDepth);
                                pixels[o + 2] = Scale(b, bitDepth);
                                var keyed = transparency != null && transparency.Length >= 6
                                    && r == BinaryPrimitives.ReadUInt16BigEndian(transparency)
                                    && g == BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(2))
                                    && b == BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(4));
                                pixels[o + 3] = keyed ? (byte)0 : (byte)255;
                                break;
                            }

                        case 3:
                            {
                                var index = Sample(rows, rowStart, x, bitDepth);
                                if (palette != null && (index * 3) + 2 < palette.Length)
                                {
                                    pixels[o] = palette[index * 3];
                                    pixels[o + 1] = palette[(index * 3) + 1];
                                    pixels[o + 2] = palette[(index * 3) + 2];
                                }

                                pixels[o + 3] = transparency != null && index < transparency.Length ? transparency[index] : (byte)255;
                                break;
                            }

                        case 4:
                            pixels[o] = pixels[o + 1] = pixels[o + 2] = Scale(Sample(rows, rowStart, x * 2, bitDepth), bitDepth);
                            pixels[o + 3] = Scale(Sample(rows, rowStart, (x * 2) + 1, bitDepth), bitDepth);
                            break;
                        default:
                            for (var c = 0; c < 4; c++)
                                pixels[o + c] = Scale(Sample(rows, rowStart, (x * 4) + c, bitDepth), bitDepth);
                            break;
                    }
                }
            }

            return new RgbaImage(width, height, pixels);
        }

        /// <summary>
        /// Reads only the header, for the texture size.
        /// </summary>
        /// <param name="png">PNG file contents.</param>
        /// <returns>Width and height.</returns>
        public static (int Width, int Height) ReadSize(byte[] png) =>
            ((int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16)), (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20)));

        private static byte[] Unfilter(byte[] raw, int stride, int height, int unit)
        {
            var rows = new byte[stride * height];
            for (var y = 0; y < height; y++)
            {
                var filter = raw[y * (stride + 1)];
                var source = (y * (stride + 1)) + 1;
                var target = y * stride;
                var previous = target - stride;
                for (var i = 0; i < stride; i++)
                {
                    var a = i >= unit ? rows[target + i - unit] : 0;
                    var b = y > 0 ? rows[previous + i] : 0;
                    var c = i >= unit && y > 0 ? rows[previous + i - unit] : 0;
                    var value = raw[source + i];
                    rows[target + i] = filter switch
                    {
                        0 => value,
                        1 => (byte)(value + a),
                        2 => (byte)(value + b),
                        3 => (byte)(value + ((a + b) / 2)),
                        4 => (byte)(value + Paeth(a, b, c)),
                        _ => throw new InvalidDataException($"Unknown PNG filter {filter}."),
                    };
                }
            }

            return rows;
        }

        private static int Paeth(int a, int b, int c)
        {
            var p = a + b - c;
            var pa = Math.Abs(p - a);
            var pb = Math.Abs(p - b);
            var pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc)
                return a;
            return pb <= pc ? b : c;
        }

        // Sample n of a row: a sub-byte index, a byte, or the high byte of a 16-bit value.
        private static int Sample(byte[] rows, int rowStart, int n, int bitDepth)
        {
            switch (bitDepth)
            {
                case 8:
                    return rows[rowStart + n];
                case 16:
                    return (rows[rowStart + (n * 2)] << 8) | rows[rowStart + (n * 2) + 1];
                default:
                    var bit = n * bitDepth;
                    var shift = 8 - bitDepth - (bit % 8);
                    return (rows[rowStart + (bit / 8)] >> shift) & ((1 << bitDepth) - 1);
            }
        }

        private static byte Scale(int value, int bitDepth) => bitDepth switch
        {
            8 => (byte)value,
            16 => (byte)(value >> 8),
            _ => (byte)(value * 255 / ((1 << bitDepth) - 1)),
        };
    }
}
