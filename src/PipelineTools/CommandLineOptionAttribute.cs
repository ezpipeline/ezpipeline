using System.CommandLine;
using System.Reflection;

namespace PipelineTools;

public class CommandLineOptionAttribute : Attribute
{
    private readonly string? _alias;
    private readonly string? _description;

    public CommandLineOptionAttribute(string? alias = null, string? description = null)
    {
        _alias = alias;
        _description = description;
    }

    public Option MakeOption(PropertyInfo property)
    {
        //System.CommandLine.NamingConventionBinder.ModelBinder<>

        string[] aliases = new[] { "--" + property.Name, _alias }.Where(_ => !string.IsNullOrWhiteSpace(_)).ToArray();
        var genericType = typeof(Option<>).MakeGenericType(property.PropertyType);
        return (Option)Activator.CreateInstance(genericType, aliases, _description);
        //return new Option<string>(new[] { _alias }.Where(_ => !string.IsNullOrWhiteSpace(_)).ToArray(), _description);
    }
}