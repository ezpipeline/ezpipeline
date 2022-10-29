using Azure.Storage.Blobs;
using PipelineTools;
using System.IO.Compression;

namespace AzurePipelineTool.Commands;

public class ZipToBlobCommand : AbstractCommand<ZipToBlobCommand.ZipToBlobOptions>
{
    public ZipToBlobCommand() : base("zipToBlob")
    {
    }

    public class ZipToBlobOptions
    {
        [CommandLineOption("-i", "Input folder")]
        public string Input { get; set; }

        [CommandLineOption("-f", "Filter regex")]
        public string Filter { get; set; }

        [CommandLineOption("-o", "Output blob name")]
        public string Output { get; set; }

        [CommandLineOption(description: "Connection string")]
        public string ConnectionString { get; set; }

        [CommandLineOption(description: "Container name")]
        public string ContainerName { get; set; }

        [CommandLineOption("-l", "Compression level")]
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;
    }

    public override async Task HandleCommandAsync(ZipToBlobOptions options)
    {
        var container = new BlobContainerClient(options.ConnectionString, options.ContainerName);
        await container.CreateIfNotExistsAsync();

        //BlobClient blobClient = container.GetBlobClient(options.Output);
        //ZipCommand.DoZip();
        //FileStream fileStream = File.OpenRead(localFilePath);
        //await blobClient.UploadAsync(BinaryData.FromStream()fileStream, true);
        //fileStream.Close();
    }
}