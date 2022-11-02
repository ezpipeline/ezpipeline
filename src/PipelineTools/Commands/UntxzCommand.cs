using System.IO.Compression;
using SevenZip.Compression.LZMA;
using PipelineTools;

namespace AzurePipelineTool.Commands;

public class UntxzCommand : AbstractCommand<UntxzCommand.Options>
{
    public UntxzCommand() : base("untxz")
    {
    }

    public override async Task HandleCommandAsync(Options options, CancellationToken cancellationToken)
    {
        using (var strmInStream = PipelineUtils.OpenFile(options.Input))
        {
            var decoder = new Decoder();

            byte[] properties2 = new byte[5];
            if (strmInStream.Read(properties2, 0, 5) != 5)
                throw (new System.Exception("input .lzma is too short"));

            long outSize = 0;
            for (int i = 0; i < 8; i++)
            {
                int v = strmInStream.ReadByte();
                if (v < 0)
                    throw (new System.Exception("Can't Read 1"));
                outSize |= ((long)(byte)v) << (8 * i);
            } //Next i
            decoder.SetDecoderProperties(properties2);

            throw new NotImplementedException();
            //long compressedSize = strmInStream.Length - strmInStream.Position;
            //decoder.Code(strmInStream, strmOutStream, compressedSize, outSize, null);

            //retVal = strmOutStream.ToArray();

            //decoder.Code();
            //using (var gzip = new Lz(fileStream, CompressionMode.Decompress))
            //{
            //    using TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzip, PipelineUtils.UTF8);
            //    tarArchive.SetKeepOldFiles(!options.Overwrite);
            //    if (!string.IsNullOrWhiteSpace(options.RootPath))
            //        tarArchive.RootPath = options.RootPath;
            //    tarArchive.ExtractContents(options.Output, true);
            //    tarArchive.Close();
            //}
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