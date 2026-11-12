using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MineCase.Client.Net.Rendering
{
    public readonly record struct EntityTypeInfo(uint Id, string Name, float Width, float Height);

    /// <summary>
    /// The 1.15.2 entity registry: names and bounding box sizes by the type id spawn packets carry.
    /// </summary>
    public static class EntityTypeTable
    {
        private static readonly Lazy<Dictionary<uint, EntityTypeInfo>> Table = new Lazy<Dictionary<uint, EntityTypeInfo>>(Load);

        public static EntityTypeInfo Get(uint id) =>
            Table.Value.TryGetValue(id, out var info) ? info : new EntityTypeInfo(id, "unknown", 0.6f, 0.6f);

        private static Dictionary<uint, EntityTypeInfo> Load()
        {
            using var stream = typeof(EntityTypeTable).Assembly.GetManifestResourceStream("MineCase.Client.Net.EntityTypes115.txt")
                ?? throw new InvalidOperationException("The entity type table is not embedded.");
            using var reader = new StreamReader(stream);
            var table = new Dictionary<uint, EntityTypeInfo>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0 || line[0] == '#')
                    continue;
                var parts = line.Split(' ');
                var id = uint.Parse(parts[0], CultureInfo.InvariantCulture);
                table[id] = new EntityTypeInfo(id, parts[1], float.Parse(parts[2], CultureInfo.InvariantCulture), float.Parse(parts[3], CultureInfo.InvariantCulture));
            }

            return table;
        }
    }
}
