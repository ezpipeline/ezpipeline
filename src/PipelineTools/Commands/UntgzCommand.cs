using System.IO.Compression;
using ICSharpCode.SharpZipLib.Tar;
using PipelineTools;

namespace AzurePipelineTool.Commands;

public class UntgzCommand : AbstractCommand<UntgzCommand.Options>
{
    public UntgzCommand() : base("untgz")
    {
    }

    public override async Task HandleCommandAsync(Options options, CancellationToken cancellationToken)
    {
        using (var fileStream = PipelineUtils.OpenFile(options.Input))
        {
            using (var gzip = new GZipStream(fileStream, CompressionMode.Decompress))
            {
                using TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzip, PipelineUtils.UTF8);
                tarArchive.SetKeepOldFiles(!options.Overwrite);
                if (!string.IsNullOrWhiteSpace(options.RootPath))
                    tarArchive.RootPath = options.RootPath;
                tarArchive.ExtractContents(options.Output, true);
                tarArchive.Close();
            }
        }
    }

    public class Options
    {
        [CommandLineOption("-i", "Input file")]
        public string Input { get; set; }

        [CommandLineOption("-o", "Output folder")]
        public string Output { get; set; } = Directory.GetCurrentDirectory();

        [CommandLineOption(description: "Overwrite files")]
        public bool Overwrite { get; set; }

        [CommandLineOption("-r", "Root path to skip")]
        public string? RootPath { get; set; }
    }
}