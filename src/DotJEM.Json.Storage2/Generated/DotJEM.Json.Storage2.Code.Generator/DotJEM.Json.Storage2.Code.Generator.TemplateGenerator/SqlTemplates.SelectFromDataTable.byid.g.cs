namespace DotJEM.Json.Storage2.Generated;

internal static partial class SqlFiles
{
     public static string SelectFromDataTable_byid(string schema, string data_table_name)
     {
         return "  SELECT [Id]\n      ,[ContentType]\n      ,[Version]\n      ,[Created]\n      ,[Updated]\n      ,[CreatedBy]\n      ,[UpdatedBy]\n      ,[Data]\n      ,[RV]\n  FROM [" + schema + "].[" + data_table_name + "]\n  WHERE [Id] = @id;\n";
     }
}