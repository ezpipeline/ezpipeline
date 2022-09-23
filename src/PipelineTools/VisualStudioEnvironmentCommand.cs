using System.CommandLine;
using System.CommandLine.Invocation;

namespace PipelineTools;

public class VisualStudioEnvironmentCommand: AbstractCommand
{
    private Option<string> _buildPlatform;

    public VisualStudioEnvironmentCommand():base("vsenv")
    {
        _buildPlatform = new Option<string>("--buildPlatform", "Build Platform");
        Command.Add(_buildPlatform);
    }
    public override void HandleCommand(InvocationContext invocationContext)
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            Console.WriteLine($"Can't setup VS on {Environment.OSVersion}");
            return;
        }

        var vsLocation = RunProcess(@"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe", "-latest", "-property", "installationPath");
        Console.WriteLine($"Visual Studio found at: {vsLocation}");

        var toolsVersion = File.ReadAllText(Path.Combine(vsLocation, @"VC\Auxiliary\Build\Microsoft.VCToolsVersion.default.txt")).Trim();
        SetEnvironmentVariable("MSVC_TOOLS_VERSION", toolsVersion);

        var toolsFolder = Path.Combine(vsLocation, @"VC\Tools\MSVC", toolsVersion);

        var buildPlatform = GetValueForHandlerParameter(_buildPlatform, invocationContext) ?? "x64";

        var pathsToAdd = new List<string>
        {
            Path.Combine(vsLocation,@"Common7\IDE\Extensions\Microsoft\IntelliCode\CLI"),
            Path.Combine(vsLocation,@"VC\Tools\MSVC",toolsVersion,"bin","HostX64",buildPlatform),
            Path.Combine(vsLocation,@"Common7\IDE\VC\VCPackages"),
            Path.Combine(vsLocation,@"Common7\IDE\CommonExtensions\Microsoft\TestWindow"),
            Path.Combine(vsLocation,@"Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer"),
            Path.Combine(vsLocation,@"MSBuild\Current\bin\Roslyn"),
            Path.Combine(vsLocation,@"Team Tools\Performance Tools"),
            Path.Combine(vsLocation,@"Common7\IDE\CommonExtensions\Microsoft\FSharp\Tools"),
            Path.Combine(vsLocation,@"Common7\Tools\devinit"),
            Path.Combine(vsLocation,@"MSBuild\Current\Bin"),
            Path.Combine(vsLocation,@"Common7\IDE\"),
            Path.Combine(vsLocation,@"Common7\Tools\"),
            Path.Combine(vsLocation,@"Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin"),
            Path.Combine(vsLocation,@"Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja"),
            Path.Combine(vsLocation,@"Common7\IDE\VC\Linux\bin\ConnectionManagerExe"),

        };
        PrepandPath("PATH", string.Join(Path.PathSeparator, pathsToAdd));

        var include = Path.Combine(toolsFolder, @"include");
        var atlmfcInclude = Path.Combine(toolsFolder, @"atlmfc\include");
        PrepandPath("INCLUDE", $"{include}{Path.PathSeparator}{atlmfcInclude}");


        var lib = Path.Combine(toolsFolder, @"lib", buildPlatform);
        var atlmfcLib = Path.Combine(toolsFolder, @"atlmfc\lib", buildPlatform);
        PrepandPath("LIB", $"{lib}{Path.PathSeparator}{atlmfcLib}");
    }

}