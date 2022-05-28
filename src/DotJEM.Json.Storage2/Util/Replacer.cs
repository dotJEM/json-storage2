using System.Text.RegularExpressions;

namespace DotJEM.Json.Storage2.Util;

public interface IStringTemplateReplacer
{
    string Replace(string input, params (string key, string value)[] values);
    string Replace(string input, IDictionary<string, string> values);
}

public class StringTemplateReplacer : IStringTemplateReplacer
{
    private readonly Regex pattern;

    public StringTemplateReplacer(string pattern = "@\\{(.+?)}", RegexOptions options = RegexOptions.Compiled)
        : this(new Regex(pattern, options))
    {
    }

    public StringTemplateReplacer(Regex pattern)
    {
        this.pattern = pattern;
    }

    public string Replace(string input, params (string key, string value)[] values)
        => Replace(input, values.ToDictionary(kv => kv.key, kv => kv.value));

    public string Replace(string input, IDictionary<string, string> values)
    {
        return pattern.Replace(input, match =>
        {
            string key = match.Groups[1].ToString();
            return values[key];
        });
    }
}