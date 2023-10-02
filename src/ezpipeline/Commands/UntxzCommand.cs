using ICSharpCode.SharpZipLib.Tar;
using PipelineTools;
using SevenZip.Compression.LZMA;

namespace AzurePipelineTool.Commands;

public class UntxzCommand : AbstractCommand<UntxzCommand.Options>
{
    public UntxzCommand() : base("untxz", "Unarchive content of .tar.xz file")
    {
    }

    public override async Task HandleCommandAsync(Options options, CancellationToken cancellationToken)
    {
        using (var strmInStream = PipelineUtils.OpenFile(options.Input))
        {
            var decoder = new Decoder();

            var properties2 = new byte[5];
            if (strmInStream.Read(properties2, 0, 5) != 5)
                throw new Exception("input .lzma is too short");

            long outSize = 0;
            for (var i = 0; i < 8; i++)
            {
                var v = strmInStream.ReadByte();
                if (v < 0)
                    throw new Exception("Can't Read 1");
                outSize |= (long)(byte)v << (8 * i);
            } //Next i

            decoder.SetDecoderProperties(properties2);

            var compressedSize = strmInStream.Length - strmInStream.Position;
            var strmOutStream = new MemoryStream();
            decoder.Code(strmInStream, strmOutStream, compressedSize, outSize, null);
            strmOutStream.Flush();
            strmOutStream.Position = 0;

            using var tarArchive = TarArchive.CreateInputTarArchive(strmOutStream, PipelineUtils.UTF8);
            tarArchive.SetKeepOldFiles(!options.Overwrite);
            if (!string.IsNullOrWhiteSpace(options.RootPath))
                tarArchive.RootPath = options.RootPath;
            tarArchive.ExtractContents(options.Output, true);
            tarArchive.Close();
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