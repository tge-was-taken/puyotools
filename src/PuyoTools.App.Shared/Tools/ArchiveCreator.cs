﻿using PuyoTools.App.Formats.Archives;
using PuyoTools.Modules.Archive;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;

namespace PuyoTools.App.Tools
{
    class ArchiveCreator
    {
        public static void Execute(
            IArchiveFormat format,
            IList<ArchiveCreatorFileEntry> files,
            string outputPath,
            ArchiveCreatorOptions options,
            IArchiveFormatOptions formatOptions,
            IProgress<ToolProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            // Setup some stuff for the progress dialog
            int entryIndex = 0;
            progress?.Report(new ToolProgress((double)entryIndex / files.Count, outputPath));

            // For some archives, the file needs to be a specific format. As such,
            // they may be rejected when trying to add them. We'll store such files in
            // this list to let the user know they could not be added.
            List<string> rejectedFiles = new List<string>();

            // Create the stream we are going to write the archive to
            Stream destination;
            if (options.CompressionFormat == null)
            {
                // We are not compression the archive. Write directly to the destination
                destination = File.Create(outputPath);
            }
            else
            {
                // We are compressing the archive. Write to a memory stream first.
                destination = new MemoryStream();
            }

            // Create the archive
            using (ArchiveWriter archive = format.GetCodec().Create(destination))
            {
                // Set archive settings
                /*ModuleSettingsControl settingsControl = settings.WriterSettingsControl;
                if (settingsControl != null)
                {
                    Action moduleSettingsAction = () => settingsControl.SetModuleSettings(archive);
                    settingsControl.Invoke(moduleSettingsAction);
                }*/
                if (formatOptions != null)
                {
                    if (formatOptions is ISynchronizeInvoke invoker && invoker.InvokeRequired)
                    {
                        invoker.Invoke((Action)(() => formatOptions.MapTo(archive)), null);
                    }
                    else
                    {
                        formatOptions.MapTo(archive);
                    }
                }

                // Set up event handlers
                archive.EntryWriting += (sender, e) =>
                {
                    /*if (archive.Entries.Count == 1)
                    {
                        dialog.Description = description + "\n\n" + string.Format("Adding {0}", Path.GetFileName(e.Entry.Path));
                    }
                    else
                    {
                        dialog.Description = description + "\n\n" + string.Format("Adding {0} ({1:N0} of {2:N0})", Path.GetFileName(e.Entry.Path), entryIndex + 1, archive.Entries.Count);
                    }*/
                };

                archive.EntryWritten += (sender, e) =>
                {
                    entryIndex++;

                    /*dialog.ReportProgress(entryIndex * 100 / archive.Entries.Count, description);

                    if (entryIndex == archive.Entries.Count)
                    {
                        dialog.ReportProgress(100, "Finishing up");
                    }*/
                };

                // Add the files to the archive. We're going to do this in a try catch since
                // sometimes an exception may be thrown (namely if the archive cannot contain
                // the file the user is trying to add)
                foreach (ArchiveCreatorFileEntry entry in files)
                {
                    try
                    {
                        archive.CreateEntryFromFile(entry.SourceFile, entry.FilenameInArchive);
                    }
                    catch (FileRejectedException)
                    {
                        rejectedFiles.Add(entry.SourceFile);
                    }
                }

                // If rejectedFiles is not empty, then show a message to the user
                // and ask them if they want to continue
                if (rejectedFiles.Count > 0)
                {
                    /*if (new RejectedFilesDialog(rejectedFiles).ShowDialog() != DialogResult.Yes)
                    {
                        destination.Close();
                        return;
                    }*/
                }
            }

            // Do we want to compress this archive?
            if (options.CompressionFormat != null)
            {
                destination.Position = 0;

                using (FileStream outStream = File.Create(outputPath))
                {
                    options.CompressionFormat.GetCodec().Compress(destination, outStream);
                }
            }

            destination.Close();
        }
    }
}
