using System.IO.Compression;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using PipelineTools;

namespace AzurePipelineTool.Commands;

public class ZipToBlobCommand : AbstractCommand<ZipToBlobCommand.ZipToBlobOptions>
{
    public ZipToBlobCommand() : base("zip-to-blob", "Archive folder into .zip Azure Blob")
    {
    }

    public override async Task HandleCommandAsync(ZipToBlobOptions options, CancellationToken cancellationToken)
    {
        var container = new BlobContainerClient(options.ConnectionString, options.ContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var tempFileName = PipelineUtils.GetTempFileName(options.Temp);

        var blobClient = container.GetBlobClient(options.BlobName);
        try
        {
            using (var fileStream = PipelineUtils.CreateFile(tempFileName))
            {
                ZipCommand.DoZip(fileStream, options.Input, options.Filter, options.CompressionLevel);
            }

            var blobHttpHeaders = new BlobHttpHeaders();
            blobHttpHeaders.ContentType = "application/zip";
            var response = await blobClient.UploadAsync(
                tempFileName,
                blobHttpHeaders,
                conditions: options.Overwrite ? null : new BlobRequestConditions { IfNoneMatch = new ETag("*") },
                cancellationToken: cancellationToken);
        }
        finally
        {
            if (File.Exists(tempFileName)) File.Delete(tempFileName);
        }
    }

    public class ZipToBlobOptions
    {
        [CommandLineOption("-i", "Input folder")]
        public string Input { get; set; } = Directory.GetCurrentDirectory();

        [CommandLineOption("-f", "Filter regex")]
        public string? Filter { get; set; }

        [CommandLineOption("-n", "Output blob name")]
        public string BlobName { get; set; }

        [CommandLineOption(description: "Connection string")]
        public string ConnectionString { get; set; }

        [CommandLineOption("-c", "Container name")]
        public string ContainerName { get; set; }

        [CommandLineOption("-l", "Compression level")]
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

        [CommandLineOption("-t", "Temp folder or file name")]
        public string? Temp { get; set; }

        [CommandLineOption(description: "Overwrite blob")]
        public bool Overwrite { get; set; }
    }
}