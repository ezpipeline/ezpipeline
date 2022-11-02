using Mono.Cecil;
using PipelineTools;

namespace AzurePipelineTool.Commands;

public class PatchDllImportCommand : AbstractCommand<PatchDllImportCommand.PatchDllImportOptions>
{
    public PatchDllImportCommand() : base("patch-dllimport")
    {
    }

    public override async Task HandleCommandAsync(PatchDllImportOptions options, CancellationToken cancellationToken)
    {
        var fileName = options.Input;
        if (string.IsNullOrWhiteSpace(options.Input)) throw new ArgumentException("Missing --input argument");

        var assembly = AssemblyDefinition.ReadAssembly(fileName);
        var moduleName = options.NewValue;
        if (string.IsNullOrWhiteSpace(moduleName)) throw new ArgumentException("Missing --value argument");

        foreach (var typeDefinition in assembly.EnumerateTypes())
        foreach (var methodDefinition in typeDefinition.Methods)
            if (methodDefinition.PInvokeInfo != null)
            {
                Console.WriteLine($"Patching {methodDefinition.DeclaringType.Name}.{methodDefinition.Name}(...)");
                methodDefinition.PInvokeInfo.Module.Name = moduleName;
            }

        assembly.Write(options.Output);
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