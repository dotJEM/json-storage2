namespace DotJEM.Json.Storage2.Generated;

internal static partial class SqlFiles
{
     public static string CreateDataTable_default(string schema, string data_table_name)
     {
         return "CREATE TABLE [" + schema + "].[" + data_table_name + "] (\n    [Id] [uniqueidentifier] NOT NULL,\n    [Version] [int] NOT NULL,\n    [ContentType] [varchar](64) NOT NULL,\n    [Created] [datetime] NOT NULL,\n    [Updated] [datetime] NOT NULL,\n    [CreatedBy] [nvarchar](256) NULL,\n    [UpdatedBy] [nvarchar](256) NULL,\n    [Data] [nvarchar](max) NOT NULL,\n    [RV] [rowversion] NOT NULL,\n    CONSTRAINT [PK_" + data_table_name + "] PRIMARY KEY CLUSTERED ( [Id] ASC )\n    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]\n) ON [PRIMARY];\n\nALTER TABLE [" + schema + "].[" + data_table_name + "] ADD CONSTRAINT [DF_" + data_table_name + "_Id] DEFAULT (NEWSEQUENTIALID()) FOR [Id];\n\n\n-- ALTER TABLE [dbo].[" + data_table_name + "] ADD  CONSTRAINT [DF_" + data_table_name + "_Updated]  DEFAULT (getdate()) FOR [Updated]\n";
     }
}