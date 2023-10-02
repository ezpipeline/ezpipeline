using System.Runtime.InteropServices;
using PipelineTools;
using Xunit.Abstractions;

namespace ezpipeline;

public class MockPlatformEnvironment : IPlatformEnvironment
{
    private readonly ITestOutputHelper _helper;
    private List<string> _output = new List<string>();
    private List<string> _error = new List<string>();
    private Dictionary<string, string> _env = new Dictionary<string, string>();

    public MockPlatformEnvironment(ITestOutputHelper helper)
    {
        _helper = helper;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            PlatformId = PlatformIdentifier.MacOSX;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            PlatformId = PlatformIdentifier.Windows;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            PlatformId = PlatformIdentifier.Linux;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            PlatformId = PlatformIdentifier.FreeBSD;
        else
            PlatformId = PlatformIdentifier.Unknown;
    }
    public void WriteLine(string message)
    {
        if (_helper != null)
            _helper.WriteLine(message);
        _output.Append(message);
    }

    public void WriteErrorLine(string message)
    {
        if (_helper != null)
            _helper.WriteLine(message);
        _error.Append(message);
    }

    public PlatformIdentifier PlatformId { get; set; }

    PlatformIdentifier IPlatformEnvironment.GetPlatformId()
    {
        return PlatformId;
    }

    public void SetEnvironmentVariable(string envName, string value)
    {
        _env[envName] = value;
    }

    public string? GetEnvironmentVariable(string envName)
    {
        if (_env.TryGetValue(envName, out string val))
            return val;
        return null;
    }
}