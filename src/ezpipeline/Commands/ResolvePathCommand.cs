using PipelineTools;

namespace AzurePipelineTool.Commands;

public class ResolvePathCommand : AbstractCommand<ResolvePathCommand.Options>
{
    private readonly IPlatformEnvironment _environment;

    public ResolvePathCommand(IPlatformEnvironment environment) : base("resolve-path", "Resolve path")
    {
        _environment = environment;
    }

    public override Task HandleCommandAsync(Options options, CancellationToken cancellationToken)
    {
        var count = 0;
        string lastPath = null;
        var elements = PipelineUtils.ResolvePaths(options.Input).Take(options.Take);
        if (options.Directory) elements = elements.Select(_ => Path.GetDirectoryName(_)).Distinct();
        foreach (var resolvePath in elements)
        {
            _environment.WriteLine(resolvePath);
            ++count;
            lastPath = resolvePath;
        }

        if (count == 1)
            _environment.SetEnvironmentVariable(options.Variable, lastPath);
        else if (count == 0)
            throw new Exception("Can't set environment variable: no matching entries found");
        else
            throw new Exception("Can't set environment variable: multiple options found");

        return Task.CompletedTask;
    }

    public class Options
    {
        [CommandLineOption("-i", "Input file")]
        public string Input { get; set; }

        [CommandLineOption("-v", "Environment variable to set")]
        public string? Variable { get; set; }

        [CommandLineOption("-t", "Number of elements to take")]
        public int Take { get; set; } = int.MaxValue;

        [CommandLineOption("-d", "Take directory instead of elements found")]
        public bool Directory { get; set; }
    }
}