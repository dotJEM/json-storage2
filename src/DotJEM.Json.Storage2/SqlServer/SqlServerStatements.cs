using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DotJEM.Json.Storage2.Util;
using static System.Collections.Specialized.BitVector32;

namespace DotJEM.Json.Storage2.SqlServer;

public static class SqlServerStatements
{
    private static readonly IAssemblyResourceLoader loader = new AssemblyResourceLoader();
    private static readonly IStringTemplateReplacer replacer = new StringTemplateReplacer();
    private static readonly ISqlServerStatementTemplateCache cache = new SqlServerStatementTemplateCache(loader.LoadString);
    
    public static string Load(string input, params (string key, string value)[] values)
        => Load(input, "default", values.ToDictionary(kv => kv.key, kv => kv.value));
    public static string Load(string input, IDictionary<string, string> values)
        => Load(input, "default", values);
    public static string Load(string input, string section, params (string key, string value)[] values)
        => Load(input, section, values.ToDictionary(kv => kv.key, kv => kv.value));

    public static string Load(string resource, string section, IDictionary<string, string> values)
    {
        string sql = cache.Lookup(resource, section);

        //string sql = ReadSection(loader.LoadString($"DotJEM.Json.Storage2.SqlServer.Statements.{resource}.sql"), section);
        return replacer.Replace(sql, values);
    }
}

//public static class SqlServerStatementTemplateCacheExtensions 
//{
//    public static string Load(this ISqlServerStatementTemplateCache self, string input, params (string key, string value)[] values)
//        => self.Load(input, "default", values.ToDictionary(kv => kv.key, kv => kv.value));
//    public static string Load(this ISqlServerStatementTemplateCache self, string input, IDictionary<string, string> values)
//        => self.Load(input, "default", values);
//    public static string Load(this ISqlServerStatementTemplateCache self, string input, string section, params (string key, string value)[] values)
//        => self.Load(input, section, values.ToDictionary(kv => kv.key, kv => kv.value));
//    public static string Load(this ISqlServerStatementTemplateCache self, string resource, string section, IDictionary<string, string> values)
//    {
//        string sql = self.Lookup(resource, section);

//        //string sql = ReadSection(loader.LoadString($"DotJEM.Json.Storage2.SqlServer.Statements.{resource}.sql"), section);
//        return replacer.Replace(sql, values);
//    }
//}

public interface ISqlServerStatementTemplateCache
{
    string Lookup(string resource, string section);
}

public class SqlServerStatementTemplateCache : ISqlServerStatementTemplateCache
{
    private readonly ConcurrentDictionary<string, IDictionary<string, string>> map = new();
    private static readonly IAssemblyResourceLoader loader = new AssemblyResourceLoader();

    private readonly Func<string, string> valueProvider;

    public SqlServerStatementTemplateCache(Func<string, string> valueProvider)
    {
        this.valueProvider = valueProvider;
    }

    public string Lookup(string resource, string section = "default")
    {
        IDictionary<string, string> templates = map.GetOrAdd(resource, Load);
        return templates[section];
    }
    
    private IDictionary<string, string> Load(string resource)
    {
        StringReader reader = new (valueProvider.Invoke($"DotJEM.Json.Storage2.SqlServer.Statements.{resource}.sql"));
        return ReadToEnd(reader).ToDictionary(tuple => tuple.Key, tuple => tuple.Template);
    }

    private static IEnumerable<(string Key, string Template)> ReadToEnd(StringReader reader)
    {
        StringBuilder buffer = new();
        string section = "default";
        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith("--start"))
            {
                string[] parts = line.Split(':');
                if (parts.Length == 2)
                    section = parts[1].Trim();
                continue;
            }

            if (line.StartsWith("--end"))
            {
                string template = buffer.ToString();
                buffer.Clear();
                yield return (section, template);
                continue;
            }

            buffer.AppendLine(line);
        }

        if (buffer.Length > 0)
        {
            string template = buffer.ToString();
            yield return (section, template);
        }
    }
}