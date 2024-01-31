using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using static System.Net.Mime.MediaTypeNames;


namespace DotJEM.Json.Storage2.Code.Generator;

//if (!Debugger.IsAttached)
//    Debugger.Launch();
// SEE: https://github.com/podimo/Podimo.ConstEmbed/blob/develop/src/Podimo.ConstEmbed/ConstEmbedGenerator.cs
// SEE: https://stackoverflow.com/questions/72095200/c-sharp-incremental-generator-how-i-can-read-additional-files-additionaltexts
// https://andrewlock.net/creating-a-source-generator-part-6-saving-source-generator-output-in-source-control/
// https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md


[Generator(LanguageNames.CSharp)]
public class TemplateGenerator : IIncrementalGenerator
{
    private readonly StringTemplateBuilder builder = new StringTemplateBuilder();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<string?> assemblyName = context.CompilationProvider.Select(static (c, _) => c.AssemblyName);
        IncrementalValuesProvider<AdditionalText> templateFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".html")); //Note hardcoded filetype for now.

        IncrementalValuesProvider<(AdditionalText Left, string? Right)> combined = templateFiles.Combine(assemblyName);
        
        IncrementalValuesProvider<StringTemplate> templates = combined
            .Select((tuple, _) => tuple.Left)
            .SelectMany(builder.Build);

        context.RegisterSourceOutput(templates, (spc, template) => {
            spc.AddSource($"Html.{template.Name}.{template.Key}.g.cs", template.ToString());
        });
    }
}



public readonly struct StringTemplate
{
    public string Name { get; }
    public string Key { get; }
    public string Source { get; }
    public string[] Args { get; }

    public StringTemplate(string name, string key, string source, string[] args)
    {
        Name = name;
        Key = key;
        Source = source;
        Args = args;
    }

    public override string ToString()
    {
        return $$""""
                 namespace DotJEM.Json.Storage2.Generated;
                 
                 internal static partial class SqlFiles
                 {
                      public static string {{Name}}_{{Key}}({{string.Join(", ", Args)}})
                      {
                          return {{Source}};
                      }
                 }
                 """";
    }
}

public class StringTemplateBuilder 
{
    private readonly Regex pattern;
    private readonly Regex nlPattern = new Regex(@"\r\n?|\n", RegexOptions.Compiled);

    public StringTemplateBuilder(string pattern = "@\\{(.+?)}", RegexOptions options = RegexOptions.Compiled)
        : this(new Regex(pattern, options))
    {
    }

    public StringTemplateBuilder(Regex pattern)
    {
        this.pattern = pattern;
    }

    public IEnumerable<StringTemplate> Build(AdditionalText content, CancellationToken token)
    {
        string name = Path.GetFileNameWithoutExtension(content.Path);
        string sourceFromFile = content.GetText(token)!.ToString();

        foreach ((string Key, string Template)  in new TemplateReader().ReadToEnd(new StringReader(sourceFromFile)))
        {
            string source = Template;
            int index = 0;
            StringBuilder builder = new StringBuilder();
            HashSet<string> args = new();

            source = nlPattern.Replace(source, "\\n");
            foreach (Match match in pattern.Matches(source).Cast<Match>())
            {
                string before = source.Substring(index, match.Index - index);
                string key = match.Groups[1].Value;
                args.Add($"string {key}");
                if (index > 0)
                    builder.Append(" + ");

                builder.Append("\"");
                builder.Append(before);
                builder.Append("\" + ");
                builder.Append(key);

                index = match.Index + key.Length + 3;
            }
            string remainder = source.Substring(index);
            if (index > 0)
                builder.Append(" + ");
            builder.Append("\"");
            builder.Append(remainder);
            builder.Append("\"");

            yield return new(name, Key, builder.ToString(), args.ToArray());
        }
    }
}

public class TemplateReader
{
    public IEnumerable<(string Key, string Template)> ReadToEnd(StringReader reader)
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