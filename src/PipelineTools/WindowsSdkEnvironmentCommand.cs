using System.CommandLine;
using System.CommandLine.Invocation;

namespace PipelineTools;

public class WindowsSdkEnvironmentCommand : AbstractCommand
{
    private Option<string> _buildPlatform;

    public WindowsSdkEnvironmentCommand() : base("winsdkenv")
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

        var buildPlatform = GetValueForHandlerParameter(_buildPlatform, invocationContext) ?? "x64";

        var baseNetFxPath = @"C:\Program Files (x86)\Windows Kits\NETFXSDK";
        var netFxSdkFolder = Directory.GetDirectories(baseNetFxPath).OrderByDescending(_ => _).FirstOrDefault();
        PrepandPath("INCLUDE", string.Join(Path.PathSeparator, Directory.GetDirectories(Path.Combine(netFxSdkFolder, @"Include\um"))));
        PrepandPath("LIB", string.Join(Path.PathSeparator, Directory.GetDirectories(Path.Combine(netFxSdkFolder, @"Lib\um", buildPlatform))));

        var basePath = @"C:\Program Files (x86)\Windows Kits\10";

        var sdkFolder = Directory.GetDirectories(Path.Combine(basePath,@"bin"),"10.0.*").OrderByDescending(_=>_).FirstOrDefault();

        var sdkVersion = Path.GetFileName(sdkFolder);
        SetEnvironmentVariable("WINDOWS_SDK_VERSION", sdkVersion);

        var pathsToAdd = new List<string>()
        {
            Path.Combine(basePath, @"bin", sdkVersion, buildPlatform),
            Path.Combine(basePath, @"bin", buildPlatform),
        };
        PrepandPath("PATH", string.Join(Path.PathSeparator, pathsToAdd));


        var includeFolder = Path.Combine(basePath, "Include", sdkVersion);
        PrepandPath("INCLUDE",  string.Join(Path.PathSeparator, Directory.GetDirectories(includeFolder)));

        var libFolder = Path.Combine(basePath, "Lib", sdkVersion);
        var umLib = Path.Combine(libFolder, @"um", buildPlatform);
        var ucrtLib = Path.Combine(libFolder, @"ucrt", buildPlatform);
        PrepandPath("LIB", $"{umLib}{Path.PathSeparator}{ucrtLib}");
    }
}