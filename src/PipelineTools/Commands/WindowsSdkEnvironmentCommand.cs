using System.CommandLine;
using PipelineTools;
using static AzurePipelineTool.Commands.WindowsSdkEnvironmentCommand;

namespace AzurePipelineTool.Commands;

public class WindowsSdkEnvironmentCommand : AbstractCommand<WindowsSdkEnvironmentOptions>
{
    public WindowsSdkEnvironmentCommand() : base("winsdkenv")
    {
    }

    public override async Task HandleCommandAsync(WindowsSdkEnvironmentOptions options)
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            Console.WriteLine($"Can't setup VS on {Environment.OSVersion}");
            return;
        }

        var buildPlatform = options.BuildPlatform ?? "x64";

        var baseNetFxPath = @"C:\Program Files (x86)\Windows Kits\NETFXSDK";
        var netFxSdkFolder = Directory.GetDirectories(baseNetFxPath).OrderByDescending(_ => _).FirstOrDefault();
        PrepandPath("INCLUDE",
            string.Join(Path.PathSeparator, Directory.GetDirectories(Path.Combine(netFxSdkFolder, @"Include\um"))));
        PrepandPath("LIB",
            string.Join(Path.PathSeparator,
                Directory.GetDirectories(Path.Combine(netFxSdkFolder, @"Lib\um", buildPlatform))));

        var basePath = @"C:\Program Files (x86)\Windows Kits\10";

        var sdkFolder = Directory.GetDirectories(Path.Combine(basePath, @"bin"), "10.0.*").OrderByDescending(_ => _)
            .FirstOrDefault();

        var sdkVersion = Path.GetFileName(sdkFolder);
        SetEnvironmentVariable("WINDOWS_SDK_VERSION", sdkVersion);

        var pathsToAdd = new List<string>
        {
            Path.Combine(basePath, @"bin", sdkVersion, buildPlatform),
            Path.Combine(basePath, @"bin", buildPlatform)
        };
        PrepandPath("PATH", string.Join(Path.PathSeparator, pathsToAdd));


        var includeFolder = Path.Combine(basePath, "Include", sdkVersion);
        PrepandPath("INCLUDE", string.Join(Path.PathSeparator, Directory.GetDirectories(includeFolder)));

        var libFolder = Path.Combine(basePath, "Lib", sdkVersion);
        var umLib = Path.Combine(libFolder, @"um", buildPlatform);
        var ucrtLib = Path.Combine(libFolder, @"ucrt", buildPlatform);
        PrepandPath("LIB", $"{umLib}{Path.PathSeparator}{ucrtLib}");
    }

    public class WindowsSdkEnvironmentOptions
    {
        [CommandLineOption(description: "Build Platform")]
        public string? BuildPlatform { get; set; }
    }
}