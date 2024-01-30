namespace DotJEM.Json.Storage2.Generated;

internal static partial class SqlFiles
{
     public static string SelectFromDataTable(string schema, string data_table_name)
     {
         return "--start:byid\n  SELECT [Id]\n      ,[ContentType]\n      ,[Version]\n      ,[Created]\n      ,[Updated]\n      ,[CreatedBy]\n      ,[UpdatedBy]\n      ,[Data]\n      ,[RV]\n  FROM [" + schema + "].[" + data_table_name + "]\n  WHERE [Id] = @id;\n--end:byid\n\n--start:paged\nSELECT [Id]\n      ,[Version]\n      ,[Created]\n      ,[Updated]\n      ,[CreatedBy]\n      ,[UpdatedBy]\n      ,[Data]\n      ,[RV]\n  FROM [" + schema + "].[" + data_table_name + "]\n  ORDER BY [Created]\n  OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;\n--end:paged";
     }
}