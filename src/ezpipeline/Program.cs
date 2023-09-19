using System.CommandLine;
using AzurePipelineTool.Commands;

namespace PipelineTools;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        //Console.WriteLine("ezpipeline "+string.Join(" ", args));

        List<string> filteredArgs = new List<string>();
        bool doubleDash = false;
        bool printArgs = false;
        foreach (var arg in args)
        {
            if (!doubleDash && !printArgs)
            {
                if (arg == "--echo")
                {
                    Console.WriteLine("ezpipeline " + string.Join(" ", args));
                    printArgs = true;
                    continue;
                }
            }

            if (arg == "--")
            {
                doubleDash = true;
            }
            filteredArgs.Add(arg);
        }

        var rootCommand = new RootCommand();
        rootCommand.AddGlobalOption(new Option<bool>("--echo", description:"Print command line arguments to output"));

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

        return await rootCommand.InvokeAsync(filteredArgs.ToArray());
    }
}