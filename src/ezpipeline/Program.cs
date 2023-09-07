using System.CommandLine;
using AzurePipelineTool.Commands;

namespace PipelineTools;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("ezpipeline "+string.Join(" ", args));

        var rootCommand = new RootCommand();
        var commands = new CommandBase[]
        {
            new AppendZipCommand(),
            new FetchToolCommand(),
            new GetCPUInfoCommand(),
            new GitHeightVersionCommand(),
            new PatchDllImportCommand(),
            new ResolvePathCommand(),
            new SendDiscordNotification(),
            new SendTelegramNotification(),
            new SetMSBuildPropertyCommand(),
            new UntgzCommand(),
            new UntxzCommand(),
            new UnzipBlobCommand(),
            new UnzipCommand(),
            new UnzipUrlCommand(),
            new VisualStudioEnvironmentCommand(),
            new WindowsSdkEnvironmentCommand(),
            new XCodeSetBuildSystemTypeCommand(),
            new ZipCommand(),
            new ZipToBlobCommand(),
        };
        foreach (var cmd in commands.OrderBy(_ => _.Command.Name)) rootCommand.Add(cmd.Command);

        return await rootCommand.InvokeAsync(args);
    }
}