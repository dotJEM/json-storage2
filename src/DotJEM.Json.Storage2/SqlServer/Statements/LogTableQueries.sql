--start:CreateTable
CREATE TABLE [@{schema}].[@{area_name}.log] (
    [Revision] [bigint] IDENTITY(1,1) NOT NULL,
    [Id] [uniqueidentifier] NOT NULL,
    [Event] [varchar](1) NOT NULL,
    [Time] [datetime2] NOT NULL,
    [User] [nvarchar](256) NULL, 
    [Version] [int] NOT NULL,
    [Meta] [nvarchar](max) NOT NULL,
    [Data] [nvarchar](max) NOT NULL,
    CONSTRAINT [PK_@{area_name}.log] PRIMARY KEY CLUSTERED ( [Revision] ASC )
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
--end

--start:SelectPaged
SELECT TOP (@count) 
        logTable.[Revision]
       ,logTable.[Event]
       ,dataTable.[Id]
       ,dataTable.[Created]
       ,dataTable.[Updated]
       ,dataTable.[Version]
       ,dataTable.[ContentType]
       ,dataTable.[Created]
       ,dataTable.[Updated]
       ,dataTable.[CreatedBy]
       ,dataTable.[UpdatedBy]
       ,dataTable.[Meta]
       ,dataTable.[Data]
FROM (
    SELECT MAX([Revision]) as Latest
    FROM [@{schema}].[@{area_name}.log]
    WHERE [Revision] > @start
    GROUP BY [Id]
    ) temp
    JOIN [@{schema}].[@{area_name}.log] logTable ON logTable.Revision = temp.Latest
    JOIN [@{schema}].[@{area_name}.data] dataTable on logTable.Id = dataTable.Id
ORDER BY [Revision]
--end

--start:SelectLatestGeneration
SELECT MAX([Revision]) FROM [@{schema}].[@{area_name}.log]
--end