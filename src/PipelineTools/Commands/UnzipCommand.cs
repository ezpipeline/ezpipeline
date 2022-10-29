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

    public override void HandleCommand(UnzipOptions options)
    {
        var di = new DirectoryInfo(Path.GetFullPath(options.Output));
        Regex? filter = null;
        if (!string.IsNullOrWhiteSpace(options.Filter)) filter = new Regex(options.Filter, RegexOptions.Compiled);

        using (var fileStream = PipelineUtils.OpenFile(options.Input))
        {
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, false))
            {
                foreach (var entry in archive.Entries)
                {
                    if (filter != null && !filter.IsMatch(entry.FullName))
                    {
                        continue;
                    }
                    var fileDestinationPath = Path.GetFullPath(Path.Combine(di.FullName, entry.FullName));
                    var fileName = Path.GetFileName(fileDestinationPath);
                    if (fileName.Length == 0)
                    {
                        Directory.CreateDirectory(fileDestinationPath);
                    }
                    else
                    {
                        if (options.Overwrite || !File.Exists(fileDestinationPath))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath));
                            entry.ExtractToFile(fileDestinationPath, options.Overwrite);
                        }
                    }
                }
            }
        }
    }

    public class UnzipOptions
    {
        [CommandLineOption("-i", "Input file")]
        public string Input { get; set; }

        [CommandLineOption("-f", "Filter regex")]
        public string Filter { get; set; }

        [CommandLineOption("-o", "Output folder")]
        public string Output { get; set; }

        [CommandLineOption(description: "Overwrite files")]
        public bool Overwrite { get; set; }
    }
}