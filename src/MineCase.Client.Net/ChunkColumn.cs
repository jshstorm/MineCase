using System;
using MineCase.Block;
using MineCase.Client.Net.Rendering;
using MineCase.Protocol.Play;
using MineCase.World;

namespace MineCase.Client.Net
{
    /// <summary>
    /// Global block state ids of one 16x256x16 chunk column, decoded once so meshing does not touch the packed storage.
    /// </summary>
    public sealed class ChunkColumn
    {
        public const int SectionCount = 16;
        public const int EdgeWidth = 16;
        public const int SectionVolume = EdgeWidth * EdgeWidth * EdgeWidth;
        public const int Height = SectionCount * EdgeWidth;

        // MineCase sends every section in the direct (global palette) format that ChunkSectionCompactStorage uses.
        private const int DirectBitsPerBlock = 14;

        private readonly ushort[][] _sections;

        public ChunkColumn(int chunkX, int chunkZ, ushort[][] sections)
        {
            if (sections == null || sections.Length != SectionCount)
                throw new ArgumentException($"A chunk column has {SectionCount} sections.", nameof(sections));

            ChunkX = chunkX;
            ChunkZ = chunkZ;
            _sections = sections;
        }

        public int ChunkX { get; }

        public int ChunkZ { get; }

        /// <summary>
        /// Returns the section's block state ids indexed by <see cref="Index"/>, or null when the section is empty.
        /// </summary>
        /// <param name="sectionY">Section index from the bottom.</param>
        /// <returns>Block state ids or null.</returns>
        public ushort[] GetSection(int sectionY) => _sections[sectionY];

        public ushort GetState(int x, int y, int z)
        {
            if (y < 0 || y >= Height)
                return 0;
            var section = _sections[y >> 4];
            return section == null ? (ushort)0 : section[Index(x, y & 15, z)];
        }

        public BlockId GetBlock(int x, int y, int z) => (BlockId)BlockStateTable.Get(GetState(x, y, z)).Block.FirstStateId;

        public static int Index(int x, int y, int z) => (((y * EdgeWidth) + z) * EdgeWidth) + x;

        public static ChunkColumn FromPacket(ChunkData packet)
        {
            var sections = new ushort[SectionCount][];
            var dataIndex = 0;
            for (var sectionY = 0; sectionY < SectionCount; sectionY++)
            {
                if ((packet.PrimaryBitMask & (1u << sectionY)) == 0)
                    continue;

                var section = packet.Data[dataIndex++];
                if (section.BitsPerBlock != DirectBitsPerBlock)
                    throw new NotSupportedException($"Only the direct {DirectBitsPerBlock}-bit section format is supported, got {section.BitsPerBlock} bits.");

                var storage = new ChunkSectionCompactStorage.DataArray(section.DataArray);
                var states = new ushort[SectionVolume];
                for (var y = 0; y < EdgeWidth; y++)
                {
                    for (var z = 0; z < EdgeWidth; z++)
                    {
                        for (var x = 0; x < EdgeWidth; x++)
                        {
                            var state = storage[x, y, z];
                            states[Index(x, y, z)] = (ushort)(state.Id + state.MetaValue);
                        }
                    }
                }

                sections[sectionY] = states;
            }

            return new ChunkColumn(packet.ChunkX, packet.ChunkZ, sections);
        }
    }
}
