using System.Globalization;
using System.Runtime.InteropServices;
using PipelineTools;
using static AzurePipelineTool.Commands.GetCPUInfoCommand;

namespace AzurePipelineTool.Commands;

public class GetCPUInfoCommand : AbstractCommand<GetNumberOfProcessorsOptions>
{
    private readonly IPlatformEnvironment _environment;

    public GetCPUInfoCommand(IPlatformEnvironment environment) : base("cpu-info", "Get CPU info")
    {
        _environment = environment;
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

        _environment.WriteLine(result);
        _environment.SetEnvironmentVariable(options.Variable, result);
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