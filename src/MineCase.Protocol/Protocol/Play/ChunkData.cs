using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Nbt;
using MineCase.Nbt.Tags;
using MineCase.Serialization;
using MineCase.World;
using Orleans;

namespace MineCase.Protocol.Play
{
    [GenerateSerializer]
    [Packet(0x22)]
    public sealed class ChunkData : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Int)]
        public int ChunkX;

        [Id(1)]
        [SerializeAs(DataType.Int)]
        public int ChunkZ;

        [Id(2)]
        [SerializeAs(DataType.Boolean)]
        public bool FullChunk;

        [Id(3)]
        [SerializeAs(DataType.VarInt)]
        public uint PrimaryBitMask;

        [Id(4)]
        [SerializeAs(DataType.NBTTag)]
        public NbtCompound Heightmaps;

        [Id(5)]
        [SerializeAs(DataType.VarInt)]
        public uint Size;

        [Id(6)]
        [SerializeAs(DataType.Array)]
        public ChunkSection[] Data;

        [Id(7)]
        [SerializeAs(DataType.IntArray)]
        public int[] Biomes;

        [Id(8)]
        [SerializeAs(DataType.VarInt)]
        public uint NumberOfBlockEntities;

        [Id(9)]
        [SerializeAs(DataType.NbtArray)]
        public NbtCompound[] BlockEntities;

        public static ChunkData Deserialize(ref SpanReader br, bool isOverworld)
        {
            var result = new ChunkData
            {
                ChunkX = br.ReadAsInt(),
                ChunkZ = br.ReadAsInt(),
                FullChunk = br.ReadAsBoolean(),
                PrimaryBitMask = br.ReadAsVarInt(out _),
                Heightmaps = br.ReadAsNbtTag(),
            };

            var hasBioms = result.FullChunk;
            if (hasBioms)
                result.Biomes = br.ReadAsIntArray(1024);

            result.Size = br.ReadAsVarInt(out _);
            var dataReader = br.ReadAsSubReader((int)result.Size);
            var data = new List<ChunkSection>();
            while (!dataReader.IsCosumed)
                data.Add(ChunkSection.Deserialize(ref dataReader, isOverworld));
            result.Data = data.ToArray();

            result.NumberOfBlockEntities = br.ReadAsVarInt(out _);
            result.BlockEntities = br.ReadAsNbtTagArray((int)result.NumberOfBlockEntities);
            return result;
        }

        public void Deserialize(ref SpanReader br)
        {
            throw new NotSupportedException();
        }

        public void Serialize(BinaryWriter bw)
        {
            bw.WriteAsInt(ChunkX);
            bw.WriteAsInt(ChunkZ);
            bw.WriteAsBoolean(FullChunk);
            bw.WriteAsVarInt(PrimaryBitMask, out _);
            bw.WriteAsNbtTag(Heightmaps);
            if (FullChunk && Biomes != null)
                bw.WriteAsIntArray(Biomes);

            using (var mem = new MemoryStream())
            {
                using (var dbw = new BinaryWriter(mem, Encoding.UTF8, true))
                    dbw.WriteAsArray(Data);

                var dataBytes = mem.ToArray();
                Size = (uint)dataBytes.Length;

                bw.WriteAsVarInt(Size, out _);
                bw.WriteAsByteArray(dataBytes);
            }

            bw.WriteAsVarInt(NumberOfBlockEntities, out _);
            bw.WriteAsNbtTagArray(BlockEntities);
        }
    }

    [GenerateSerializer]
    public sealed class ChunkSection : IPacket
    {
        [Id(0)]
        [SerializeAs(DataType.Short)]
        public short BlockCount;

        [Id(1)]
        [SerializeAs(DataType.Byte)]
        public byte BitsPerBlock;

        [Id(2)]
        [SerializeAs(DataType.VarInt)]
        public uint PaletteLength;

        [Id(3)]
        [SerializeAs(DataType.Array)]
        public uint[] Palette;

        [Id(4)]
        [SerializeAs(DataType.VarInt)]
        public uint DataArrayLength;

        [Id(5)]
        [SerializeAs(DataType.Array)]
        public ulong[] DataArray;

        public static ChunkSection Deserialize(ref SpanReader br, bool isOverworld)
        {
            var result = new ChunkSection
            {
                BlockCount = br.ReadAsShort(),
                BitsPerBlock = br.ReadAsByte(),
            };

            if (result.BitsPerBlock < 4)
            {
                throw new InvalidDataException("ChunkSection: BitsPerBlock should be greater than or equal to 4.");
            }
            else if (result.BitsPerBlock >= 5 && result.BitsPerBlock < 9)
            {
                // Mirrors Serialize: the palette length is only written for indirect palettes.
                result.PaletteLength = br.ReadAsVarInt(out _);
                if (result.PaletteLength != 0)
                {
                    var paletteReader = br.ReadAsSubReader((int)result.PaletteLength);
                    var palette = new List<uint>();
                    while (!paletteReader.IsCosumed)
                        palette.Add(paletteReader.ReadAsVarInt(out _));
                    result.Palette = palette.ToArray();
                }
            }

            result.DataArrayLength = br.ReadAsVarInt(out _);
            var dataArray = new ulong[result.DataArrayLength];
            for (int i = 0; i < dataArray.Length; i++)
                dataArray[i] = br.ReadAsUnsignedLong();
            result.DataArray = dataArray;
            return result;
        }

        public void Deserialize(ref SpanReader br)
        {
            throw new NotImplementedException();
        }

        public void Serialize(BinaryWriter bw)
        {
            DataArrayLength = (uint)DataArray.Length;
            bw.WriteAsShort(BlockCount);
            bw.WriteAsByte(BitsPerBlock);

            if (BitsPerBlock < 4)
            {
                throw new InvalidDataException("ChunkSection: BitsPerBlock should be greater than or equal to 4.");
            }
            else if (BitsPerBlock >= 5 && BitsPerBlock < 9)
            {
                if (Palette != null)
                {
                    using (var mem = new MemoryStream())
                    {
                        using (var pbw = new BinaryWriter(mem, Encoding.UTF8, true))
                        {
                            foreach (var item in Palette)
                                pbw.WriteAsVarInt(item, out _);
                        }

                        var paletteBytes = mem.ToArray();
                        PaletteLength = (uint)paletteBytes.Length;
                        bw.WriteAsVarInt(PaletteLength, out _);
                        bw.WriteAsByteArray(paletteBytes);
                    }
                }
            }

            bw.WriteAsVarInt(DataArrayLength, out _);
            foreach (var item in DataArray)
                bw.WriteAsUnsignedLong(item);
        }
    }
}
