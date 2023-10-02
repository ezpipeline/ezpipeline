using Mono.Cecil;
using PipelineTools;

namespace AzurePipelineTool.Commands;

public class PatchDllImportCommand : AbstractCommand<PatchDllImportCommand.PatchDllImportOptions>
{
    private readonly IPlatformEnvironment _environment;

    public PatchDllImportCommand(IPlatformEnvironment environment) : base("patch-dllimport",
        "Patch dllimport in an assembly bytecode")
    {
        _environment = environment;
    }

    public override async Task HandleCommandAsync(PatchDllImportOptions options, CancellationToken cancellationToken)
    {
        var fileName = options.Input;
        if (string.IsNullOrWhiteSpace(options.Input)) throw new ArgumentException("Missing --input argument");

        var inputFullPath = Path.GetFullPath(fileName);
        _environment.WriteLine($"Reading {inputFullPath}");

        var assembly = AssemblyDefinition.ReadAssembly(inputFullPath);
        var moduleName = options.NewValue;
        if (string.IsNullOrWhiteSpace(moduleName)) throw new ArgumentException("Missing --new-value argument");

        foreach (var typeDefinition in assembly.EnumerateTypes())
        foreach (var methodDefinition in typeDefinition.Methods)
            if (methodDefinition.PInvokeInfo != null)
            {
                _environment.WriteLine($"Patching {methodDefinition.DeclaringType.Name}.{methodDefinition.Name}(...)");
                methodDefinition.PInvokeInfo.Module.Name = moduleName;
            }

        var outputFullPath = Path.GetFullPath(options.Output);
        _environment.WriteLine($"Writing {outputFullPath}");
        Directory.CreateDirectory(Path.GetDirectoryName(outputFullPath));
        assembly.Write(outputFullPath);
    }

    public class PatchDllImportOptions
    {
        [CommandLineOption("-i", "Input assembly")]
        public string Input { get; set; }

        [CommandLineOption("-o", "Output assembly")]
        public string Output { get; set; }

        [CommandLineOption("-v", "New value")] public string NewValue { get; set; }
    }
}