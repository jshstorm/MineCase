using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;

namespace MineCase.Protocol
{
    public static class Protocol
    {
        public const uint Version = 578;

        public const string VersionName = "1.15.2";

        public const uint SetCompressionPacketId = 0x03;

        public static void ValidatePacketLength(uint length)
        {
            if (length > 16 * 1024)
                throw new ArgumentOutOfRangeException("Packet is too large.");
        }

        /// <summary>
        /// Reads the leading length field, which compressed and uncompressed packets share.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>The packet length.</returns>
        public static uint ReadPacketLength(Stream stream)
        {
            using (var br = new BinaryReader(stream, Encoding.UTF8, true))
            {
                return br.ReadAsVarInt(out _);
            }
        }
    }
}
