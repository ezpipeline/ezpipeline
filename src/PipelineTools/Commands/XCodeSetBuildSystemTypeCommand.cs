using System.Xml;
using System.Xml.Linq;
using PipelineTools;
using static AzurePipelineTool.Commands.XCodeSetBuildSystemTypeCommand;

namespace AzurePipelineTool.Commands;

public class XCodeSetBuildSystemTypeCommand : AbstractCommand<XCodeSetBuildSystemTypeOptions>
{
    public XCodeSetBuildSystemTypeCommand() : base("xcode-setbuildsystemtype")
    {
    }

    public override async Task HandleCommandAsync(XCodeSetBuildSystemTypeOptions options, CancellationToken cancellationToken)
    {
        var xcodeProjectDir = options.Input;
        if (string.IsNullOrWhiteSpace(xcodeProjectDir)) throw new ArgumentException("Missing --input argument");

        var buildsystemtypeValue = options.BuildSystemType;

        var keyName = XName.Get("key");
        foreach (var file in Directory.GetFiles(xcodeProjectDir, "WorkspaceSettings.xcsettings",
                     SearchOption.AllDirectories))
        {
            Console.WriteLine($"Patching {file}");
            XDocument doc;
            var xmlText = File.ReadAllText(file);
            xmlText = xmlText.Replace("EN\"\"http:", "EN\" \"http:");
            try
            {
                doc = XDocument.Parse(xmlText);
            }
            catch (XmlException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(xmlText);
                continue;
            }

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

            doc.Save(file);
        }
    }

    public class XCodeSetBuildSystemTypeOptions
    {
        [CommandLineOption("-i", "Xcode application folder")]
        public string Input { get; set; }

        [CommandLineOption("-b", "Build system type")]
        public string BuildSystemType { get; set; } = "Original";
    }
}