namespace DotJEM.Json.Storage2.Generated;

internal static partial class SqlFiles
{
     public static string InsertIntoDataTable_default(string schema, string data_table_name, string log_table_name)
     {
         return "INSERT INTO [" + schema + "].[" + data_table_name + "]\n           ([ContentType]\n           ,[Version]\n           ,[Created]\n           ,[Updated]\n           ,[CreatedBy]\n           ,[UpdatedBy]\n           ,[Data])\n     OUTPUT 'C', INSERTED.[Id], INSERTED.[Version], @timestamp, INSERTED.[CreatedBy], INSERTED.[Data] \n		INTO [" + schema + "].[" + log_table_name + "]([Event], [Id], [Version], [Time], [User], [Data])\n     OUTPUT \n            INSERTED.[Id]\n     VALUES\n           (@contentType\n           ,0\n           ,@timestamp\n           ,@timestamp\n           ,@user\n           ,@user\n           ,@data);\n";
     }
}