using System.CommandLine;
using System.Reflection;
using System.Text;

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

        string[] aliases = new[] { "--" + DeCamel(property.Name), _alias }.Where(_ => !string.IsNullOrWhiteSpace(_))
            .ToArray();
        var genericType = typeof(Option<>).MakeGenericType(property.PropertyType);
        return (Option)Activator.CreateInstance(genericType, aliases, _description);
        //return new Option<string>(new[] { _alias }.Where(_ => !string.IsNullOrWhiteSpace(_)).ToArray(), _description);
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