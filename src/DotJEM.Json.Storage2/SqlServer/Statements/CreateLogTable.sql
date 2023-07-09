CREATE TABLE [@{schema}].[@{log_table_name}] (
    [Revision] [bigint] IDENTITY(1,1) NOT NULL,
    [Event] [varchar](1) NOT NULL,
    [Id] [uniqueidentifier] NOT NULL,
    [Time] [datetime] NOT NULL,
    [Version] [bigint] NOT NULL,
    [Data] [nvarchar](max) NOT NULL,
    CONSTRAINT [PK_@{log_table_name}] PRIMARY KEY CLUSTERED ( [Revision] ASC )
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];