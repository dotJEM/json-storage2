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

[Generator(LanguageNames.CSharp)]
public class SqlTemplateGenerator : IIncrementalGenerator
{
    private readonly StringTemplateBuilder builder = new StringTemplateBuilder();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //if (!Debugger.IsAttached)
        //    Debugger.Launch();
        // SEE: https://github.com/podimo/Podimo.ConstEmbed/blob/develop/src/Podimo.ConstEmbed/ConstEmbedGenerator.cs
        // SEE: https://stackoverflow.com/questions/72095200/c-sharp-incremental-generator-how-i-can-read-additional-files-additionaltexts

        IncrementalValuesProvider<AdditionalText> sqlFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".sql"));

        IncrementalValuesProvider<StringTemplate> templates = sqlFiles
            .Select(builder.Build);

        context.RegisterSourceOutput(templates, (spc, template) => {
            spc.AddSource($"SqlFiles.{template.Name}.g.cs", template.ToString());
        });
    }

}



public readonly struct StringTemplate
{
    public string Name { get; }
    public string Source { get; }
    public string[] Args { get; }

    public StringTemplate(string name, string source, string[] args)
    {
        Name = name;
        Source = source;
        Args = args;
    }

    public override string ToString()
    {
        return $$""""
                 namespace DotJEM.Json.Storage2.Generated;
                 
                 internal static partial class SqlFiles
                 {
                      public static string {{Name}}({{string.Join(", ", Args)}})
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

    public StringTemplate Build(AdditionalText content, CancellationToken token)
    {
        string name = Path.GetFileNameWithoutExtension(content.Path);
        string source = content.GetText(token)!.ToString();

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

        return new(name, builder.ToString(), args.ToArray());
    }
}