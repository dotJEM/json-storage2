namespace DotJEM.Json.Storage2.Generated;

internal static partial class SqlFiles
{
     public static string DeleteDataTable(string schema, string data_table_name, string log_table_name)
     {
         return "DELETE FROM [" + schema + "].[" + data_table_name + "]\n	  OUTPUT 'D', DELETED.[Id], -1, @timestamp, @user, '{}' \n		INTO [dbo].[" + log_table_name + "]([Event], [Id], [Version], [Time], [User], [Data])\n	  OUTPUT DELETED.[Id]\n            ,DELETED.[ContentType]\n            ,DELETED.[Version]\n            ,DELETED.[Created]\n            ,DELETED.[Updated]\n            ,DELETED.[CreatedBy]\n            ,DELETED.[UpdatedBy]\n            ,DELETED.[Data]\n WHERE [Id] = @id;";
     }
}