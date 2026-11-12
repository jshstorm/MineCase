using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using MineCase.Block;
using MineCase.Nbt;
using MineCase.Nbt.Tags;
using MineCase.World.Biomes;
using Orleans;

namespace MineCase.World
{
    public static class ChunkConstants
    {
        public const int ChunkHeight = 256;
        public const int SectionsPerChunk = 16;

        public const int BlockEdgeWidthInSection = 16;

        public const int BlocksInSection = BlockEdgeWidthInSection * BlockEdgeWidthInSection * BlockEdgeWidthInSection;

        public const int BlocksInChunk = BlocksInSection * SectionsPerChunk;
    }

    [GenerateSerializer]
    public sealed class ChunkColumnCompactStorage : IChunkColumnStorage
    {
        public uint SectionBitMask
        {
            get
            {
                // Bit n marks section n, counting from the bottom of the column.
                uint mask = 0;
                for (int index = 0; index < ChunkConstants.SectionsPerChunk; index++)
                {
                    if (Sections[index] != null)
                        mask |= 1u << index;
                }

                return mask;
            }
        }

        public NbtCompound Heightmaps
        {
            get
            {
                long[] compactArray = new long[36];

                // Load WorldSurface to it
                for (int x = 0; x < ChunkConstants.BlockEdgeWidthInSection; ++x)
                {
                    for (int z = 0; z < ChunkConstants.BlockEdgeWidthInSection; ++z)
                    {
                        int i = z * ChunkConstants.BlockEdgeWidthInSection + x;

                        int maxEntryValue = (1 << 9) - 1;
                        int bitOffset = i * 9;
                        int ulongOfsset = bitOffset >> 6;
                        int ulongOfssetNext = (i + 1) * 9 - 1 >> 6;
                        int bitsLow = bitOffset ^ ulongOfsset << 6;
                        compactArray[ulongOfsset] = compactArray[ulongOfsset] & ~(maxEntryValue << bitsLow) | ((long)GroundHeight[x, z] & maxEntryValue) << bitsLow;
                        if (ulongOfsset != ulongOfssetNext)
                        {
                            int bitsHigh = 64 - bitsLow;
                            int entryOffset = 9 - bitsHigh;
                            compactArray[ulongOfssetNext] = compactArray[ulongOfssetNext] >> entryOffset << entryOffset | ((long)GroundHeight[x, z] & maxEntryValue) >> bitsHigh;
                        }
                    }
                }

                NbtCompound ret = new NbtCompound();
                ret.Add(new NbtLongArray(compactArray, "MOTION_BLOCKING"));
                return ret;
            }
        }

        [Id(0)]
        public ChunkSectionCompactStorage[] Sections { get; } = new ChunkSectionCompactStorage[ChunkConstants.SectionsPerChunk];

        [Id(1)]
        public int[,] GroundHeight { get; } = new int[ChunkConstants.BlockEdgeWidthInSection, ChunkConstants.BlockEdgeWidthInSection];

        [Id(2)]
        public int[] Biomes { get; }

        public BlockState this[int x, int y, int z]
        {
            get => Sections[y / 16].Data[x, y % 16, z];
            set => Sections[y / 16].Data[x, y % 16, z] = value;
        }

        public ChunkColumnCompactStorage()
        {
            Biomes = new int[1024];
            for (int i = 0; i < Biomes.Length; ++i)
            {
                Biomes[i] = (int)BiomeId.Plains;
            }
        }

        public ChunkColumnCompactStorage(int[] biomes)
        {
            Biomes = biomes;
        }
    }

    [GenerateSerializer]
    public sealed class ChunkSectionCompactStorage
    {
        private const byte _bitsPerBlock = 14;
        public const ulong BlockMask = (1u << _bitsPerBlock) - 1;

        [Id(0)]
        private short _nonAirBlockCount = 4096; // FIXME: count block non air

        public byte BitsPerBlock => _bitsPerBlock;

        [Id(1)]
        public DataArray Data { get; }

        [Id(2)]
        public NibbleArray BlockLight { get; }

        [Id(3)]
        public NibbleArray SkyLight { get; }

