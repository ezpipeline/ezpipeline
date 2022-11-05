using System.Runtime.InteropServices;
using System.Text;

namespace PipelineTools;

public static class PipelineUtils
{
    public static Encoding UTF8 = new UTF8Encoding(false);

    public static PlatformID GetPlatformId()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return PlatformID.MacOSX;
        return Environment.OSVersion.Platform;
    }

    public static Stream CreateFile(string fileName)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(fileName)));
        return File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
    }

    public static Stream OpenFile(string fileName)
    {
        return File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    public static void SetEnvironmentVariable(string envName, string value)
    {
        Console.WriteLine($"Seting environment variable {envName} to {value}");
        Environment.SetEnvironmentVariable(envName, value, EnvironmentVariableTarget.Process);
        var githubEnv = Environment.GetEnvironmentVariable("GITHUB_ENV");
        Console.WriteLine($"##vso[task.setvariable variable={envName}]{value}");
    }

    public static void PrepandPath(string envName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        var existingValue = Environment.GetEnvironmentVariable(envName);
        var a = Environment.GetEnvironmentVariables();
        var visitedPath = new HashSet<string>();
        var combinedPaths = new List<string>();

        foreach (var s in value.Split(Path.PathSeparator).Where(_ => !string.IsNullOrWhiteSpace(_)))
            if (!Directory.Exists(s))
            {
                Console.Error.WriteLine($"Path not found: \"{s}\"");
            }
            else if (visitedPath.Add(s))
            {
                combinedPaths.Add(s);
                Console.WriteLine($"{envName}: Adding path \"{s}\"");
            }

        if (!string.IsNullOrWhiteSpace(existingValue))
            foreach (var s in existingValue.Split(Path.PathSeparator).Where(_ => !string.IsNullOrWhiteSpace(_)))
                if (visitedPath.Add(s))
                {
                    combinedPaths.Add(s);
                    Console.WriteLine($"{envName}: Existing path \"{s}\"");
                }

        var newValue = string.Join(Path.PathSeparator, combinedPaths);
        Environment.SetEnvironmentVariable(envName, newValue, EnvironmentVariableTarget.Process);
        Console.WriteLine($"##vso[task.setvariable variable={envName}]{newValue}");
    }

    public static string GetTempFileName(string? optionsTemp)
    {
        var tempFileName = optionsTemp;
        if (string.IsNullOrWhiteSpace(tempFileName))
        {
            tempFileName = Path.GetTempFileName();
        }
        else
        {
            if (!File.Exists(tempFileName))
            {
                if (Directory.Exists(tempFileName))
                {
                    tempFileName = Path.Combine(tempFileName, Guid.NewGuid().ToString());
                }
                else
                {
                    if (tempFileName.EndsWith("/") || tempFileName.EndsWith("\\"))
                    {
                        Directory.CreateDirectory(tempFileName);
                        tempFileName = Path.Combine(tempFileName, Guid.NewGuid().ToString());
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(tempFileName)));
                    }
                }
            }
        }

        if (File.Exists(tempFileName)) File.Delete(tempFileName);

        return tempFileName;
    }
}