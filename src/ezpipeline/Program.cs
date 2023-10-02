using System.CommandLine;
using Autofac;

namespace PipelineTools;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        //_environment.WriteLine("ezpipeline "+string.Join(" ", args));

        var filteredArgs = new List<string>();
        var doubleDash = false;
        var printArgs = false;
        foreach (var arg in args)
        {
            if (!doubleDash && !printArgs)
                if (arg == "--echo")
                {
                    Console.WriteLine("ezpipeline " + string.Join(" ", args));
                    printArgs = true;
                    continue;
                }

            if (arg == "--") doubleDash = true;
            filteredArgs.Add(arg);
        }

        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<DefaultPlatformEnvironment>().As<IPlatformEnvironment>().SingleInstance();
        containerBuilder.RegisterAssemblyTypes(typeof(Program).Assembly).AssignableTo(typeof(CommandBase))
            .As<CommandBase>();
        var container = containerBuilder.Build();

        var commands = container.Resolve<IEnumerable<CommandBase>>()
            //.OrderBy(_=>_.Command.Aliases.First())
            .ToArray();

        var rootCommand = new RootCommand();
        rootCommand.AddGlobalOption(new Option<bool>("--echo", "Print command line arguments to output"));

        foreach (var cmd in commands.OrderBy(_ => _.Command.Name)) rootCommand.Add(cmd.Command);

        return await rootCommand.InvokeAsync(filteredArgs.ToArray());
    }
}