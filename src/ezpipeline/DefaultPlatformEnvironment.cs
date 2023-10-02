using System.Runtime.InteropServices;
using System.Text;

namespace PipelineTools;

public class DefaultPlatformEnvironment : IPlatformEnvironment
{
    public static Encoding UTF8NoIdentifier = new UTF8Encoding(false);

    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }

    public void WriteErrorLine(string message)
    {
        Console.Error.WriteLine(message);
    }

    public PlatformIdentifier GetPlatformId()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return PlatformIdentifier.MacOSX;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return PlatformIdentifier.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return PlatformIdentifier.Linux;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            return PlatformIdentifier.FreeBSD;

        return PlatformIdentifier.Unknown;
    }

    public void SetEnvironmentVariable(string envName, string value)
    {
        if (string.IsNullOrWhiteSpace(envName))
            return;

        WriteLine($"Setting environment variable {envName} to {value}");
        Environment.SetEnvironmentVariable(envName, value, EnvironmentVariableTarget.Process);
        var githubEnv = Environment.GetEnvironmentVariable("GITHUB_ENV");
        if (string.IsNullOrWhiteSpace(githubEnv))
            WriteLine($"##vso[task.setvariable variable={envName}]{value}");
        else
            using (var file = PipelineUtils.AppendFile(githubEnv))
            {
                using (var writer = new StreamWriter(file, UTF8NoIdentifier))
                {
                    writer.WriteLine($"{envName}={value}");
                }
            }
    }

    public string? GetEnvironmentVariable(string envName)
    {
        return Environment.GetEnvironmentVariable(envName);
    }
}