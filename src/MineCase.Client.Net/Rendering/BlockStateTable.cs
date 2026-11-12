using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MineCase.Client.Net.Rendering
{
    /// <summary>
    /// One block type of the 1.15.2 global palette and the properties its states are made of.
    /// </summary>
    public sealed class BlockInfo
    {
        public BlockInfo(string name, ushort firstStateId, bool hasCollision, IReadOnlyList<(string Name, string[] Values)> properties)
        {
            Name = name;
            FirstStateId = firstStateId;
            HasCollision = hasCollision;
            Properties = properties;
            StateCount = properties.Aggregate(1, (count, p) => count * p.Values.Length);
        }

        public string Name { get; }

        public ushort FirstStateId { get; }

        public int StateCount { get; }

        public bool HasCollision { get; }

        public IReadOnlyList<(string Name, string[] Values)> Properties { get; }
    }

    /// <summary>
    /// A global block state id decoded into its block and property values.
    /// </summary>
    public sealed class BlockStateInfo
    {
        public BlockStateInfo(ushort stateId, BlockInfo block, IReadOnlyDictionary<string, string> properties)
        {
            StateId = stateId;
            Block = block;
            Properties = properties;
        }

        public ushort StateId { get; }

        public BlockInfo Block { get; }

        public string Name => Block.Name;

        public IReadOnlyDictionary<string, string> Properties { get; }

        public string GetProperty(string name) => Properties.TryGetValue(name, out var value) ? value : null;
    }

    /// <summary>
    /// The 1.15.2 block state registry (the ids the server puts in chunk sections).
    /// </summary>
    public static class BlockStateTable
    {
        private static readonly Lazy<(BlockInfo[] Blocks, BlockStateInfo[] States)> Table = new Lazy<(BlockInfo[] Blocks, BlockStateInfo[] States)>(Load);

        public static IReadOnlyList<BlockInfo> Blocks => Table.Value.Blocks;

        public static int StateCount => Table.Value.States.Length;

        public static BlockStateInfo Get(int stateId)
        {
            var states = Table.Value.States;
            return (uint)stateId < (uint)states.Length ? states[stateId] : states[0];
        }

        private static (BlockInfo[] Blocks, BlockStateInfo[] States) Load()
        {
            using var stream = typeof(BlockStateTable).Assembly.GetManifestResourceStream("MineCase.Client.Net.BlockStates115.txt")
                ?? throw new InvalidOperationException("The block state table is not embedded.");
            using var reader = new StreamReader(stream);
            var blocks = new List<BlockInfo>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0 || line[0] == '#')
                    continue;

                var parts = line.Split(' ');
                var properties = parts.Skip(3).Select(p =>
                {
                    var separator = p.IndexOf('=', StringComparison.Ordinal);
                    return (p.Substring(0, separator), p.Substring(separator + 1).Split(','));
                }).ToList();
                blocks.Add(new BlockInfo(parts[0], ushort.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), parts[2] == "1", properties));
            }

            var last = blocks[blocks.Count - 1];
            var states = new BlockStateInfo[last.FirstStateId + last.StateCount];
            foreach (var block in blocks)
            {
                for (var offset = 0; offset < block.StateCount; offset++)
                {
                    var values = new Dictionary<string, string>(StringComparer.Ordinal);
                    var rest = offset;
                    for (var i = block.Properties.Count - 1; i >= 0; i--)
                    {
                        var property = block.Properties[i];
                        values[property.Name] = property.Values[rest % property.Values.Length];
                        rest /= property.Values.Length;
                    }

                    var id = (ushort)(block.FirstStateId + offset);
                    states[id] = new BlockStateInfo(id, block, values);
                }
            }

            return (blocks.ToArray(), states);
        }
    }
}
