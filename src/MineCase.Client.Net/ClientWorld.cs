using System;
using System.Collections.Generic;
using MineCase.Block;

namespace MineCase.Client.Net
{
    /// <summary>
    /// The chunk columns the server has sent and not yet unloaded.
    /// </summary>
    public sealed class ClientWorld
    {
        private readonly Dictionary<(int X, int Z), ChunkColumn> _columns = new Dictionary<(int X, int Z), ChunkColumn>();

        public int ColumnCount => _columns.Count;

        public IEnumerable<ChunkColumn> Columns => _columns.Values;

        public void SetColumn(ChunkColumn column) => _columns[(column.ChunkX, column.ChunkZ)] = column;

        public bool RemoveColumn(int chunkX, int chunkZ) => _columns.Remove((chunkX, chunkZ));

        public bool TryGetColumn(int chunkX, int chunkZ, out ChunkColumn column) => _columns.TryGetValue((chunkX, chunkZ), out column);

        public BlockId GetBlock(int x, int y, int z)
        {
            if (!_columns.TryGetValue((x >> 4, z >> 4), out var column))
                return BlockId.Air;
            return column.GetBlock(x & 15, y, z & 15);
        }

        public static int ToChunkCoordinate(double blockCoordinate) => (int)Math.Floor(blockCoordinate / ChunkColumn.EdgeWidth);
    }
}
