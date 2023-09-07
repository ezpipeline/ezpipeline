using System.CommandLine;
using System.Reflection;
using System.Text;

namespace PipelineTools;

public class CommandLineOptionAttribute : Attribute
{
    private readonly string? _alias;
    private readonly string? _description;
    private readonly bool _required;

    public CommandLineOptionAttribute(string? alias = null, string? description = null, bool required = false)
    {
        _alias = alias;
        _description = description;
        _required = required;
    }

    public Option MakeOption(PropertyInfo property)
    {
        string?[] aliases = new[] { "--" + DeCamel(property.Name), _alias }.Where(_ => !string.IsNullOrWhiteSpace(_))
            .ToArray();
        var optionType = property.PropertyType;
        var genericType = typeof(Option<>).MakeGenericType(optionType);
        var result = (Option)Activator.CreateInstance(genericType, aliases, _description)!;
        result.IsRequired = _required;
        return result;
    }

    private string DeCamel(string name)
    {
        var result = new StringBuilder(name.Length + 2);
        var prevLowCase = false;
        foreach (var c in name)
        {
            if (prevLowCase && char.IsUpper(c)) result.Append('-');

            result.Append(char.ToLower(c));
            prevLowCase = !char.IsUpper(c);
        }

        return result.ToString();
    }
}