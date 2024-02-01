namespace DotJEM.Json.Storage2.Generated;

internal static partial class SqlFiles
{
     public static string Temp_default(string schema, string data_table_name, string log_table_name)
     {
         return "UPDATE [" + schema + "].[" + data_table_name + "]\n   SET [Version] = [Version] + 1\n      ,[Updated] = @timestamp\n	  ,[UpdatedBy] = @user\n      ,[Data] = @data\n	  OUTPUT 'U', INSERTED.[Id], INSERTED.[Version], @timestamp, @user, INSERTED.[Data]\n		INTO [" + schema + "].[" + log_table_name + "]([Event], [Id], [Version], [Time], [User], [Data])\n	  OUTPUT INSERTED.[ContentType], INSERTED.[Version], INSERTED.[Created], INSERTED.[CreatedBy]\n WHERE [Id] = @id;\n --end:default\n";
     }
}