namespace DotJEM.Json.Storage2.Generated;

internal static partial class SqlFiles
{
     public static string CreateSchemasTable(string schema, string schema_table_name)
     {
         return "CREATE TABLE [" + schema + "].[" + schema_table_name + "] (\n    [Id] [uniqueidentifier] NOT NULL,\n    [Version] [int] NOT NULL,\n    [ContentType] [varchar](64) NOT NULL,\n    [Created] [datetime] NOT NULL,\n    [Updated] [datetime] NOT NULL,\n    [Data] [nvarchar](max) NOT NULL,\n    [RV] [rowversion] NOT NULL,\n    CONSTRAINT [PK_" + schema_table_name + "] PRIMARY KEY CLUSTERED ( [Id] ASC )\n    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]\n) ON [PRIMARY];";
     }
}