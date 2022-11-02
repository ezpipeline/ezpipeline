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

        Command.SetHandler(OnHandleCommandAsync);
    }

    public Command Command { get; }

    public abstract Task HandleCommandAsync(TOptions options, CancellationToken cancellationToken);

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

    private async Task OnHandleCommandAsync(InvocationContext invocationContext)
    {
        _invocationContext = invocationContext;
        var instance = new ModelBinder<TOptions>().CreateInstance(invocationContext.BindingContext);    
        await HandleCommandAsync((TOptions)instance, invocationContext.GetCancellationToken());
        if (_invocationContext == invocationContext)
            _invocationContext = null;
    }
}