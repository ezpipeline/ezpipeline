using PipelineTools;
using static AzurePipelineTool.Commands.GetNumberOfProcessorsCommand;

namespace AzurePipelineTool.Commands;

public class GetNumberOfProcessorsCommand : AbstractCommand<GetNumberOfProcessorsOptions>
{
    public GetNumberOfProcessorsCommand() : base("processorcount")
    {
    }

    public override void HandleCommand(GetNumberOfProcessorsOptions invocationContext)
    {
        Console.WriteLine(Environment.ProcessorCount);
    }

    public class GetNumberOfProcessorsOptions
    {
    }
}