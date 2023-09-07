using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using AzurePipelineTool.Commands;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace ezpipeline
{
    public class XCodeSetBuildSystemTypeTests
    {
        private readonly ITestOutputHelper _output;

        public XCodeSetBuildSystemTypeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GenerateReadableXml()
        {
            var command = new XCodeSetBuildSystemTypeCommand();
            var res = command.Patch(new XCodeSetBuildSystemTypeCommand.XCodeSetBuildSystemTypeOptions(){BuildSystemType = "Original" },
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN""""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
	<dict>
		<key>BuildSystemType</key>
		<string>Original</string>
		<key>DisableBuildSystemDeprecationWarning</key>
		<true/>
	</dict>
</plist>");
            _output.WriteLine(res);
            var doc = XDocument.Parse(res);
        }
    }
}
