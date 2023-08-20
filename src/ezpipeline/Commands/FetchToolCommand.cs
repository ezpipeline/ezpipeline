using System.Runtime.InteropServices;
using Mono.Unix;
using Newtonsoft.Json.Serialization;
using PipelineTools;
using Telegram.Bot.Types;
using static System.Net.WebRequestMethods;

namespace AzurePipelineTool.Commands;

public class FetchToolCommand : AbstractCommand<FetchToolCommand.Options>
{
    public FetchToolCommand() : base("fetch-tool", "Fetch tool (ninja, cmake, etc.)")
    {
    }

    public enum ToolName
    {
        None,
        Butler,
        Ninja,
        CMake,
        CCache,
        Clang
    }

    private static async Task FetchButler(Options options, CancellationToken cancellationToken)
    {
        var os = "windows";
        switch (PipelineUtils.GetPlatformId())
        {
            case PlatformIdentifier.Windows:
                os = "windows";
                break;
            case PlatformIdentifier.Linux:
                os = "linux";
                break;
            case PlatformIdentifier.MacOSX:
                os = "darwin";
                break;
        }

        var arch = "386";
        switch (RuntimeInformation.ProcessArchitecture)
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
        await new UnzipUrlCommand().HandleCommandAsync(new UnzipUrlCommand.UnzipUrlOptions
        {
            Temp = options.Temp,
            Output = options.Output,
            Overwrite = options.Overwrite,
            Url = url,
            ArchiveType = ArchiveType.zip
        }, cancellationToken);

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            var unixFileInfo = new UnixFileInfo(Path.Combine(options.Output, "butler"));
            if (unixFileInfo.Exists)
            {
                Console.WriteLine($"Making {unixFileInfo.Name} executable");
                unixFileInfo.FileAccessPermissions = unixFileInfo.FileAccessPermissions
                                                     | FileAccessPermissions.GroupExecute
                                                     | FileAccessPermissions.UserExecute
                                                     | FileAccessPermissions.OtherExecute;
            }
        }

