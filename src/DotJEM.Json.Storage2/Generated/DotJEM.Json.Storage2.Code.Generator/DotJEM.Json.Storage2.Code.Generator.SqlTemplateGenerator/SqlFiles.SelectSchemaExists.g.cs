namespace DotJEM.Json.Storage2.Generated;

internal static partial class SqlFiles
{
     public static string SelectSchemaExists()
     {
         return "SELECT ISNULL(\n    (SELECT TOP 1 1 FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = @schema), 0);\n";
     }
}