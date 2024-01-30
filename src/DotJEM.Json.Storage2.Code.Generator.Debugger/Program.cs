// See https://aka.ms/new-console-template for more information


using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

Console.WriteLine("Hello, World!");

StringTemplateBuilder builder = new StringTemplateBuilder();

StringTemplate test =builder.Build("""
                                   --start:byid
                                     SELECT [Id]
                                         ,[ContentType]
                                         ,[Version]
                                         ,[Created]
                                         ,[Updated]
                                         ,[CreatedBy]
                                         ,[UpdatedBy]
                                         ,[Data]
                                         ,[RV]
                                     FROM [@{schema}].[@{data_table_name}]
                                     WHERE [Id] = @id;
                                   --end:byid
                                   
                                   --start:paged
                                   SELECT [Id]
                                         ,[Version]
                                         ,[Created]
                                         ,[Updated]
                                         ,[CreatedBy]
                                         ,[UpdatedBy]
                                         ,[Data]
                                         ,[RV]
                                     FROM [@{schema}].[@{data_table_name}]
                                     ORDER BY [Created]
                                     OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;
                                   --end:paged
                                   """, CancellationToken.None);
Console.WriteLine(test);

public static class StringBuilderExtensions
{
    public static StringBuilder AppendBlock(this StringBuilder builder, string source, int index, int? length = null, string? parameter = null)
    {
        ReadOnlySpan<char> block = length.HasValue
            ? source.AsSpan(index, (int)length)
            : source.AsSpan(index);

        if (index > 0)
            builder.Append(" + ");
        builder.Append("\"");
        builder.Append(block);
        if (parameter != null)
        {
            builder.Append("\" + ");
            builder.Append(parameter);
        }
        else
        {
            builder.Append("\"");
        }
        return builder;
        
    }
}

public class StringTemplateBuilder
{
    private readonly Regex pattern;

    public StringTemplateBuilder(string pattern = "@\\{(.+?)}", RegexOptions options = RegexOptions.Compiled)
        : this(new Regex(pattern, options))
    {
    }

    public StringTemplateBuilder(Regex pattern)
    {
        this.pattern = pattern;
    }

    public StringTemplate Build(string source, CancellationToken token)
    {
        int index = 0;
        StringBuilder builder = new StringBuilder();
        HashSet<string> args = new();

        source = Regex.Replace(source, @"\r\n?|\n", "\\n");
        foreach (Match match in pattern.Matches(source).Cast<Match>())
        {
            string parameter = match.Groups[1].Value;
            args.Add($"string {parameter}");

            builder.AppendBlock(source, index, match.Index - index, parameter);

            index = match.Index + parameter.Length + 3;
        }
        builder.AppendBlock(source, index);

        return new("name", builder.ToString(), args.ToArray());
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
        return BuildClass();
    }

    public string BuildClass()
    {
        return $$""""
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