        if (options.Path) PipelineUtils.PrepandPath("PATH", Path.GetFullPath(options.Output));
    }

    public override async Task HandleCommandAsync(Options options, CancellationToken cancellationToken)
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
            case ToolName.Clang:
                await FetchClang(options, cancellationToken);
                break;
        }
    }

    private async Task FetchClang(Options options, CancellationToken cancellationToken)
    {
        var osVersionPlatform = PipelineUtils.GetPlatformId();
        var processArchitecture = RuntimeInformation.ProcessArchitecture;

        if (string.IsNullOrWhiteSpace(options.Version))
            options.Version = "15.0.1";

        string? url = null;
        switch (osVersionPlatform)
        {
            case PlatformIdentifier.Windows:
                //url = $"https://ziglang.org/deps/zig+llvm+lld+clang-x86_64-windows-gnu-{options.Version}.zip";
                break;
            case PlatformIdentifier.Linux:
                switch (processArchitecture)
                {
                    case Architecture.Arm64:
                        url = $"https://github.com/llvm/llvm-project/releases/download/llvmorg-{options.Version}/clang+llvm-{options.Version}-aarch64-linux-gnu.tar.xz";
                        break;
                    case Architecture.X64:
                        url = $"https://github.com/llvm/llvm-project/releases/download/llvmorg-{options.Version}/clang+llvm-{options.Version}-x86_64-unknown-linux-gnu-sles15.tar.xz";
                        break;
                }
                break;
            case PlatformIdentifier.MacOSX:
                switch (processArchitecture)
                {
                    case Architecture.Arm64:
                        url = $"https://github.com/llvm/llvm-project/releases/download/llvmorg-{options.Version}/clang+llvm-{options.Version}-arm64-apple-darwin21.0.tar.xz";
                        break;
                    case Architecture.X64:
                        url = $"https://github.com/llvm/llvm-project/releases/download/llvmorg-{options.Version}/clang+llvm-{options.Version}-x86_64-apple-darwin.tar.xz";
                        break;
                }
                break;
        }

        await new UnzipUrlCommand().HandleCommandAsync(new UnzipUrlCommand.UnzipUrlOptions
        {
            Temp = options.Temp,
            Output = options.Output,
            Overwrite = options.Overwrite,
            Url = url,
            ArchiveType = ArchiveType.auto
        }, cancellationToken);

        PipelineUtils.PrepandPath("PATH", Path.Combine(Path.GetFullPath(options.Output), "bin"));
    }

    private async Task FetchCCache(Options options, CancellationToken cancellationToken)
    {
        var osVersionPlatform = PipelineUtils.GetPlatformId();
        osVersionPlatform = PlatformIdentifier.Linux;
        var processArchitecture = RuntimeInformation.ProcessArchitecture;

        if (string.IsNullOrWhiteSpace(options.Version))
            options.Version = "4.7.2";

        string? fileName = null;
        switch (osVersionPlatform)
        {
            case PlatformIdentifier.Windows:
                if (processArchitecture == Architecture.X86)
                    fileName = $"ccache-{options.Version}-windows-i686.zip";
                else
                    fileName = $"ccache-{options.Version}-windows-x86_64.zip";
                break;
            case PlatformIdentifier.Linux:
                fileName = $"ccache-{options.Version}-linux-x86_64.tar.xz";
                break;
        }
        var url = $"https://github.com/ccache/ccache/releases/download/v{options.Version}/{fileName}";

        await new UnzipUrlCommand().HandleCommandAsync(new UnzipUrlCommand.UnzipUrlOptions
        {
            Temp = options.Temp,
            Output = options.Output,
            Overwrite = options.Overwrite,
            Url = url,
            ArchiveType = ArchiveType.auto
        }, cancellationToken);

        PipelineUtils.PrepandPath("PATH", Path.Combine(Path.GetFullPath(options.Output), Path.GetFileNameWithoutExtension(fileName)));
    }

    private async Task FetchCMake(Options options, CancellationToken cancellationToken)
    {
        var processArchitecture = RuntimeInformation.ProcessArchitecture;
        var osVersionPlatform = PipelineUtils.GetPlatformId();

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
            case PlatformIdentifier.Windows:
                os = "windows";
                archiveType = ArchiveType.zip;
                break;
            case PlatformIdentifier.Linux:
                os = "linux";
                if (processArchitecture == Architecture.Arm64)
                    arch = "aarch64";
                archiveType = ArchiveType.tgz;
                break;
            case PlatformIdentifier.MacOSX:
                os = "macos10.10";
                arch = "universal";
                archiveType = ArchiveType.tgz;
                break;
        }

        if (string.IsNullOrWhiteSpace(options.Version))
            options.Version = "3.24.3";
        var fileExt = archiveType == ArchiveType.zip ? "zip" : "tar.gz";
        var url =
            $"https://github.com/Kitware/CMake/releases/download/v{options.Version}/cmake-{options.Version}-{os}-{arch}.{fileExt}";
        await new UnzipUrlCommand().HandleCommandAsync(new UnzipUrlCommand.UnzipUrlOptions
        {
            Temp = options.Temp,
            Output = options.Output,
            Overwrite = options.Overwrite,
            Url = url,
            ArchiveType = archiveType
        }, cancellationToken);

        if (options.Path)
        {
            if (osVersionPlatform == PlatformIdentifier.MacOSX)
                PipelineUtils.PrepandPath("PATH",
                    Path.Combine(Path.GetFullPath(options.Output),
                        $"cmake-{options.Version}-{os}-{arch}/CMake.app/Contents/bin"));
            else
                PipelineUtils.PrepandPath("PATH",
                    Path.Combine(Path.GetFullPath(options.Output), $"cmake-{options.Version}-{os}-{arch}/bin"));
        }
    }

    private async Task FetchNinja(Options options, CancellationToken cancellationToken)
    {
        var os = "win";
        switch (PipelineUtils.GetPlatformId())
        {
            case PlatformIdentifier.Windows:
                os = "win";
                break;
            case PlatformIdentifier.Linux:
                os = "linux";
                break;
            case PlatformIdentifier.MacOSX:
                os = "mac";
                break;
        }

        if (string.IsNullOrWhiteSpace(options.Version))
            options.Version = "1.11.1";
        var url = $"https://github.com/ninja-build/ninja/releases/download/v{options.Version}/ninja-{os}.zip";
        await new UnzipUrlCommand().HandleCommandAsync(new UnzipUrlCommand.UnzipUrlOptions
        {
            Temp = options.Temp,
            Output = options.Output,
            Overwrite = options.Overwrite,
            Url = url
        }, cancellationToken);

        if (options.Path) PipelineUtils.PrepandPath("PATH", Path.GetFullPath(options.Output));
    }

    public class Options
    {
        [CommandLineOption("-n", "Tool name")] public ToolName Name { get; set; }

        [CommandLineOption("-o", "Output folder")]
        public string Output { get; set; } = Directory.GetCurrentDirectory();

        [CommandLineOption("-t", "Temp folder or file name")]
        public string? Temp { get; set; }

        [CommandLineOption("-v", "Version")] public string? Version { get; set; }

        [CommandLineOption(description: "Overwrite files")]
        public bool Overwrite { get; set; }

        [CommandLineOption("-p", "Add to PATH")]
        public bool Path { get; set; }
    }
}