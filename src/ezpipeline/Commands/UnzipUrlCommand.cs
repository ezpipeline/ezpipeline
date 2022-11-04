using System.Net;
using PipelineTools;

namespace AzurePipelineTool.Commands;

public class UnzipUrlCommand : AbstractCommand<UnzipUrlCommand.UnzipUrlOptions>
{
    public UnzipUrlCommand() : base("unzip-url", "Unarchive content of a web file")
    {
    }

    public override async Task HandleCommandAsync(UnzipUrlOptions options, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Downloading {options.Url} to {options.Output}");
        var tempFileName = PipelineUtils.GetTempFileName(options.Temp);
        try
        {
            await new WebClient().DownloadFileTaskAsync(new Uri(options.Url), tempFileName);
            if (options.ArchiveType == ArchiveType.auto)
            {
                if (options.Url.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
                    options.ArchiveType = ArchiveType.zip;
                else if (options.Url.EndsWith(".tar.gz", StringComparison.InvariantCultureIgnoreCase))
                    options.ArchiveType = ArchiveType.tgz;
                else if (options.Url.EndsWith(".tgz", StringComparison.InvariantCultureIgnoreCase))
                    options.ArchiveType = ArchiveType.tgz;
            }

            switch (options.ArchiveType)
            {
                case ArchiveType.zip:
                    await new UnzipCommand().HandleCommandAsync(new UnzipCommand.UnzipOptions
                    {
                        Input = tempFileName,
                        Output = options.Output,
                        Filter = options.Filter,
                        Overwrite = options.Overwrite,
                        RootPath = options.RootPath
                    }, cancellationToken);
                    break;
                case ArchiveType.tgz:
                    if (!string.IsNullOrWhiteSpace(options.Filter))
                        throw new NotImplementedException("Filter is not supported yet");
                    if (!string.IsNullOrWhiteSpace(options.RootPath))
                        throw new NotImplementedException("RootPath is not supported yet");
                    await new UntgzCommand().HandleCommandAsync(new UntgzCommand.Options
                    {
                        Input = tempFileName,
                        Output = options.Output,
                        Overwrite = options.Overwrite,
                        RootPath = options.RootPath
                    }, cancellationToken);
                    break;
                default: throw new NotImplementedException(options.ArchiveType.ToString());
            }
        }
        finally
        {
            if (File.Exists(tempFileName)) File.Delete(tempFileName);
        }
    }

    public class UnzipUrlOptions
    {
        [CommandLineOption("-o", "Output folder")]
        public string Output { get; set; }

        [CommandLineOption("-f", "Filter regex")]
        public string? Filter { get; set; }

        [CommandLineOption("-u", "URL")] public string Url { get; set; }

        [CommandLineOption("-t", "Temp folder or file name")]
        public string? Temp { get; set; }

        [CommandLineOption(description: "Overwrite files")]
        public bool Overwrite { get; set; }

        [CommandLineOption("-r", "Root path to skip")]
        public string? RootPath { get; set; }

        [CommandLineOption(description: "Archive type")]
        public ArchiveType ArchiveType { get; set; }
    }
}