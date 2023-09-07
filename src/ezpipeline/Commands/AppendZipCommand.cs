using System.IO.Compression;
using System.Text.RegularExpressions;
using PipelineTools;

namespace AzurePipelineTool.Commands;

public class AppendZipCommand : AbstractCommand<AppendZipCommand.ZipOptions>
{
    public AppendZipCommand() : base("append-zip", "Append folder or file into existing .zip file")
    {
    }

    public static void DoZip(Stream fileStream, string input, string subfolder, ZipArchiveMode mode, string? filterPattern, CompressionLevel compressionLevel)
    {
        //if ((i.Attributes & FileAttributes.Directory) != 0)
        //{
        var di = new DirectoryInfo(input);
        var basePath = di.FullName;
        //}

        Regex? filter = null;
        if (!string.IsNullOrWhiteSpace(filterPattern)) filter = new Regex(filterPattern, RegexOptions.Compiled);

        subfolder = subfolder ?? "";
        if (subfolder.Length > 0)
        {
            subfolder = subfolder.Replace("\\", "/");
            if (!subfolder.EndsWith("/"))
                subfolder += "/";
        }

        var searchPattern = "*";
        using (var archive = new ZipArchive(fileStream, mode, false))
        {
            var existingEntries = (mode == ZipArchiveMode.Update) ? new HashSet<string>(archive.Entries.Select(_ => _.FullName)) : new HashSet<string>();

            foreach (var file in di.EnumerateFileSystemInfos(searchPattern, SearchOption.AllDirectories))
            {
                var entryName = subfolder + EntryFromPath(file.FullName, basePath);
                if (!existingEntries.Contains(entryName))
                {
                    if (filter != null && !filter.IsMatch(entryName))
                    {
                        Console.WriteLine($"Skipping {entryName}: does not match {filterPattern}");
                        continue;
                    }

                    if (file is FileInfo)
                    {
                        // Create entry for file:
                        archive.CreateEntryFromFile(file.FullName, entryName, compressionLevel);
                        Console.WriteLine($"{entryName}");
                    }
                    else
                    {
                        // Entry marking an empty dir:
                        if (file is DirectoryInfo possiblyEmpty && !possiblyEmpty.EnumerateFileSystemInfos().Any())
                        {
                            var dirEntry = $"{entryName}/";
                            archive.CreateEntry(dirEntry);
                            Console.WriteLine(dirEntry);
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Skipping {entryName}: already exists in archive");
                }
            }
        }
    }

    private static string EntryFromPath(string fileFullName, string basePath)
    {
        if (!fileFullName.StartsWith(basePath, StringComparison.InvariantCultureIgnoreCase))
            throw new ArgumentException($"{fileFullName} doesn't start with {basePath}");

        var start = basePath.Length;
        while (start < fileFullName.Length && (fileFullName[start] == Path.DirectorySeparatorChar ||
                                               fileFullName[start] == Path.AltDirectorySeparatorChar)) ++start;

        var end = fileFullName.Length;
        while (end > start && (fileFullName[end - 1] == Path.DirectorySeparatorChar ||
                               fileFullName[end - 1] == Path.AltDirectorySeparatorChar)) --end;

        if (end <= start)
            return string.Empty;
        return fileFullName.Substring(start, end - start).Replace('\"', '/');
    }

    public override Task HandleCommandAsync(ZipOptions options, CancellationToken cancellationToken)
    {
        var input = PipelineUtils.ResolvePath(options.Input);
        var output = PipelineUtils.ResolvePath(options.Output);
        var filterPattern = options.Filter;
        var subfolder = options.Subfolder;
        var compressionLevel = options.CompressionLevel;

        if (File.Exists(output))
        {
            using (var fileStream = PipelineUtils.OpenOrCreateFile(output))
            {
                DoZip(fileStream, input, subfolder, ZipArchiveMode.Update, filterPattern, compressionLevel);
            }
        }
        else
        {
            using (var fileStream = PipelineUtils.CreateFile(output))
            {
                DoZip(fileStream, input, subfolder, ZipArchiveMode.Create, filterPattern, compressionLevel);
            }
        }

        return Task.CompletedTask;
    }

    public class ZipOptions
    {
        [CommandLineOption("-i", "Input folder")]
        public string Input { get; set; }

        [CommandLineOption("-f", "Filter regex")]
        public string Filter { get; set; }

        [CommandLineOption("-o", "Output file")]
        public string Output { get; set; }

        [CommandLineOption("-s", "Output file subfolder")]
        public string Subfolder { get; set; }

        [CommandLineOption("-l", "Compression level")]
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;
    }
}