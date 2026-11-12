using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace MineCase.Client.Net.Rendering
{
    /// <summary>
    /// Resource packs (folders, .zip files or a client .jar) searched from the last one added to the first,
    /// the way the game stacks them. Paths are relative to the pack root, e.g. "assets/minecraft/models/block/stone.json".
    /// </summary>
    public sealed class ResourcePackStack : IDisposable
    {
        private readonly List<IPackSource> _sources = new List<IPackSource>();

        public IReadOnlyList<string> Names => _sources.Select(s => s.Name).ToList();

        public static ResourcePackStack Open(IEnumerable<string> paths)
        {
            var stack = new ResourcePackStack();
            try
            {
                foreach (var path in paths)
                    stack.Add(path);
            }
            catch
            {
                stack.Dispose();
                throw;
            }

            return stack;
        }

        public void Add(string path)
        {
            if (Directory.Exists(path))
                _sources.Add(new DirectorySource(path));
            else if (File.Exists(path))
                _sources.Add(new ZipSource(path));
            else
                throw new FileNotFoundException($"Resource pack not found: {path}", path);
        }

        public bool TryRead(string path, out byte[] data)
        {
            for (var i = _sources.Count - 1; i >= 0; i--)
            {
                if (_sources[i].TryRead(path, out data))
                    return true;
            }

            data = null;
            return false;
        }

        public bool TryReadText(string path, out string text)
        {
            if (TryRead(path, out var data))
            {
                text = Encoding.UTF8.GetString(data);
                return true;
            }

            text = null;
            return false;
        }

        public void Dispose()
        {
            foreach (var source in _sources)
                source.Dispose();
            _sources.Clear();
        }

        private interface IPackSource : IDisposable
        {
            string Name { get; }

            bool TryRead(string path, out byte[] data);
        }

        private sealed class DirectorySource : IPackSource
        {
            private readonly string _root;

            public DirectorySource(string root)
            {
                _root = root;
            }

            public string Name => _root;

            public bool TryRead(string path, out byte[] data)
            {
                var file = Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar));
                data = File.Exists(file) ? File.ReadAllBytes(file) : null;
                return data != null;
            }

            public void Dispose()
            {
            }
        }

        private sealed class ZipSource : IPackSource
        {
            private readonly ZipArchive _archive;
            private readonly Dictionary<string, ZipArchiveEntry> _entries;

            public ZipSource(string file)
            {
                Name = file;
                _archive = ZipFile.OpenRead(file);
                _entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
                foreach (var entry in _archive.Entries)
                    _entries[entry.FullName] = entry;
            }

            public string Name { get; }

            public bool TryRead(string path, out byte[] data)
            {
                if (!_entries.TryGetValue(path, out var entry))
                {
                    data = null;
                    return false;
                }

                using var stream = entry.Open();
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                data = memory.ToArray();
                return true;
            }

            public void Dispose() => _archive.Dispose();
        }
    }
}
