using System.CommandLine;

namespace PipelineTools;

public abstract class CommandBase
{
    public abstract Command Command { get; }
}