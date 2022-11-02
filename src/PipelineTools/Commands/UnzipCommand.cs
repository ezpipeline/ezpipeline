using System.IO.Compression;
using System.Text.RegularExpressions;
using PipelineTools;
using static AzurePipelineTool.Commands.UnzipCommand;

namespace AzurePipelineTool.Commands;

public class UnzipCommand : AbstractCommand<UnzipOptions>
{
    public UnzipCommand() : base("unzip")
    {
    }

    public static void DoUnzip(Stream fileStream, string optionsOutput, string? optionsFilter, bool overwrite, string? rootPath, CancellationToken cancellationToken)
    {
        var di = new DirectoryInfo(Path.GetFullPath(optionsOutput));
        Regex? filter = null;
        if (!string.IsNullOrWhiteSpace(optionsFilter)) filter = new Regex(optionsFilter, RegexOptions.Compiled);

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            rootPath = null;
        }
        else
        {
            rootPath = rootPath.Replace("\\", "/").TrimEnd('/')+'/';
        }

        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, false))
        {
            foreach (var entry in archive.Entries)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                var entryFullName = entry.FullName;


                if (rootPath != null && entryFullName.StartsWith(rootPath))
                {
                    if (entryFullName.Length == rootPath.Length)
                        continue;
                    entryFullName = entryFullName.Substring(rootPath.Length);
                }
                if (filter != null && !filter.IsMatch(entryFullName)) continue;
                var fileDestinationPath = Path.GetFullPath(Path.Combine(di.FullName, entryFullName));
                var fileName = Path.GetFileName(fileDestinationPath);
                if (fileName.Length == 0)
                {
                    Directory.CreateDirectory(fileDestinationPath);
                }
                else
                {
                    if (overwrite || !File.Exists(fileDestinationPath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath));
                        entry.ExtractToFile(fileDestinationPath, overwrite);
                    }
                }
            }
        }
    }

    public override async Task HandleCommandAsync(UnzipOptions options, CancellationToken cancellationToken)
    {
        using (var fileStream = PipelineUtils.OpenFile(options.Input))
        {
            DoUnzip(fileStream, options.Output, options.Filter, options.Overwrite, options.RootPath, cancellationToken);
        }
    }

    public class UnzipOptions
    {
        [CommandLineOption("-i", "Input file")]
        public string Input { get; set; }

        [CommandLineOption("-f", "Filter regex")]
        public string? Filter { get; set; }

        [CommandLineOption("-o", "Output folder")]
        public string Output { get; set; } = Directory.GetCurrentDirectory();

        [CommandLineOption(description: "Overwrite files")]
        public bool Overwrite { get; set; }

        [CommandLineOption("-r", "Root path to skip")]
        public string? RootPath { get; set; }
    }
}