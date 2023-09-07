using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PipelineTools;

public static class PipelineUtils
{
    public static Encoding UTF8 = new UTF8Encoding(false);

    public static PlatformIdentifier GetPlatformId()
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

    public static IEnumerable<string> ResolvePaths(string path)
    {
        path = Path.GetFullPath(path);
        if (!path.Contains("?") && !path.Contains("*"))
        {
            yield return path;
            yield break;
        }

        var lookUpFolders = new HashSet<string>();
        var nextFolders = new HashSet<string>();

        var segments = path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (path.StartsWith('\\') || path.StartsWith('/'))
            lookUpFolders.Add(path.Substring(0, 1));
        else
            lookUpFolders.Add("");

        for (var index = 0; index < segments.Length && lookUpFolders.Count > 0; index++)
        {
            var segment = segments[index];
            bool isLastSegment = index == segments.Length - 1;
            if (segment == "**")
            {
                foreach (var folder in lookUpFolders)
                {
                    nextFolders.Add(folder);
                    foreach (var dir in Directory.GetDirectories(folder, "*", SearchOption.AllDirectories))
                    {
                        nextFolders.Add(dir + Path.DirectorySeparatorChar);
                    }
                }
            }
            else if (segment.Contains('*') || segment.Contains('?'))
            {
                if (isLastSegment)
                {
                    foreach (var folder in lookUpFolders)
                    {
                        foreach (var file in Directory.GetFiles(folder, segment))
                        {
                            yield return file;
                        }
                    }
                }

                foreach (var folder in lookUpFolders)
                {
                    foreach (var subdir in Directory.GetDirectories(folder, segment))
                    {
                        nextFolders.Add(subdir + Path.DirectorySeparatorChar);
                    }
                }
            }
            else
            {
                foreach (var folder in lookUpFolders)
                {
                    var combined = Path.Combine(folder, segment);
                    if (File.Exists(combined))
                        nextFolders.Add(combined);
                    else if (Directory.Exists(combined))
                        nextFolders.Add(combined + Path.DirectorySeparatorChar);
                }
            }

            (lookUpFolders, nextFolders) = (nextFolders, lookUpFolders);
            nextFolders.Clear();
        }

        foreach (var lookUpFolder in lookUpFolders)
        {
            yield return lookUpFolder;
        }
    }

    public static string? ResolvePath(string path)
    {
        bool hasResult = false;
        string result = Path.GetFullPath(path);
        foreach (var resolvedPath in ResolvePaths(path).Distinct())
        {
            if (hasResult)
            {
                throw new Exception($"Can't resolve path {path} because of multiple options: {result}, {resolvedPath}...");
            }
        }

        return Path.GetFullPath(path);
    }

    public static Stream OpenOrCreateFile(string fileName)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(fileName)) ?? string.Empty);
        return File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
    }

    public static Stream CreateFile(string fileName)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(fileName)) ?? string.Empty);
        return File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
    }

    public static Stream OpenFile(string fileName)
    {
        return File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    private static Stream AppendFile(string fileName)
    {
        return File.Open(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
    }


    public static void SetEnvironmentVariable(string envName, string value)
    {
        if (string.IsNullOrWhiteSpace(envName))
            return;

        Console.WriteLine($"Setting environment variable {envName} to {value}");
        Environment.SetEnvironmentVariable(envName, value, EnvironmentVariableTarget.Process);
        var githubEnv = Environment.GetEnvironmentVariable("GITHUB_ENV");
        if (string.IsNullOrWhiteSpace(githubEnv))
        {
            Console.WriteLine($"##vso[task.setvariable variable={envName}]{value}");
        }
        else
        {
            using (var file = AppendFile(githubEnv))
            {
                using (var writer = new StreamWriter(file, UTF8))
                {
                    writer.WriteLine($"{envName}={value}");
                }
            }
        }
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
                //Console.WriteLine($"{envName}: Adding path \"{s}\"");
            }

        if (!string.IsNullOrWhiteSpace(existingValue))
            foreach (var s in existingValue.Split(Path.PathSeparator).Where(_ => !string.IsNullOrWhiteSpace(_)))
                if (visitedPath.Add(s))
                {
                    combinedPaths.Add(s);
                    //Console.WriteLine($"{envName}: Existing path \"{s}\"");
                }

        var newValue = string.Join(Path.PathSeparator, combinedPaths);
        SetEnvironmentVariable(envName, newValue);
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