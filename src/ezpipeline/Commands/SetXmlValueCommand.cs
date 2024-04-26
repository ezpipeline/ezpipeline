using System.Reflection.PortableExecutable;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using PipelineTools;

namespace ezpipeline.Commands
{
    public class SetXmlValueCommand : AbstractCommand<SetXmlValueCommand.Options>
    {
        private readonly IPlatformEnvironment _environment;

        public class Options
        {
            [CommandLineOption("-i", "Input file")]
            public string Input { get; set; }

            [CommandLineOption("-p", description: "XPath query")]
            public string Path { get; set; }

            [CommandLineOption("-a", description: "Attribute name")]
            public string Attribute { get; set; }
            
            [CommandLineOption("-v", description: "Value")]
            public string Value { get; set; }
        }

        public SetXmlValueCommand(IPlatformEnvironment environment) : base("set-xml", "Set element or attribute value in xml document")
        {
            _environment = environment;
        }

        public override async Task HandleCommandAsync(Options options, CancellationToken cancellationToken)
        {
            foreach (var path in PipelineUtils.ResolvePaths(options.Input))
            {
                XDocument xDocument;
                using (var stream = PipelineUtils.OpenFile(path))
                {
                    xDocument = await XDocument.LoadAsync(stream, LoadOptions.PreserveWhitespace, cancellationToken);

                    var namespaceManager = new XmlNamespaceManager(new NameTable());
                    
                    if (xDocument.Root != null)
                    {
                        foreach (var xAttribute in xDocument.Root.Attributes().Where(_=>_.IsNamespaceDeclaration))
                        {
                            var localName = xAttribute.Name.LocalName;
                            if (localName.StartsWith("xmlns"))
                            {
                                if (localName == "xmlns")
                                {
                                   namespaceManager.AddNamespace("", xAttribute.Value);
                                }
                                else
                                {
                                    namespaceManager.AddNamespace(localName, xAttribute.Value);
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(namespaceManager.DefaultNamespace) &&
                            !namespaceManager.HasNamespace("default"))
                        {
                            namespaceManager.AddNamespace("default", namespaceManager.DefaultNamespace);
                        }
                    }

                    var elements = xDocument.XPathSelectElements(options.Path, namespaceManager).ToList();
                    if (elements.Count > 0)
                    {
                        foreach (var xElement in elements)
                        {

                            if (string.IsNullOrWhiteSpace(options.Attribute))
                            {
                                xElement.Value = options.Value;
                                _environment.WriteLine(
                                    $"{xElement.ToXPathNavigable()}.{options.Attribute} = {options.Value}");
                            }
                            else
                            {
                                xElement.SetAttributeValue(XName.Get(options.Attribute), options.Value);
                                _environment.WriteLine($"{xElement.ToXPathNavigable()} = {options.Value}");
                            }
                        }
                    }
                    else
                    {
                        throw new Exception($"No elements found at path {options.Path}");
                    }
                }
                using (var stream = PipelineUtils.CreateFile(path))
                {
                    await xDocument.SaveAsync(stream, SaveOptions.None, cancellationToken);
                }
            }
        }
    }
}
