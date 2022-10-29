using Mono.Cecil;

namespace PipelineTools;

public static class ExtensionMethods
{
    public static IEnumerable<TypeDefinition> EnumerateTypes(this AssemblyDefinition assembly)
    {
        foreach (var moduleDefinition in assembly.Modules)
        foreach (var type in moduleDefinition.EnumerateTypes())
            yield return type;
    }

    public static IEnumerable<TypeDefinition> EnumerateTypes(this ModuleDefinition module)
    {
        foreach (var typeDefinition in module.Types)
        foreach (var type in typeDefinition.EnumerateTypes())
            yield return type;
    }

    public static IEnumerable<TypeDefinition> EnumerateTypes(this TypeDefinition typeDefinition)
    {
        yield return typeDefinition;
        foreach (var nestedType in typeDefinition.NestedTypes)
        foreach (var type in nestedType.EnumerateTypes())
            yield return type;
    }
}