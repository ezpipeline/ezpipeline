using System.Text;
using System.Xml.Linq;
using PipelineTools;

namespace AzurePipelineTool.Commands;

public class SetMSBuildPropertyCommand : AbstractCommand<SetMSBuildPropertyCommand.Options>
{
    public SetMSBuildPropertyCommand() : base("set-msbuild-property",
        "Set property in msbuild file (*.csproj, *.props)")
    {
    }

    public override async Task HandleCommandAsync(Options options, CancellationToken cancellationToken)
    {
        XDocument document = null;
        if (File.Exists(options.Input))
            await using (var fileStream = PipelineUtils.OpenFile(options.Input))
            {
                document = await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken);
            }

        if (document == null)
            document = XDocument.Parse("<Project>\r\n  <PropertyGroup>\r\n  </PropertyGroup>\r\n</Project>\r\n");

        var propertyGroupName = XName.Get("PropertyGroup", "");
        var propertyGroups = document.Descendants(propertyGroupName).ToList();
        if (propertyGroups.Count == 0)
        {
            var group = new XElement(propertyGroupName);
            document.Root.Add(group);
            propertyGroups.Add(group);
        }

        foreach (var propertyKeyValue in options.Property)
        {
            var separatorIndex = propertyKeyValue.IndexOf(":");
            if (separatorIndex < 0)
                throw new ArgumentException($"Invalid property format: {propertyKeyValue}");

            var propertyValue = propertyKeyValue.Substring(separatorIndex + 1);
            var propertyName = XName.Get(propertyKeyValue.Substring(0, separatorIndex), "");
            foreach (var propertyGroup in propertyGroups)
            foreach (var xElement in propertyGroup.Elements(propertyName).ToList())
                propertyGroups.Remove(xElement);

            propertyGroups[0].Add(new XElement(propertyName, new XText(propertyValue)));
        }

        using (var fileStream = PipelineUtils.CreateFile(options.Input))
        {
            using (var writer = new StreamWriter(fileStream, new UTF8Encoding(false)))
            {
                await document.SaveAsync(writer, SaveOptions.None, cancellationToken);
            }
        }
    }

    public class Options
    {
        [CommandLineOption("-i", "Project or properties xml file (*.csproj, *.props)")]
        public string Input { get; set; }

        [CommandLineOption("-p", "Property to set")]
        public List<string> Property { get; set; } = new();
    }
}