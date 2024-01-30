namespace DotJEM.Json.Storage2.Generated;

internal static partial class SqlFiles
{
     public static string SelectSchemaNames()
     {
         return "SELECT \n	--CATALOG_NAME,\n	SCHEMA_NAME\n	--SCHEMA_OWNER\n  FROM INFORMATION_SCHEMA.SCHEMATA;";
     }
}