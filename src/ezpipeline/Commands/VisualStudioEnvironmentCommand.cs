using PipelineTools;
using static AzurePipelineTool.Commands.VisualStudioEnvironmentCommand;

namespace AzurePipelineTool.Commands;

public class VisualStudioEnvironmentCommand : AbstractCommand<VisualStudioEnvironmentOptions>
{
    private readonly IPlatformEnvironment _environment;

    public VisualStudioEnvironmentCommand(IPlatformEnvironment environment) : base("vsenv",
        "Setup VisualStudio environment variables")
    {
        _environment = environment;
    }

    public override async Task HandleCommandAsync(VisualStudioEnvironmentOptions options,
        CancellationToken cancellationToken)
    {
        if (_environment.GetPlatformId() != PlatformIdentifier.Windows)
        {
            _environment.WriteErrorLine($"Can't setup VS on {Environment.OSVersion}");
            return;
        }

        var vsLocation = RunProcess(@"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe", "-latest",
            "-property", "installationPath");
        _environment.WriteLine($"Visual Studio found at: {vsLocation}");

        var toolsVersion = File
            .ReadAllText(Path.Combine(vsLocation, @"VC\Auxiliary\Build\Microsoft.VCToolsVersion.default.txt")).Trim();
        _environment.SetEnvironmentVariable("MSVC_TOOLS_VERSION", toolsVersion);

        var toolsFolder = Path.Combine(vsLocation, @"VC\Tools\MSVC", toolsVersion);

        var buildPlatform = options.BuildPlatform ?? "x64";

        var pathsToAdd = new List<string>
        {
            Path.Combine(vsLocation, @"Common7\IDE\Extensions\Microsoft\IntelliCode\CLI"),
            Path.Combine(vsLocation, @"VC\Tools\MSVC", toolsVersion, "bin", "HostX64", buildPlatform),
            Path.Combine(vsLocation, @"Common7\IDE\VC\VCPackages"),
            Path.Combine(vsLocation, @"Common7\IDE\CommonExtensions\Microsoft\TestWindow"),
            Path.Combine(vsLocation, @"Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer"),
            Path.Combine(vsLocation, @"MSBuild\Current\bin\Roslyn"),
            Path.Combine(vsLocation, @"Team Tools\Performance Tools"),
            Path.Combine(vsLocation, @"Common7\IDE\CommonExtensions\Microsoft\FSharp\Tools"),
            Path.Combine(vsLocation, @"Common7\Tools\devinit"),
            Path.Combine(vsLocation, @"MSBuild\Current\Bin"),
            Path.Combine(vsLocation, @"Common7\IDE\"),
            Path.Combine(vsLocation, @"Common7\Tools\"),
            Path.Combine(vsLocation, @"Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin"),
            Path.Combine(vsLocation, @"Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja"),
            Path.Combine(vsLocation, @"Common7\IDE\VC\Linux\bin\ConnectionManagerExe")
        };
        _environment.PrepandPath("PATH", string.Join(Path.PathSeparator, pathsToAdd));

        var include = Path.Combine(toolsFolder, @"include");
        var atlmfcInclude = Path.Combine(toolsFolder, @"atlmfc\include");
        _environment.PrepandPath("INCLUDE", $"{include}{Path.PathSeparator}{atlmfcInclude}");


        var lib = Path.Combine(toolsFolder, @"lib", buildPlatform);
        var atlmfcLib = Path.Combine(toolsFolder, @"atlmfc\lib", buildPlatform);
        _environment.PrepandPath("LIB", $"{lib}{Path.PathSeparator}{atlmfcLib}");
    }

    public class VisualStudioEnvironmentOptions
    {
        [CommandLineOption(description: "Build Platform")]
        public string? BuildPlatform { get; set; }
    }
}