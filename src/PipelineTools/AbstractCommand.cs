using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace PipelineTools;

public abstract class AbstractCommand<TOptions>
{
    private InvocationContext? _invocationContext;

    public AbstractCommand(string command, string description = null)
    {
        Command = new Command(command, description);
        var props = typeof(TOptions).GetProperties();
        foreach (var propertyInfo in props)
        {
            var attr = propertyInfo.GetCustomAttribute<CommandLineOptionAttribute>();
            if (attr != null) Command.AddOption(attr.MakeOption(propertyInfo));
        }

        Command.SetHandler(OnHandleCommand);
    }

    public Command Command { get; }

    internal static T? GetValueFor<T>(ParseResult res, IValueDescriptor<T> symbol)
    {
        return symbol switch
        {
            Argument<T> argument => res.GetValueForArgument(argument),
            Option<T> option => res.GetValueForOption(option),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public abstract void HandleCommand(TOptions options);

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
            if (!Directory.Exists(s))
            {
                Console.Error.WriteLine($"Path not found: \"{s}\"");
            }
            else if (visitedPath.Add(s))
            {
                combinedPaths.Add(s);
                Console.WriteLine($"{envName}: Adding path \"{s}\"");
            }

        if (!string.IsNullOrWhiteSpace(existingValue))
            foreach (var s in existingValue.Split(Path.PathSeparator).Where(_ => !string.IsNullOrWhiteSpace(_)))
                if (visitedPath.Add(s))
                {
                    combinedPaths.Add(s);
                    Console.WriteLine($"{envName}: Existing path \"{s}\"");
                }

        var newValue = string.Join(Path.PathSeparator, combinedPaths);
        Environment.SetEnvironmentVariable(envName, newValue, EnvironmentVariableTarget.Process);
        Console.WriteLine($"##vso[task.setvariable variable={envName}]{newValue}");
    }

    protected string RunProcess(string fileName, string args)
    {
        var processStartInfo = new ProcessStartInfo(fileName) { Arguments = args };
        return RunProcess(processStartInfo);
    }

    protected string RunProcess(string fileName, params string[] args)
    {
        var processStartInfo = new ProcessStartInfo(fileName);
        foreach (var s in args) processStartInfo.ArgumentList.Add(s);
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

    private void OnHandleCommand(InvocationContext invocationContext)
    {
        _invocationContext = invocationContext;
        HandleCommand((TOptions)new ModelBinder<TOptions>().CreateInstance(invocationContext.BindingContext));
        if (_invocationContext == invocationContext)
            _invocationContext = null;
    }
}