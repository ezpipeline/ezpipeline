using System.IO.Compression;
using System.Text.RegularExpressions;
using PipelineTools;
using static AzurePipelineTool.Commands.ZipCommand;

namespace AzurePipelineTool.Commands;

public class ZipCommand : AbstractCommand<ZipOptions>
{
    public ZipCommand() : base("zip")
    {
    }

    public override void HandleCommand(ZipOptions options)
    {
        var di = new DirectoryInfo(Path.GetFullPath(options.Input));
        var basePath = di.FullName;

        Regex? filter = null;
        if (!string.IsNullOrWhiteSpace(options.Filter)) filter = new Regex(options.Filter, RegexOptions.Compiled);

        var searchPattern = "*";
        using (var fileStream = PipelineUtils.CreateFile(options.Output))
        {
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, false))
            {
                foreach (var file in di.EnumerateFileSystemInfos(searchPattern, SearchOption.AllDirectories))
                {
                    var entryName = EntryFromPath(file.FullName, basePath);
                    if (filter != null && !filter.IsMatch(entryName)) continue;

                    if (file is FileInfo)
                    {
                        // Create entry for file:
                        archive.CreateEntryFromFile(file.FullName, entryName, options.CompressionLevel);
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
    }

    private string EntryFromPath(string fileFullName, string basePath)
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