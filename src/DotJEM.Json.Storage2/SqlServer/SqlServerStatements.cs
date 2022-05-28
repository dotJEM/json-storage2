using DotJEM.Json.Storage2.Util;

namespace DotJEM.Json.Storage2.SqlServer;

public static class SqlServerStatements
{
    private static readonly IAssemblyResourceLoader Loader = new AssemblyResourceLoader();
    private static readonly IStringTemplateReplacer Replacer = new StringTemplateReplacer();

    public static string Load(string input, params (string key, string value)[] values)
        => Load(input, values.ToDictionary(kv => kv.key, kv => kv.value));

    public static string Load(string resource, IDictionary<string, string> values)
    {
        return Replacer.Replace(Loader.LoadString($"DotJEM.Json.Storage2.SqlServer.Statements.{resource}.sql"), values);
    }
}