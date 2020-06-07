﻿using System;
using System.IO;
using System.Text;

namespace PuyoTools.Modules.Archive
{
    public class SpkArchive : ArchiveBase
    {
        public override ArchiveReader Open(Stream source)
        {
            return new SpkArchiveReader(source);
        }

        public override ArchiveWriter Create(Stream destination)
        {
            return new SpkArchiveWriter(destination);
        }

        /// <summary>
        /// Returns if this codec can read the data in <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The data to read.</param>
        /// <returns>True if the data can be read, false otherwise.</returns>
        public static bool Identify(Stream source)
        {
            return source.Length > 16
                && PTStream.Contains(source, 0, new byte[] { (byte)'S', (byte)'N', (byte)'D', (byte)'0' });
        }
    }

    #region Archive Reader
    public class SpkArchiveReader : ArchiveReader
    {
        public SpkArchiveReader(Stream source) : base(source)
        {
            // Get the number of files in the archive
            source.Position += 4;
            int numEntries = PTStream.ReadInt32(source);
            entries = new ArchiveEntryCollection(this, numEntries);

            source.Position += 8;

            // Read in all the entries
            for (int i = 0; i < numEntries; i++)
            {
                // Read in the entry filename extension, offset, length, and filename without the extension
                string entryFileExtension = PTStream.ReadCString(source, 4, EncodingExtensions.ShiftJIS);
                int entryOffset = PTStream.ReadInt32(source);
                int entryLength = PTStream.ReadInt32(source);
                string entryFilename = PTStream.ReadCString(source, 20, EncodingExtensions.ShiftJIS);

                if (entryFileExtension != String.Empty)
                    entryFileExtension = "." + entryFileExtension;

                // Add this entry to the collection
                entries.Add(startOffset + entryOffset, entryLength, entryFilename + entryFileExtension);
            }

            // Set the position of the stream to the end of the file
            source.Seek(0, SeekOrigin.End);
        }
    }
    #endregion

    #region Archive Writer
    public class SpkArchiveWriter : ArchiveWriter
    {
        public SpkArchiveWriter(Stream destination) : base(destination) { }

        public override void Flush()
        {
            // The start of the archive
            long offset = destination.Position;

            // Magic code "SND0"
            destination.WriteByte((byte)'S');
            destination.WriteByte((byte)'N');
            destination.WriteByte((byte)'D');
            destination.WriteByte((byte)'0');

            // Number of entries in the archive
            PTStream.WriteInt32(destination, entries.Count);

            destination.Position += 8;

            // Write out the header for the archive
            int entryOffset = 16 + (entries.Count * 32);

            for (int i = 0; i < entries.Count; i++)
            {
                // Write out the file extension
                string fileExtension = Path.GetExtension(entries[i].Name);
                if (fileExtension != String.Empty)
                    fileExtension = fileExtension.Substring(1);

                PTStream.WriteCString(destination, fileExtension, 4, EncodingExtensions.ShiftJIS);

                // Write out the offset, length, and filename (without the extension)
                PTStream.WriteInt32(destination, entryOffset);
                PTStream.WriteInt32(destination, entries[i].Length);
                PTStream.WriteCString(destination, Path.GetFileNameWithoutExtension(entries[i].Name), 20, EncodingExtensions.ShiftJIS);

                entryOffset += PTMethods.RoundUp(entries[i].Length, 16);
            }

            // Write out the file data for each entry
            for (int i = 0; i < entries.Count; i++)
            {
                PTStream.CopyToPadded(entries[i].Open(), destination, 16, 0);

                // Call the file added event
                OnFileAdded(EventArgs.Empty);
            }
        }
    }
    #endregion
}