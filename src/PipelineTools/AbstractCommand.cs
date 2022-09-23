using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace PipelineTools;

public abstract class AbstractCommand
{
    private Command _command;

    public Command Command => _command;

    public AbstractCommand(string command, string description = null)
    {
        _command = new Command(command, description);
        _command.SetHandler(HandleCommand);
    }

    public abstract void HandleCommand(InvocationContext invocationContext);

    protected static T? GetValueForHandlerParameter<T>(
        IValueDescriptor<T> symbol,
        InvocationContext context)
    {
        if (symbol is IValueSource valueSource &&
            valueSource.TryGetValue(symbol, context.BindingContext, out var boundValue) &&
            boundValue is T value)
        {
            return value;
        }
        else
        {
            return GetValueFor(context.ParseResult, symbol);
        }
    }
    protected void SetEnvironmentVariable(string envName, string value)
    {
        Console.WriteLine($"Seting environment variable {envName} to {value}");
        Environment.SetEnvironmentVariable(envName, value, EnvironmentVariableTarget.Process);
        Console.WriteLine($"##vso[task.setvariable variable={envName}]{value}");
    }

    protected void PrepandPath(string envName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        var existingValue = Environment.GetEnvironmentVariable(envName);
        var a = Environment.GetEnvironmentVariables();
        var visitedPath = new HashSet<string>();
        var combinedPaths = new List<string>();

        foreach (var s in value.Split(Path.PathSeparator).Where(_ => !string.IsNullOrWhiteSpace(_)))
        {
            if (!Directory.Exists(s))
            {
                Console.Error.WriteLine($"Path not found: \"{s}\"");
            }
            else if (visitedPath.Add(s))
            {
                combinedPaths.Add(s);
                Console.WriteLine($"{envName}: Adding path \"{s}\"");
            }
        }
        if (!string.IsNullOrWhiteSpace(existingValue))
        {
            foreach (var s in existingValue.Split(Path.PathSeparator).Where(_=>!string.IsNullOrWhiteSpace(_)))
            {
                if (visitedPath.Add(s))
                {
                    combinedPaths.Add(s);
                    Console.WriteLine($"{envName}: Existing path \"{s}\"");
                }
            }
        }

        var newValue = string.Join(Path.PathSeparator, combinedPaths);
        Environment.SetEnvironmentVariable(envName, newValue, EnvironmentVariableTarget.Process);
        Console.WriteLine($"##vso[task.setvariable variable={envName}]{newValue}");
    }

    protected string RunProcess(string fileName, string args)
    {
        var processStartInfo = new ProcessStartInfo(fileName) {Arguments = args};
        return RunProcess(processStartInfo);
    }

    protected string RunProcess(string fileName, params string[] args)
    {
        var processStartInfo = new ProcessStartInfo(fileName);
        foreach (var s in args)
        {
            processStartInfo.ArgumentList.Add(s);
        }
        return RunProcess(processStartInfo);
    }

    protected string RunProcess(ProcessStartInfo info)
    {
        var output = new StringBuilder(1024);
        
        var process = Process.Start(info);
        info.UseShellExecute = false;
        info.RedirectStandardError = true;
        info.RedirectStandardOutput = true;

        void OnProcessOnDataReceived(object s, DataReceivedEventArgs d)
        {
            var str = d?.Data;
            if (str != null)
            {
                if (output.Length != 0) output.Append(Environment.NewLine);
                output.Append(str);
            }
        }
        process.ErrorDataReceived += OnProcessOnDataReceived;
        process.OutputDataReceived += OnProcessOnDataReceived;
        process.EnableRaisingEvents = true;
        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        process.WaitForExit();
        process.Close();
        return output.ToString();
    }

    internal static T? GetValueFor<T>(ParseResult res, IValueDescriptor<T> symbol) =>
        symbol switch
        {
            Argument<T> argument => res.GetValueForArgument(argument),
            Option<T> option => res.GetValueForOption(option),
            _ => throw new ArgumentOutOfRangeException()
        };
}
