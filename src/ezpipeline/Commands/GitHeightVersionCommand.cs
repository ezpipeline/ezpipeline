using System.Globalization;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;
using PipelineTools;

namespace AzurePipelineTool.Commands;

public class GitHeightVersionCommand : AbstractCommand<GitHeightVersionCommand.Options>
{
    private readonly IPlatformEnvironment _environment;

    public GitHeightVersionCommand(IPlatformEnvironment environment) : base("git-height-version",
        "Evaluate version from git height")
    {
        _environment = environment;
    }

    public override async Task HandleCommandAsync(Options options, CancellationToken cancellationToken)
    {
        string firstCommit;
        var workingDirectory = Directory.GetCurrentDirectory();
        if (string.IsNullOrWhiteSpace(options.Input))
        {
            var gitLog = await Cli.Wrap("git").WithArguments(new[] { "rev-list", "--max-parents=0", "HEAD" })
                .ExecuteBufferedAsync();
            firstCommit = gitLog.StandardOutput.Trim();
        }
        else
        {
            var fullPath = Path.GetFullPath(options.Input);
            workingDirectory = fullPath;

            if (File.Exists(workingDirectory)) workingDirectory = Path.GetDirectoryName(workingDirectory);
            var gitLog = await Cli.Wrap("git").WithWorkingDirectory(workingDirectory)
                .WithArguments(new[] { "log", "-n1", "--pretty=format:%H", "--", fullPath }).ExecuteBufferedAsync();
            firstCommit = gitLog.StandardOutput.Trim();
        }


        var gitHeight = await Cli.Wrap("git").WithWorkingDirectory(workingDirectory)
            .WithArguments(new[] { "rev-list", "--first-parent", "--count", $"{firstCommit}..HEAD" })
            .ExecuteBufferedAsync();
        var height = int.Parse(gitHeight.StandardOutput.Trim(), CultureInfo.InvariantCulture);

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

        var stringVersion = height.ToString(CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(options.BaseVersion))
        {
            var version = Version.Parse(options.BaseVersion);
            if (version.Revision >= 0)
                version = new Version(version.Major, version.Minor, version.Build, version.Revision + height);
            else
                version = new Version(version.Major, version.Minor, version.Build + height);

            var gitCommit = await Cli.Wrap("git").WithWorkingDirectory(workingDirectory)
                .WithArguments(new[] { "rev-parse", "--short", "HEAD" }).ExecuteBufferedAsync();
            var commit = gitCommit.StandardOutput.Trim();

            var isMain = new Regex(options.Main).Match(branch).Success;

            stringVersion = version.ToString();
            if (!isMain)
                stringVersion = stringVersion + "-alpha" + commit;
        }

        _environment.WriteLine(stringVersion);
        _environment.SetEnvironmentVariable(options.Variable, stringVersion);
    }

    public class Options
    {
        [CommandLineOption("-i", "Input file to evaluate height on")]
        public string Input { get; set; }

        [CommandLineOption("-b", "Base version")]
        public string BaseVersion { get; set; }

        [CommandLineOption("-m", "Main branch regex")]
        public string Main { get; set; } = "(master|main)";

        [CommandLineOption("-v", "Environment variable to set")]
        public string? Variable { get; set; }
    }
}