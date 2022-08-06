using System.Text;
using DotJEM.Json.Storage2.Util;

namespace DotJEM.Json.Storage2.SqlServer;

public static class SqlServerStatements
{
    private static readonly IAssemblyResourceLoader loader = new AssemblyResourceLoader();
    private static readonly IStringTemplateReplacer replacer = new StringTemplateReplacer();

    public static string Load(string input, params (string key, string value)[] values)
        => Load(input, values.ToDictionary(kv => kv.key, kv => kv.value));

    public static string Load(string resource, IDictionary<string, string> values)
    {
        return replacer.Replace(loader.LoadString($"DotJEM.Json.Storage2.SqlServer.Statements.{resource}.sql"), values);
    }

    public static string Load(string input, string section, params (string key, string value)[] values)
        => Load(input, section, values.ToDictionary(kv => kv.key, kv => kv.value));

    public static string Load(string resource, string section, IDictionary<string, string> values)
    {
        string sql = ReadSection(loader.LoadString($"DotJEM.Json.Storage2.SqlServer.Statements.{resource}.sql"), section);
        return replacer.Replace(sql, values);
    }

    private static string ReadSection(string input, string section)
    {
        bool yield = false;
        StringReader reader = new StringReader(input);
        StringBuilder output = new StringBuilder();
        while (reader.ReadLine() is { } line)
        {
            if (line.Equals($"--start:{section}"))
            {
                yield = true;
                continue;
            }

            if (line.Equals($"--end:{section}"))
                break;

            if (yield) output.AppendLine(line);
        }

        return output.ToString();
    }
}