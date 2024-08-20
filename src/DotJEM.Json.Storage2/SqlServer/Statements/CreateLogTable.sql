CREATE TABLE [@{schema}].[@{area_name}.log] (
    [Revision] [bigint] IDENTITY(1,1) NOT NULL,
    [Id] [uniqueidentifier] NOT NULL,
    [Event] [varchar](1) NOT NULL,
    [Time] [datetime] NOT NULL,
    [User] [nvarchar](256) NULL, 
    [Version] [bigint] NOT NULL,
    [Data] [nvarchar](max) NOT NULL,
    CONSTRAINT [PK_@{area_name}.log] PRIMARY KEY CLUSTERED ( [Revision] ASC )
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];