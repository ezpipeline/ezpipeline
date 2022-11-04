using Azure.Storage.Blobs;
using PipelineTools;
using static AzurePipelineTool.Commands.UnzipBlobCommand;

namespace AzurePipelineTool.Commands;

public class UnzipBlobCommand : AbstractCommand<UnzipBlobOptions>
{
    public UnzipBlobCommand() : base("unzip-blob", "Unarchive content of Azure Blob")
    {
    }

    public override async Task HandleCommandAsync(UnzipBlobOptions options, CancellationToken cancellationToken)
    {
        var container = new BlobContainerClient(options.ConnectionString, options.ContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var tempFileName = PipelineUtils.GetTempFileName(options.Temp);

        var blobClient = container.GetBlobClient(options.BlobName);
        try
        {
            await blobClient.DownloadToAsync(tempFileName, cancellationToken);
            await new UnzipCommand().HandleCommandAsync(new UnzipCommand.UnzipOptions
            {
                Input = tempFileName,
                Output = options.Output,
                Filter = options.Filter,
                Overwrite = options.Overwrite,
                RootPath = options.RootPath
            }, cancellationToken);
        }
        finally
        {
            if (File.Exists(tempFileName)) File.Delete(tempFileName);
        }
    }

    public class UnzipBlobOptions
    {
        [CommandLineOption("-o", "Output folder")]
        public string Output { get; set; } = Directory.GetCurrentDirectory();

        [CommandLineOption("-f", "Filter regex")]
        public string? Filter { get; set; }

        [CommandLineOption("-n", "Output blob name")]
        public string BlobName { get; set; }

        [CommandLineOption(description: "Connection string")]
        public string ConnectionString { get; set; }

        [CommandLineOption("-c", "Container name")]
        public string ContainerName { get; set; }

        [CommandLineOption("-t", "Temp folder or file name")]
        public string? Temp { get; set; }

        [CommandLineOption(description: "Overwrite files")]
        public bool Overwrite { get; set; }

        [CommandLineOption("-r", "Root path to skip")]
        public string? RootPath { get; set; }
    }
}