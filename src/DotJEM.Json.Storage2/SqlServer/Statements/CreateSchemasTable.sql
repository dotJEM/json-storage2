CREATE TABLE [@{schema}].[@{area_name}.schemas] (
    [Id] [uniqueidentifier] NOT NULL,
    [Version] [int] NOT NULL,
    [ContentType] [varchar](64) NOT NULL,
    [Created] [datetime2] NOT NULL,
    [Updated] [datetime2] NOT NULL,
    [Data] [nvarchar](max) NOT NULL,
    [RV] [rowversion] NOT NULL,
    CONSTRAINT [PK_@{area_name}.schemas] PRIMARY KEY CLUSTERED ( [Id] ASC )
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];