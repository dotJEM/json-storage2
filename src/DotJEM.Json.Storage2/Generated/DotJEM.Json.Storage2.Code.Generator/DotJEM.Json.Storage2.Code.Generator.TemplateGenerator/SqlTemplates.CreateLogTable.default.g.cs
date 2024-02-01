namespace DotJEM.Json.Storage2.Generated;

internal static partial class SqlFiles
{
     public static string CreateLogTable_default(string schema, string log_table_name)
     {
         return "CREATE TABLE [" + schema + "].[" + log_table_name + "] (\n    [Revision] [bigint] IDENTITY(1,1) NOT NULL,\n    [Id] [uniqueidentifier] NOT NULL,\n    [Event] [varchar](1) NOT NULL,\n    [Time] [datetime] NOT NULL,\n    [User] [nvarchar](256) NULL, \n    [Version] [bigint] NOT NULL,\n    [Data] [nvarchar](max) NOT NULL,\n    CONSTRAINT [PK_" + log_table_name + "] PRIMARY KEY CLUSTERED ( [Revision] ASC )\n    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]\n) ON [PRIMARY];\n";
     }
}