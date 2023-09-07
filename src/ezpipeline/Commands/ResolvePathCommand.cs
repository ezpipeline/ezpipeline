﻿using PipelineTools;

namespace AzurePipelineTool.Commands;

public class ResolvePathCommand : AbstractCommand<ResolvePathCommand.Options>
{
    public ResolvePathCommand() : base("resolve-path", "Resolve path")
    {
    }

    public override Task HandleCommandAsync(Options options, CancellationToken cancellationToken)
    {
        int count = 0;
        string lastPath = null;
        foreach (var resolvePath in PipelineUtils.ResolvePaths(options.Input))
        {
            Console.WriteLine(resolvePath);
            ++count;
            lastPath = resolvePath;
        }

        if (count == 1)
        {
            PipelineUtils.SetEnvironmentVariable(options.Variable, lastPath);
        }
        else if (count == 0)
        {
            throw new Exception("Can't set environment variable: no matching entries found");
        }
        else
        {
            throw new Exception("Can't set environment variable: multiple options found");
        }

        return Task.CompletedTask;
    }

    public class Options
    {
        [CommandLineOption("-i", "Input file")]
        public string Input { get; set; }

        [CommandLineOption("-v", "Environment variable to set")]
        public string? Variable { get; set; }
    }
}