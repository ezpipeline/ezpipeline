using System.Globalization;
using System.Runtime.InteropServices;
using PipelineTools;
using static AzurePipelineTool.Commands.GetCPUInfoCommand;

namespace AzurePipelineTool.Commands;

public class GetCPUInfoCommand : AbstractCommand<GetNumberOfProcessorsOptions>
{
    public GetCPUInfoCommand() : base("cpu-info", "Get CPU info")
    {
    }

    public enum CpuInfo
    {
        Count,
        Arch
    }

    public override Task HandleCommandAsync(GetNumberOfProcessorsOptions options,
        CancellationToken cancellationToken)
    {
        var result = "";
        switch (options.Info)
        {
            case CpuInfo.Count:
                result = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture);
                break;
            case CpuInfo.Arch:
                result = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
                break;
        }

        Console.WriteLine(result);
        PipelineUtils.SetEnvironmentVariable(options.Variable, result);
        return Task.CompletedTask;
    }

    public class GetNumberOfProcessorsOptions
    {
        [CommandLineOption("-i", "CPU property")]
        public CpuInfo Info { get; set; }

        [CommandLineOption("-v", "Environment variable to set")]
        public string? Variable { get; set; }
    }
}