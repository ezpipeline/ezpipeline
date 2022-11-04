using System.IO.Compression;
using System.Text.RegularExpressions;
using PipelineTools;

namespace AzurePipelineTool.Commands;

public class ZipCommand : AbstractCommand<ZipCommand.ZipOptions>
{
    public ZipCommand() : base("zip", "Archive folder into .zip file")
    {
    }

    public static void DoZip(Stream fileStream, string input, string filterPattern, CompressionLevel compressionLevel)
    {
        //if ((i.Attributes & FileAttributes.Directory) != 0)
        //{
        var di = new DirectoryInfo(Path.GetFullPath(input));
        var basePath = di.FullName;
        //}

        Regex? filter = null;
        if (!string.IsNullOrWhiteSpace(filterPattern)) filter = new Regex(filterPattern, RegexOptions.Compiled);

        var searchPattern = "*";
        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, false))
        {
            foreach (var file in di.EnumerateFileSystemInfos(searchPattern, SearchOption.AllDirectories))
            {
                var entryName = EntryFromPath(file.FullName, basePath);
                if (filter != null && !filter.IsMatch(entryName)) continue;

                if (file is FileInfo)
                {
                    // Create entry for file:
                    archive.CreateEntryFromFile(file.FullName, entryName, compressionLevel);
                }
                else
                {
                    // Entry marking an empty dir:
                    if (file is DirectoryInfo possiblyEmpty && !possiblyEmpty.EnumerateFileSystemInfos().Any())
                        archive.CreateEntry(entryName + "/");
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

    public override async Task HandleCommandAsync(ZipOptions options, CancellationToken cancellationToken)
    {
        var input = options.Input;
        var filterPattern = options.Filter;
        var compressionLevel = options.CompressionLevel;

        using (var fileStream = PipelineUtils.CreateFile(options.Output))
        {
            DoZip(fileStream, input, filterPattern, compressionLevel);
        }
    }

    public class ZipOptions
    {
        [CommandLineOption("-i", "Input folder")]
        public string Input { get; set; }

        [CommandLineOption("-f", "Filter regex")]
        public string Filter { get; set; }

        [CommandLineOption("-o", "Output file")]
        public string Output { get; set; }

        [CommandLineOption("-l", "Compression level")]
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;
    }
}