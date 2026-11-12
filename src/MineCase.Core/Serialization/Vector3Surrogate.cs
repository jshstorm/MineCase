using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Orleans;

namespace MineCase.Serialization
{
    [GenerateSerializer]
    public struct Vector3Surrogate
    {
        [Id(0)]
        public float X;

        [Id(1)]
        public float Y;

        [Id(2)]
        public float Z;
    }

    [RegisterConverter]
    public sealed class Vector3SurrogateConverter : IConverter<Vector3, Vector3Surrogate>
    {
        public Vector3 ConvertFromSurrogate(in Vector3Surrogate surrogate) => new Vector3(surrogate.X, surrogate.Y, surrogate.Z);

        public Vector3Surrogate ConvertToSurrogate(in Vector3 value) => new Vector3Surrogate { X = value.X, Y = value.Y, Z = value.Z };
    }
}
