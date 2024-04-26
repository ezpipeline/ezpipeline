using System.Text;
using System.Xml.Linq;
using ezpipeline.Commands;
using Xunit;
using Xunit.Abstractions;

namespace ezpipeline;

public class SetXmlValueCommandTests
{
    private readonly ITestOutputHelper _output;

    public SetXmlValueCommandTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task PatchXmlAttribute()
    {
        var xml =
            @"<?xml version=""1.0"" encoding=""utf-8""?>
<Package
  xmlns=""http://schemas.microsoft.com/appx/manifest/foundation/windows10""
  xmlns:mp=""http://schemas.microsoft.com/appx/2014/phone/manifest""
  xmlns:uap=""http://schemas.microsoft.com/appx/manifest/uap/windows10""
  IgnorableNamespaces=""uap mp"">

  <Identity
    Name=""39ad144a-075c-4e1b-9d81-1a2e4672a258""
    Version=""0.0.1.0"" />
</Package>";

        var fileName = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(fileName, xml, new UTF8Encoding(false));

            var command = new SetXmlValueCommand(new MockPlatformEnvironment(_output));
            await command.HandleCommandAsync(new SetXmlValueCommand.Options()
            {
                Input = fileName,
                Path = "default:Package/default:Identity",
                Attribute = "Version",
                Value = "1.2.3.4"
            }, CancellationToken.None);

            var xdoc = XDocument.Load(fileName);
            var xElement = xdoc.Root.Element(XName.Get("Identity", "http://schemas.microsoft.com/appx/manifest/foundation/windows10"));
            Assert.Equal("1.2.3.4", xElement.Attribute(XName.Get("Version")).Value);
        }
        finally
        {
            File.Delete(fileName);
        }
    }
}