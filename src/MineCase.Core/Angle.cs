using System;
using System.Collections.Generic;
using System.Text;
using Orleans;

namespace MineCase
{
    [GenerateSerializer]
    public struct Angle
    {
        [Id(0)]
        public byte Value { get; set; }

        public float Degrees
        {
            get => Value * 360f / 256f;
            set => Value = (byte)(NormalizeDegree(value) * 256f / 360f);
        }

        public Angle(byte value)
        {
            Value = value;
        }

        public static implicit operator Angle(float degree)
        {
            var angle = default(Angle);
            angle.Degrees = degree;
            return angle;
        }

        private static float NormalizeDegree(float degree)
        {
            degree %= 360;
            if (degree < 0) degree += 360;
            return degree;
        }
    }
}
