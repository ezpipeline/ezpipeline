using System.Xml.Linq;
using PipelineTools;

namespace AzurePipelineTool.Commands;

public class GetMSBuildPropertyCommand : AbstractCommand<GetMSBuildPropertyCommand.Options>
{
    private readonly IPlatformEnvironment _environment;

    public GetMSBuildPropertyCommand(IPlatformEnvironment environment) : base("get-msbuild-property",
        "Get property from msbuild file (*.csproj, *.props)")
    {
        _environment = environment;
    }

    public override async Task HandleCommandAsync(Options options, CancellationToken cancellationToken)
    {
        XDocument document = null;
        if (File.Exists(options.Input))
            await using (var fileStream = PipelineUtils.OpenFile(options.Input))
            {
                document = await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken);
            }

        var propName = XName.Get(options.Property, "");
        var properties = document.Descendants(XName.Get("PropertyGroup", "")).SelectMany(_=>_.Descendants()).Where(_ => _.Name == propName).ToList();

        if (properties.Count == 0)
        {
            throw new Exception($"Property <{options.Property}/> not found");
        }
        if (properties.Count > 1)
        {
            throw new Exception($"More than one property <{options.Property}/> found");
        }

        var propertyValue = properties[0].Value.Trim();

        _environment.WriteLine(propertyValue);
        _environment.SetEnvironmentVariable(options.Variable, propertyValue);
    }

    public class Options
    {
        [CommandLineOption("-i", "Project or properties xml file (*.csproj, *.props)")]
        public string Input { get; set; }

        [CommandLineOption("-p", "Property to get")]
        public string Property { get; set; }

        [CommandLineOption("-v", "Environment variable to set")]
        public string? Variable { get; set; }
    }
}