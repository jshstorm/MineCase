using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MineCase.Block;
using MineCase.Engine;
using MineCase.Nbt;
using MineCase.Nbt.Tags;
using MineCase.Protocol;
using MineCase.Serialization;
using MineCase.Server.Game.Entities.Components;
using MineCase.World;
using Newtonsoft.Json.Linq;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Serialization.Serializers;
using Xunit;

namespace MineCase.UnitTest
{
    public sealed class OrleansSerializationTest : IDisposable
    {
        private static readonly string[] MineCaseAssemblies =
        {
            "MineCase.Core", "MineCase.Nbt", "MineCase.Protocol", "MineCase.Server.Engine", "MineCase.Server.Interfaces", "MineCase.Server.Grains",
        };

        private readonly IHost _host;
        private readonly Serializer _serializer;
        private readonly DeepCopier _copier;
        private readonly ICodecProvider _codecs;

        public OrleansSerializationTest()
        {
            _host = new HostBuilder()
                .UseOrleansClient(client => client
                    .UseLocalhostClustering()
                    .Services.AddSerializer(serializer => serializer.AddMineCaseSerializers()))
                .Build();
            _serializer = _host.Services.GetRequiredService<Serializer>();
            _copier = _host.Services.GetRequiredService<DeepCopier>();
            _codecs = _host.Services.GetRequiredService<ICodecProvider>();
        }

        public void Dispose() => _host.Dispose();

        [Fact]
        public void EveryTypeCrossingGrainBoundariesHasCodecAndCopier()
        {
            var missing = new List<string>();
            foreach (var (type, origin) in GetWireTypes())
            {
                if (_codecs.TryGetCodec(type) == null)
                    missing.Add($"codec: {type} ({origin})");
                if (_codecs.TryGetDeepCopier(type) == null)
                    missing.Add($"copier: {type} ({origin})");
            }

            Assert.True(missing.Count == 0, string.Join(Environment.NewLine, missing));
        }

        [Fact]
        public void EveryGeneratedSerializerRoundTrips()
        {
            var failures = new List<string>();
            foreach (var type in GetAnnotatedTypes())
            {
                try
                {
                    var instance = CreateInstance(type);
                    var copy = _serializer.Deserialize<object>(_serializer.SerializeToArray<object>(instance));
                    Assert.IsType(type, copy);
                    Assert.IsType(type, _copier.Copy<object>(instance));
                }
                catch (Exception ex)
                {
                    failures.Add($"{type}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        }

        [Fact]
        public void PopulatedValuesRoundTrip()
        {
            var position = new Vector3(1.5f, -2, 64);
            Assert.Equal(position, RoundTrip(position));

            var chat = new Chat(new StringComponent
            {
                Text = "hello case!",
                Bold = true,
                ClickEvent = new ChatClickEvent(ClickEventType.OpenUrl, "http://case.orz"),
            });
            Assert.Equal(chat.ToString(), RoundTrip(chat).ToString());

            var nbt = new NbtFile();
            nbt.RootTag.Add(new NbtString("chest", "Name"));
            nbt.RootTag.Add(new NbtInt(3, "Count"));
            var slot = new Slot { BlockId = 54, ItemCount = 2, ItemDamage = 1, NBT = nbt };
            var slotCopy = RoundTrip(slot);
            Assert.Equal(slot.BlockId, slotCopy.BlockId);
            Assert.Equal(slot.ItemCount, slotCopy.ItemCount);
            var nameTag = slotCopy.NBT.RootTag.Get<NbtString>("Name");
            Assert.Equal("chest", nameTag.Value);
            Assert.Same(slotCopy.NBT.RootTag, nameTag.Parent);

            var column = new ChunkColumnCompactStorage(Enumerable.Range(0, 1024).ToArray());
            column.Sections[0] = new ChunkSectionCompactStorage(true);
            column[1, 2, 3] = BlockStates.Stone();
            var columnCopy = RoundTrip(column);
            Assert.Equal(BlockStates.Stone(), columnCopy[1, 2, 3]);
            Assert.Equal(column.Biomes, columnCopy.Biomes);

            var packet = new UncompressedPacket { Length = 4, PacketId = 0x21, Data = new ArraySegment<byte>(new byte[] { 9, 1, 2, 3, 4, 9 }, 1, 4) };
            var packetCopy = RoundTrip(packet);
            Assert.Equal(packet.PacketId, packetCopy.PacketId);
            Assert.Equal(packet.Data.ToArray(), packetCopy.Data.ToArray());

            var ask = new AskResult<(int Index, Slot Slot)> { Succeeded = true, Response = (3, new Slot { BlockId = 1, ItemCount = 1 }) };
            var askCopy = RoundTrip(ask);
            Assert.True(askCopy.Succeeded);
            Assert.Equal(3, askCopy.Response.Index);

            IEntityMessage message = new SetHeldItem { Slot = new Slot { BlockId = 5, ItemCount = 7 } };
            var messageCopy = Assert.IsType<SetHeldItem>(RoundTrip(message));
            Assert.Equal(7, messageCopy.Slot.ItemCount);
        }

        private T RoundTrip<T>(T value) => _serializer.Deserialize<T>(_serializer.SerializeToArray(value));

        private static IEnumerable<Assembly> LoadMineCaseAssemblies() => MineCaseAssemblies.Select(Assembly.Load);

        private static IEnumerable<Type> GetAnnotatedTypes() =>
            from asm in LoadMineCaseAssemblies()
            from type in asm.GetTypes()
            where type.IsDefined(typeof(GenerateSerializerAttribute), false) && !type.IsAbstract && !type.IsGenericTypeDefinition
            select type;

        private static IEnumerable<(Type Type, string Origin)> GetWireTypes()
        {
            var types = LoadMineCaseAssemblies().SelectMany(a => a.GetTypes()).ToList();
            var result = new Dictionary<Type, string>();

            void Add(Type type, string origin)
            {
                if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(ValueTask<>)))
                    type = type.GetGenericArguments()[0];
                if (type == typeof(Task) || type == typeof(ValueTask) || type == typeof(void) || type.ContainsGenericParameters)
                    return;
                if (type.IsInterface || type.IsAbstract)
                    return;
                result.TryAdd(type, origin);
            }

            foreach (var grainInterface in types.Where(t => t.IsInterface && typeof(IAddressable).IsAssignableFrom(t)))
            {
                foreach (var method in grainInterface.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    foreach (var parameter in method.GetParameters())
                        Add(parameter.ParameterType, $"{grainInterface.Name}.{method.Name}");
                    Add(method.ReturnType, $"{grainInterface.Name}.{method.Name}");
                }
            }

            foreach (var type in types.Where(t => !t.IsAbstract && !t.IsInterface && !t.ContainsGenericParameters))
            {
                if (typeof(IEntityMessage).IsAssignableFrom(type) || typeof(IPacket).IsAssignableFrom(type))
                    Add(type, "message/packet");

                foreach (var response in type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityMessage<>)))
                {
                    Add(type, "message");
                    Add(response.GetGenericArguments()[0], $"response of {type.Name}");
                    Add(typeof(AskResult<>).MakeGenericType(response.GetGenericArguments()[0]), $"TryAsk of {type.Name}");
                }
            }

            return result.Select(kv => (kv.Key, kv.Value));
        }

        private static object CreateInstance(Type type)
        {
            if (type.IsValueType || type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes) == null)
                return RuntimeHelpers.GetUninitializedObject(type);
            return Activator.CreateInstance(type, nonPublic: true);
        }
    }
}
