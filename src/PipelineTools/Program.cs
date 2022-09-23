using System.CommandLine;
using System.CommandLine.Parsing;

namespace PipelineTools
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var rootCommand = new RootCommand();
            rootCommand.Add(new VisualStudioEnvironmentCommand().Command);
            rootCommand.Add(new WindowsSdkEnvironmentCommand().Command);

            rootCommand.Parse(args).Invoke();
        }
    }
}
