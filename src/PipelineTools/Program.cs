using System.CommandLine;
using System.CommandLine.Parsing;
using AzurePipelineTool.Commands;

namespace PipelineTools;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand();
        rootCommand.Add(new VisualStudioEnvironmentCommand().Command);
        rootCommand.Add(new WindowsSdkEnvironmentCommand().Command);
        rootCommand.Add(new PatchDllImportCommand().Command);
        rootCommand.Add(new XCodeSetBuildSystemTypeCommand().Command);
        rootCommand.Add(new GetCPUInfoCommand().Command);
        rootCommand.Add(new ZipCommand().Command);
        rootCommand.Add(new UnzipCommand().Command);
        rootCommand.Add(new ZipToBlobCommand().Command);
        rootCommand.Add(new UnzipBlobCommand().Command);
        rootCommand.Add(new UnzipUrlCommand().Command);
        rootCommand.Add(new FetchToolCommand().Command);
        rootCommand.Add(new UntgzCommand().Command);
        await rootCommand.Parse(args).InvokeAsync();
    }
}