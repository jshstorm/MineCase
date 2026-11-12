using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using MineCase.Buffers;
using MineCase.Protocol;
using Xunit;

namespace MineCase.UnitTest
{
    public class PacketFramingTest
    {
        [Fact]
        public async Task FormatCanBeChosenAfterReadingLength()
        {
            var compress = new PacketCompress();
            var plain = new UncompressedPacket { PacketId = 0x00, Data = new byte[] { 0x05, (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o' } };
            var small = new UncompressedPacket { PacketId = 0x05, Data = new byte[] { 1, 2, 3 } };
            var large = new UncompressedPacket { PacketId = 0x22, Data = new byte[600] };
            new Random(1).NextBytes(large.Data.Array);

            using var stream = new MemoryStream();
            await plain.SerializeAsync(stream);
            await compress.Compress(small, 256).SerializeAsync(stream);
            await compress.Compress(large, 256).SerializeAsync(stream);
            stream.Position = 0;

            using var scope = new BufferPool<byte>(ArrayPool<byte>.Shared).CreateScope();

            var first = await UncompressedPacket.DeserializeAsync(stream, Protocol.Protocol.ReadPacketLength(stream), scope);
            Assert.Equal(plain.PacketId, first.PacketId);
            Assert.Equal(plain.Data.ToArray(), first.Data.ToArray());

            foreach (var expected in new[] { small, large })
            {
                var length = Protocol.Protocol.ReadPacketLength(stream);
                var actual = compress.Decompress(await CompressedPacket.DeserializeAsync(stream, length), 256);
                Assert.Equal(expected.PacketId, actual.PacketId);
                Assert.Equal(expected.Data.ToArray(), actual.Data.ToArray());
            }

            Assert.Equal(stream.Length, stream.Position);
        }

        [Fact]
        public async Task OriginalOverloadsStillReadWholePacket()
        {
            var packet = new UncompressedPacket { PacketId = 0x0F, Data = new byte[] { 9, 8, 7, 6, 5, 4, 3, 2 } };
            using var stream = new MemoryStream();
            await packet.SerializeAsync(stream);
            await new PacketCompress().Compress(packet, 256).SerializeAsync(stream);
            stream.Position = 0;

            using var scope = new BufferPool<byte>(ArrayPool<byte>.Shared).CreateScope();
            var uncompressed = await UncompressedPacket.DeserializeAsync(stream, scope);
            var compressed = new PacketCompress().Decompress(await CompressedPacket.DeserializeAsync(stream), 256);

            Assert.Equal(packet.Data.ToArray(), uncompressed.Data.ToArray());
            Assert.Equal(packet.Data.ToArray(), compressed.Data.ToArray());
            Assert.Equal(stream.Length, stream.Position);
        }
    }
}
