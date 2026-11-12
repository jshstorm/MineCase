using System;
using System.Numerics;

namespace MineCase.Client.Net.Rendering
{
    /// <summary>
    /// Face corners and texture mapping of model elements, following the game's model format:
    /// corners go top-left, bottom-left, bottom-right, top-right as seen from outside, which is counter-clockwise.
    /// </summary>
    internal static class ModelGeometry
    {
        private static readonly Vector3 Center = new Vector3(0.5f);

        public static Vector3[] FaceCorners(Direction face, Vector3 from, Vector3 to)
        {
            float x1 = from.X, y1 = from.Y, z1 = from.Z, x2 = to.X, y2 = to.Y, z2 = to.Z;
            return face switch
            {
                Direction.Down => new[] { new Vector3(x1, y1, z2), new Vector3(x1, y1, z1), new Vector3(x2, y1, z1), new Vector3(x2, y1, z2) },
                Direction.Up => new[] { new Vector3(x1, y2, z1), new Vector3(x1, y2, z2), new Vector3(x2, y2, z2), new Vector3(x2, y2, z1) },
                Direction.North => new[] { new Vector3(x2, y2, z1), new Vector3(x2, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y2, z1) },
                Direction.South => new[] { new Vector3(x1, y2, z2), new Vector3(x1, y1, z2), new Vector3(x2, y1, z2), new Vector3(x2, y2, z2) },
                Direction.West => new[] { new Vector3(x1, y2, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z2), new Vector3(x1, y2, z2) },
                _ => new[] { new Vector3(x2, y2, z2), new Vector3(x2, y1, z2), new Vector3(x2, y1, z1), new Vector3(x2, y2, z1) },
            };
        }

        /// <summary>
        /// The texture area (in 1/16 units) a face shows when the model does not say.
        /// </summary>
        /// <param name="face">Face direction.</param>
        /// <param name="from">Element start in 1/16 units.</param>
        /// <param name="to">Element end in 1/16 units.</param>
        /// <returns>U1, V1, U2, V2.</returns>
        public static Vector4 DefaultUv(Direction face, Vector3 from, Vector3 to) => face switch
        {
            Direction.Down => new Vector4(from.X, 16 - to.Z, to.X, 16 - from.Z),
            Direction.Up => new Vector4(from.X, from.Z, to.X, to.Z),
            Direction.North => new Vector4(16 - to.X, 16 - to.Y, 16 - from.X, 16 - from.Y),
            Direction.South => new Vector4(from.X, 16 - to.Y, to.X, 16 - from.Y),
            Direction.West => new Vector4(from.Z, 16 - to.Y, to.Z, 16 - from.Y),
            _ => new Vector4(16 - to.Z, 16 - to.Y, 16 - from.Z, 16 - from.Y),
        };

        /// <summary>
        /// Texture coordinate (0..1) of a corner; a face rotation moves the texture corners around the face.
        /// </summary>
        /// <param name="uv">U1, V1, U2, V2 in 1/16 units.</param>
        /// <param name="rotation">Face rotation in degrees (0, 90, 180, 270).</param>
        /// <param name="corner">Corner index in <see cref="FaceCorners"/> order.</param>
        /// <returns>Texture coordinate.</returns>
        public static Vector2 CornerUv(Vector4 uv, int rotation, int corner)
        {
            var shifted = (corner + (((rotation / 90) % 4) + 4)) % 4;
            var u = shifted == 0 || shifted == 1 ? uv.X : uv.Z;
            var v = shifted == 0 || shifted == 3 ? uv.Y : uv.W;
            return new Vector2(u / 16, v / 16);
        }

        /// <summary>
        /// World-aligned texture coordinate of a point on a face, used for models rotated with uvlock.
        /// </summary>
        /// <param name="face">Face direction after rotation.</param>
        /// <param name="p">Point in block space (0..1).</param>
        /// <returns>Texture coordinate.</returns>
        public static Vector2 LockedUv(Direction face, Vector3 p) => face switch
        {
            Direction.Down => new Vector2(p.X, 1 - p.Z),
            Direction.Up => new Vector2(p.X, p.Z),
            Direction.North => new Vector2(1 - p.X, 1 - p.Y),
            Direction.South => new Vector2(p.X, 1 - p.Y),
            Direction.West => new Vector2(p.Z, 1 - p.Y),
            _ => new Vector2(1 - p.Z, 1 - p.Y),
        };

        /// <summary>
        /// Rotates a point about an axis through <paramref name="origin"/> (right-handed, degrees) and optionally
        /// stretches the other two axes so a 22.5 or 45 degree plane still spans the block.
        /// </summary>
        /// <param name="p">Point.</param>
        /// <param name="origin">Rotation origin.</param>
        /// <param name="axis">'x', 'y' or 'z'.</param>
        /// <param name="degrees">Angle.</param>
        /// <param name="rescale">Whether to stretch.</param>
        /// <returns>The rotated point.</returns>
        public static Vector3 RotateElement(Vector3 p, Vector3 origin, char axis, float degrees, bool rescale)
        {
            var rotated = Rotate(p - origin, axis, degrees);
            if (rescale && degrees != 0)
            {
                var scale = 1 / MathF.Cos(MathF.Abs(degrees) * MathF.PI / 180);
                rotated *= axis switch
                {
                    'x' => new Vector3(1, scale, scale),
                    'y' => new Vector3(scale, 1, scale),
                    _ => new Vector3(scale, scale, 1),
                };
            }

            return rotated + origin;
        }

        public static Vector3 Rotate(Vector3 v, char axis, float degrees)
        {
            var (sin, cos) = SinCos(degrees);
            return axis switch
            {
                'x' => new Vector3(v.X, (v.Y * cos) - (v.Z * sin), (v.Y * sin) + (v.Z * cos)),
                'y' => new Vector3((v.X * cos) + (v.Z * sin), v.Y, (-v.X * sin) + (v.Z * cos)),
                _ => new Vector3((v.X * cos) - (v.Y * sin), (v.X * sin) + (v.Y * cos), v.Z),
            };
        }

        /// <summary>
        /// Applies a block state variant rotation: x first, then y, both in 90 degree steps, clockwise seen from the positive end of the axis.
        /// </summary>
        /// <param name="p">Point in block space.</param>
        /// <param name="x">Rotation about X in degrees.</param>
        /// <param name="y">Rotation about Y in degrees.</param>
        /// <returns>The rotated point.</returns>
        public static Vector3 RotateVariant(Vector3 p, int x, int y) =>
            Rotate(Rotate(p - Center, 'x', -x), 'y', -y) + Center;

        public static Direction RotateVariant(Direction direction, int x, int y) =>
            Directions.Nearest(Rotate(Rotate(direction.Normal(), 'x', -x), 'y', -y));

        // Exact values for right angles so rotated cubes stay on the block grid.
        private static (float Sin, float Cos) SinCos(float degrees)
        {
            var normalized = ((degrees % 360) + 360) % 360;
            return normalized switch
            {
                0 => (0, 1),
                90 => (1, 0),
                180 => (0, -1),
                270 => (-1, 0),
                _ => (MathF.Sin(degrees * MathF.PI / 180), MathF.Cos(degrees * MathF.PI / 180)),
            };
        }
    }
}
