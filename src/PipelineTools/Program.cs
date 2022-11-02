using System.CommandLine;
using System.CommandLine.Parsing;
using AzurePipelineTool.Commands;

namespace PipelineTools;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand();
        var commands = new CommandBase[]
        {
            new VisualStudioEnvironmentCommand(),
            new WindowsSdkEnvironmentCommand(),
            new PatchDllImportCommand(),
            new XCodeSetBuildSystemTypeCommand(),
            new GetCPUInfoCommand(),
            new ZipCommand(),
            new UnzipCommand(),
            new ZipToBlobCommand(),
            new UnzipBlobCommand(),
            new UnzipUrlCommand(),
            new FetchToolCommand(),
            new UntgzCommand(),
        };
        foreach (var cmd in commands.OrderBy(_=>_.Command.Name))
        {
            rootCommand.Add(cmd.Command);
        }

        await rootCommand.InvokeAsync(args);
    }
}