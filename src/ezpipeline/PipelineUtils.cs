using System.Runtime.InteropServices;
using System.Text;

namespace PipelineTools;

public static class PipelineUtils
{
    public static Encoding UTF8 = new UTF8Encoding(false);

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
            var isLastSegment = index == segments.Length - 1;
            if (segment == "**")
            {
                foreach (var folder in lookUpFolders)
                {
                    nextFolders.Add(folder);
                    foreach (var dir in Directory.GetDirectories(folder, "*", SearchOption.AllDirectories))
                        nextFolders.Add(dir + Path.DirectorySeparatorChar);
                }
            }
            else if (segment.Contains('*') || segment.Contains('?'))
            {
                if (isLastSegment)
                    foreach (var folder in lookUpFolders)
                    foreach (var file in Directory.GetFiles(folder, segment))
                        yield return file;

                foreach (var folder in lookUpFolders)
                foreach (var subdir in Directory.GetDirectories(folder, segment))
                    nextFolders.Add(subdir + Path.DirectorySeparatorChar);
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

        foreach (var lookUpFolder in lookUpFolders) yield return lookUpFolder;
    }

    public static string ResolvePath(string path)
    {
        var hasResult = false;
        var result = Path.GetFullPath(path);
        foreach (var resolvedPath in ResolvePaths(path).Distinct())
        {
            if (hasResult)
                throw new Exception(
                    $"Can't resolve path {path} because of multiple options: {result}, {resolvedPath}...");

            result = resolvedPath;
        }

        return result;
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
        return File.Open(ResolvePath(fileName), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    public static Stream AppendFile(string fileName)
    {
        return File.Open(ResolvePath(fileName), FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
    }

    public static bool ValidateDirectory(IPlatformEnvironment environment, string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (Directory.Exists(fullPath))
            return true;

        path = fullPath;
        for (;;)
        {
            var parent = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(parent)) break;

            if (Directory.Exists(parent))
            {
                var options = string.Join(", ", Directory.GetDirectories(parent).Select(Path.GetFileName));
                environment.WriteErrorLine(
                    $"Path not found: \"{path}\", missing \"{Path.GetFileName(fullPath)}\", available {options}");
                return false;
            }

            fullPath = parent;
        }

        environment.WriteErrorLine($"Path not found: \"{path}\"");

        return false;
    }

    public static void PrepandPath(this IPlatformEnvironment environment, string envName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        var existingValue = environment.GetEnvironmentVariable(envName);
        var visitedPath = new HashSet<string>();
        var combinedPaths = new List<string>();

        foreach (var s in value.Split(Path.PathSeparator).Where(_ => !string.IsNullOrWhiteSpace(_)))
        {
            var fullPath = Path.GetFullPath(s);
            if (ValidateDirectory(environment, fullPath))
                if (visitedPath.Add(s))
                    combinedPaths.Add(s);
                //_environment.WriteLine($"{envName}: Adding path \"{s}\"");
        }

        if (!string.IsNullOrWhiteSpace(existingValue))
            foreach (var s in existingValue.Split(Path.PathSeparator).Where(_ => !string.IsNullOrWhiteSpace(_)))
                if (visitedPath.Add(s))
                    combinedPaths.Add(s);
                //_environment.WriteLine($"{envName}: Existing path \"{s}\"");

        var newValue = string.Join(Path.PathSeparator, combinedPaths);
        environment.SetEnvironmentVariable(envName, newValue);
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

    public static void MakeExecutable(string binFile)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }
#if NET7_0_OR_GREATER
        foreach (var resolvePath in ResolvePaths(binFile))
        {
            if (File.Exists(resolvePath))
            {
                var mode = File.GetUnixFileMode(resolvePath);
                var newmode = mode | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute | UnixFileMode.UserExecute;
                if (newmode != mode)
                {
                    File.SetUnixFileMode(resolvePath, newmode);
                    Console.WriteLine($"File {resolvePath} is executable now");
                }
            }
            else
            {
                Console.WriteLine($"File {resolvePath} not found");
            }
        }
#else
        Console.WriteLine("Changing file mode only available in NET7.0 and newer");
#endif
    }
}