        public short NonAirBlockCount { get => _nonAirBlockCount; }

        public ChunkSectionCompactStorage(bool hasSkylight)
        {
            Data = new DataArray();
            BlockLight = new NibbleArray();
            if (hasSkylight)
                SkyLight = new NibbleArray();
        }

        public ChunkSectionCompactStorage(DataArray data, NibbleArray blockLight, NibbleArray skyLight)
        {
            Data = data;
            BlockLight = blockLight;
            SkyLight = skyLight;
        }

        [GenerateSerializer]
        public sealed class DataArray
        {
            [Id(0)]
            public ulong[] Storage { get; }

            public BlockState this[int x, int y, int z]
            {
                get
                {
                    if (y < 0 || y > 255)
                        return BlockStates.Air();
                    var offset = GetOffset(x, y, z);
                    var toRead = Math.Min(_bitsPerBlock, 64 - offset.BitOffset);
                    var value = Storage[offset.IndexOffset] >> offset.BitOffset;
                    var rest = _bitsPerBlock - toRead;
                    if (rest > 0)
                        value |= (Storage[offset.IndexOffset + 1] & ((1u << rest) - 1)) << toRead;
                    var blockState = BlockType.ParseBlockStateId((uint)(value & BlockMask));
                    return new BlockState { Id = blockState.Id, MetaValue = blockState.Meta };
                }

                set
                {
                    if (y < 0 || y > 255)
                        throw new IndexOutOfRangeException("Axis y out of range");
                    var stgValue = (ulong)((value.Id + value.MetaValue) & BlockMask);
                    var offset = GetOffset(x, y, z);
                    var tmpValue = Storage[offset.IndexOffset];
                    var mask = BlockMask << offset.BitOffset;
                    var toWrite = Math.Min(_bitsPerBlock, 64 - offset.BitOffset);
                    Storage[offset.IndexOffset] = (tmpValue & ~mask) | (stgValue << offset.BitOffset);
                    var rest = _bitsPerBlock - toWrite;
                    if (rest > 0)
                    {
                        mask = (1u << rest) - 1;
                        tmpValue = Storage[offset.IndexOffset + 1];
                        stgValue >>= toWrite;
                        Storage[offset.IndexOffset + 1] = (tmpValue & ~mask) | (stgValue & mask);
                    }
                }
            }

            public DataArray(ulong[] storage)
            {
                Storage = storage;
            }

            public DataArray()
            {
                Storage = new ulong[ChunkConstants.BlocksInSection * _bitsPerBlock / 64];
            }

            private static (int IndexOffset, int BitOffset) GetOffset(int x, int y, int z)
            {
                var index = GetBlockSerialIndex(x, y, z) * _bitsPerBlock;
                return (index / 64, index % 64);
            }
        }

        [GenerateSerializer]
        public sealed class NibbleArray
        {
            [Id(0)]
            public byte[] Storage { get; }

            public byte this[int x, int y, int z]
            {
                get
                {
                    var offset = GetBlockSerialIndex(x, y, z);
                    var value = Storage[offset / 2];
                    return offset % 2 == 0 ? (byte)(value >> 0xF) : (byte)(value & 0xF);
                }

                set
                {
                    var offset = GetBlockSerialIndex(x, y, z);
                    var tmpValue = Storage[offset / 2];
                    if (offset % 2 == 0)
                        Storage[offset / 2] = (byte)((tmpValue & 0xF) | (value << 4));
                    else
                        Storage[offset / 2] = (byte)((tmpValue & 0xF0) | (value & 0xF));
                }
            }

            public NibbleArray(byte[] storage)
            {
                Storage = storage;
            }

            public NibbleArray()
            {
                Storage = new byte[ChunkConstants.BlocksInSection / 2];
            }
        }

        private static int GetBlockSerialIndex(int x, int y, int z)
        {
            return ((y * ChunkConstants.BlockEdgeWidthInSection) + z) * ChunkConstants.BlockEdgeWidthInSection + x;
        }

        public static uint ToUInt32(ref BlockState blockState)
        {
            return blockState.Id + blockState.MetaValue;
        }
    }
}
