using System.Text;
using System.Xml;
using System.Xml.Linq;
using PipelineTools;
using static AzurePipelineTool.Commands.XCodeSetBuildSystemTypeCommand;

namespace AzurePipelineTool.Commands;

public class XCodeSetBuildSystemTypeCommand : AbstractCommand<XCodeSetBuildSystemTypeOptions>
{
    private readonly IPlatformEnvironment _environment;

    public XCodeSetBuildSystemTypeCommand(IPlatformEnvironment environment) : base("xcode-setbuildsystemtype",
        "Patch XCode build system workspace property")
    {
        _environment = environment;
    }

    public override async Task HandleCommandAsync(XCodeSetBuildSystemTypeOptions options,
        CancellationToken cancellationToken)
    {
        var xcodeProjectDir = options.Input;
        if (string.IsNullOrWhiteSpace(xcodeProjectDir)) throw new ArgumentException("Missing --input argument");

        foreach (var file in Directory.GetFiles(xcodeProjectDir, "WorkspaceSettings.xcsettings",
                     SearchOption.AllDirectories))
        {
            _environment.WriteLine($"Patching {file}");
            var xmlText = await File.ReadAllTextAsync(file);
            try
            {
                await File.WriteAllTextAsync(file, Patch(options, xmlText), new UTF8Encoding(false));
            }
            catch (XmlException ex)
            {
                _environment.WriteErrorLine(ex.Message);
                _environment.WriteErrorLine(xmlText);
                return;
            }
        }
    }

    public string Patch(XCodeSetBuildSystemTypeOptions options, string xmlText)
    {
        var buildsystemtypeValue = options.BuildSystemType;
        var keyName = XName.Get("key");

        xmlText = xmlText.Replace("EN\"\"http:", "EN\" \"http:");
        var doc = XDocument.Parse(xmlText);

        var dictElement = doc.Descendants(XName.Get("dict")).FirstOrDefault();
        var elements = dictElement.Elements().ToArray();
        var dict = new Dictionary<string, XElement>();
        for (var index = 0; index < elements.Length; index++)
        {
            var keyElement = elements[index];
            if (keyElement.Name == keyName) dict[keyElement.Value] = elements[index + 1];
        }

        if (!dict.TryGetValue("BuildSystemType", out var buildSystemType))
        {
            dictElement.Add(new XElement(keyName, "BuildSystemType"));
            dictElement.Add(new XElement(XName.Get("string"), buildsystemtypeValue));
        }
        else
        {
            buildSystemType.Value = buildsystemtypeValue;
        }

        if (!dict.TryGetValue("DisableBuildSystemDeprecationDiagnostic",
                out var disableBuildSystemDeprecationDiagnostic))
        {
            dictElement.Add(new XElement(keyName, "DisableBuildSystemDeprecationDiagnostic"));
            dictElement.Add(new XElement(XName.Get("true")));
        }

        doc.DocumentType.InternalSubset = null;
        xmlText = doc.ToString();
        return xmlText;
    }

    public class XCodeSetBuildSystemTypeOptions
    {
        [CommandLineOption("-i", "Xcode application folder")]
        public string Input { get; set; }

        [CommandLineOption("-b", "Build system type")]
        public string BuildSystemType { get; set; } = "Original";
    }
}