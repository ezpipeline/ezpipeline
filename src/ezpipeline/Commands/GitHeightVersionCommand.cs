using System.Globalization;
using PipelineTools;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;

namespace AzurePipelineTool.Commands;

public class GitHeightVersionCommand : AbstractCommand<GitHeightVersionCommand.Options>
{
    public GitHeightVersionCommand() : base("git-height-version", "Evaluate version from git height")
    {
    }

    public class Options
    {
        [CommandLineOption("-i", "Input file to evaluate height on")]
        public string Input { get; set; }

        [CommandLineOption("-b", "Base version")]
        public string BaseVersion { get; set; }

        [CommandLineOption("-m", "Main branch regex")]
        public string Main { get; set; } = "(master|main)";

        [CommandLineOption(alias: "-v", description: "Environment variable to set")]
        public string? Variable { get; set; }

    }

    public override async Task HandleCommandAsync(Options options, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(options.Input);
        var workingDirectory = fullPath;

        if (File.Exists(workingDirectory))
        {
            workingDirectory = Path.GetDirectoryName(workingDirectory);
        }

        var gitLog = await Cli.Wrap("git").WithWorkingDirectory(workingDirectory).WithArguments(new[] { "log", "-n1", "--pretty=format:%H", "--", fullPath }).ExecuteBufferedAsync();
        var firstCommit = gitLog.StandardOutput.Trim();
        var gitHeight = await Cli.Wrap("git").WithWorkingDirectory(workingDirectory).WithArguments(new[] { "rev-list", "--first-parent", "--count", $"{firstCommit}..HEAD" }).ExecuteBufferedAsync();
        var height = int.Parse(gitHeight.StandardOutput.Trim(), CultureInfo.InvariantCulture);

        
        Version version = new Version(0,0,1);
        if (!string.IsNullOrWhiteSpace(options.BaseVersion))
        {
            version = Version.Parse(options.BaseVersion);
        }
        if (version.Revision >= 0)
        {
            version = new Version(version.Major, version.Minor, version.Build, version.Revision + height);
        }
        else
        {
            version = new Version(version.Major, version.Minor, version.Build + height);
        }

        var branch = Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCH");
        if (!string.IsNullOrEmpty(branch))
        {
            var prefix = "refs/heads/";
            if (branch.StartsWith(prefix))
                branch = branch.Substring(prefix.Length);
        }
        else
        {
            var gitBranch = await Cli.Wrap("git").WithWorkingDirectory(workingDirectory)
                .WithArguments(new[] { "rev-parse", "--abbrev-ref=strict", "HEAD" }).ExecuteBufferedAsync();
            branch = gitBranch.StandardOutput.Trim();
        }

        var gitCommit = await Cli.Wrap("git").WithWorkingDirectory(workingDirectory).WithArguments(new[] { "rev-parse", "--short", "HEAD"}).ExecuteBufferedAsync();
        var commit = gitCommit.StandardOutput.Trim();

        var isMain = new Regex(options.Main).Match(branch).Success;

        var stringVersion = version.ToString();
        if (!isMain)
            stringVersion = stringVersion + "-alpha" + commit;

        Console.WriteLine(stringVersion);
        PipelineUtils.SetEnvironmentVariable(options.Variable, stringVersion);
    }
}