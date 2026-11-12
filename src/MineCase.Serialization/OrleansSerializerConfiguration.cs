using System;
using Newtonsoft.Json.Linq;
using Orleans.Serialization;

namespace MineCase.Serialization
{
    public static class OrleansSerializerConfiguration
    {
        public static ISerializerBuilder AddMineCaseSerializers(this ISerializerBuilder builder)
        {
            // Chat click/hover events carry JToken values, which only the Newtonsoft codec understands.
            return builder.AddNewtonsoftJsonSerializer(isSupported: type => typeof(JToken).IsAssignableFrom(type));
        }
    }
}
