using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualBasic;
using PipelineTools;
using static System.Net.Mime.MediaTypeNames;

namespace AzurePipelineTool.Commands;

public class FetchToolCommand : AbstractCommand<FetchToolCommand.FetchToolOptions>
{
    public FetchToolCommand() : base("fetch-tool")
    {
    }

    public override async Task HandleCommandAsync(FetchToolOptions options, CancellationToken cancellationToken)
    {
        switch (options.Name)
        {
            case ToolName.Butler:
                await FetchButler(options, cancellationToken);
                break;
            case ToolName.Ninja:
                await FetchNinja(options, cancellationToken);
                break;
            case ToolName.CMake:
                await FetchCMake(options, cancellationToken);
                break;
            case ToolName.CCache:
                await FetchCCache(options, cancellationToken);
                break;
        }
 
    }

    private async Task FetchCCache(FetchToolOptions options, CancellationToken cancellationToken)
    {
        var osVersionPlatform = Environment.OSVersion.Platform;
        var processArchitecture = RuntimeInformation.ProcessArchitecture;

        if (string.IsNullOrWhiteSpace(options.Version))
            options.Version = "4.7.2";
        var url = "";
        switch (osVersionPlatform)
        {
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.Win32NT:
            case PlatformID.WinCE:
            case PlatformID.Xbox:
                if (processArchitecture == Architecture.X86)
                    url = $"https://github.com/ccache/ccache/releases/download/v{options.Version}/ccache-{options.Version}-windows-i686.zip";
                else
                    url = $"https://github.com/ccache/ccache/releases/download/v{options.Version}/ccache-{options.Version}-windows-x86_64.zip";
                break;
            case PlatformID.Unix:
                url = $"https://github.com/ccache/ccache/releases/download/v{options.Version}/ccache-{options.Version}-linux-x86_64.tar.xz";
                break;
        }
    }

    private async Task FetchCMake(FetchToolOptions options, CancellationToken cancellationToken)
    {
        var processArchitecture = RuntimeInformation.ProcessArchitecture;
        var osVersionPlatform = Environment.OSVersion.Platform;

        var arch = "i386";
        switch (processArchitecture)
        {
            case Architecture.X86:
                arch = "i386";
                break;
            case Architecture.X64:
                arch = "x86_64";
                break;
            case Architecture.Arm64:
                arch = "arm64";
                break;
        }
        var os = "windows";
        var archiveType = ArchiveType.zip;
        switch (osVersionPlatform)
        {
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.Win32NT:
            case PlatformID.WinCE:
            case PlatformID.Xbox:
                os = "windows";
                archiveType = ArchiveType.zip;
                break;
            case PlatformID.Unix:
                os = "linux";
                if (processArchitecture == Architecture.Arm64)
                    arch = "aarch64";
                archiveType = ArchiveType.tgz;
                break;
            case PlatformID.MacOSX:
                os = "macos10.10";
                arch = "universal";
                archiveType = ArchiveType.tgz;
                break;
        }
        if (string.IsNullOrWhiteSpace(options.Version))
            options.Version = "3.24.3";
        var fileExt = (archiveType == ArchiveType.zip) ? "zip" : "tar.gz";
        var url = $"https://github.com/Kitware/CMake/releases/download/v{options.Version}/cmake-{options.Version}-{os}-{arch}.{fileExt}";
        await new UnzipUrlCommand().HandleCommandAsync(new UnzipUrlCommand.UnzipUrlOptions()
        {
            Temp = options.Temp,
            Output = options.Output,
            Overwrite = options.Overwrite,
            Url = url,
            ArchiveType = archiveType,
        }, cancellationToken);

        if (options.Path)
        {
            if (osVersionPlatform == PlatformID.MacOSX)
                PipelineUtils.PrepandPath("PATH", Path.Combine(Path.GetFullPath(options.Output), $"cmake-{options.Version}-{os}-{arch}/CMake.app/Contents/bin"));
            else
                PipelineUtils.PrepandPath("PATH",  Path.Combine(Path.GetFullPath(options.Output), $"cmake-{options.Version}-{os}-{arch}/bin"));
        }
    }

    private async Task FetchNinja(FetchToolOptions options, CancellationToken cancellationToken)
    {
        var os = "win";
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.Win32NT:
            case PlatformID.WinCE:
            case PlatformID.Xbox:
                os = "win";
                break;
            case PlatformID.Unix:
                os = "linux";
                break;
            case PlatformID.MacOSX:
                os = "mac";
                break;
        }
        if (string.IsNullOrWhiteSpace(options.Version))
            options.Version = "1.11.1";
        var url = $"https://github.com/ninja-build/ninja/releases/download/v{options.Version}/ninja-{os}.zip";
        await new UnzipUrlCommand().HandleCommandAsync(new UnzipUrlCommand.UnzipUrlOptions()
        {
            Temp = options.Temp,
            Output = options.Output,
            Overwrite = options.Overwrite,
            Url = url
        }, cancellationToken);

        if (options.Path)
        {
            PipelineUtils.PrepandPath("PATH", Path.GetFullPath(options.Output));
        }
    }

    private static async Task FetchButler(FetchToolOptions options, CancellationToken cancellationToken)
    {
        var os = "windows";
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.Win32NT:
            case PlatformID.WinCE:
            case PlatformID.Xbox:
                os = "windows";
                break;
            case PlatformID.Unix:
                os = "linux";
                break;
            case PlatformID.MacOSX:
                os = "darwin";
                break;
        }

        var arch = "386";
        switch (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86:
                arch = "386";
                break;
            case Architecture.X64:
                arch = "amd64";
                break;
        }

        if (string.IsNullOrWhiteSpace(options.Version))
            options.Version = "LATEST";

        var url = $"https://broth.itch.ovh/butler/{os}-{arch}/{options.Version}/archive/default";
        await new UnzipUrlCommand().HandleCommandAsync(new UnzipUrlCommand.UnzipUrlOptions()
        {
            Temp = options.Temp,
            Output = options.Output,
            Overwrite = options.Overwrite,
            Url = url
        }, cancellationToken);

        if (options.Path)
        {
            PipelineUtils.PrepandPath("PATH", Path.GetFullPath(options.Output));
        }
    }

    public enum ToolName
    {
        None,
        Butler,
        Ninja,
        CMake,
        CCache,
    }

    public class FetchToolOptions
    {
        [CommandLineOption("-n", "Tool name")]
        public ToolName Name { get; set; }

        [CommandLineOption("-o", "Output folder")]
        public string Output { get; set; } = Directory.GetCurrentDirectory();

        [CommandLineOption("-t", "Temp folder or file name")]
        public string? Temp { get; set; }

        [CommandLineOption("-v", "Version")]
        public string? Version { get; set; }

        [CommandLineOption(description: "Overwrite files")]
        public bool Overwrite { get; set; }

        [CommandLineOption("-p", description: "Add to PATH")]
        public bool Path { get; set; }
    }
}