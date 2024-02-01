namespace DotJEM.Json.Storage2.Generated;

internal static partial class SqlFiles
{
     public static string SelectTableNames_default()
     {
         return "SELECT \n	TABLE_NAME\n  FROM INFORMATION_SCHEMA.TABLES\n WHERE TABLE_SCHEMA = @schema \n   AND TABLE_TYPE = 'BASE TABLE';\n";
     }
}