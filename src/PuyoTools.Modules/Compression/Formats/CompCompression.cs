﻿using System;
using System.IO;

namespace PuyoTools.Modules.Compression
{
    public class CompCompression : Lz11Compression
    {
        // The COMP compression format is identical to the LZ11 compression format with the addition of
        // "COMP" at the beginning of the file.

        /// <summary>
        /// Decompress data from a stream.
        /// </summary>
        /// <param name="source">The stream to read from.</param>
        /// <param name="destination">The stream to write to.</param>
        public override void Decompress(Stream source, Stream destination)
        {
            source.Position += 4;

            base.Decompress(source, destination);
        }

        /// <summary>
        /// Compress data from a stream.
        /// </summary>
        /// <param name="source">The stream to read from.</param>
        /// <param name="destination">The stream to write to.</param>
        public override void Compress(Stream source, Stream destination)
        {
            // COMP compression can only handle files smaller than 16MB
            if (source.Length - source.Position > 0xFFFFFF)
            {
                throw new Exception("Source is too large. COMP compression can only compress files smaller than 16MB.");
            }

            destination.WriteByte((byte)'C');
            destination.WriteByte((byte)'O');
            destination.WriteByte((byte)'M');
            destination.WriteByte((byte)'P');

            base.Compress(source, destination);
        }

        /// <summary>
        /// Returns if this codec can read the data in <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The data to read.</param>
        /// <returns>True if the data can be read, false otherwise.</returns>
        public static new bool Identify(Stream source)
        {
            return source.Length > 8
                && PTStream.Contains(source, 0, new byte[] { (byte)'C', (byte)'O', (byte)'M', (byte)'P', 0x11 });
        }
    }
}