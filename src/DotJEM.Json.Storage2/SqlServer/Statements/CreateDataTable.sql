CREATE TABLE [@{schema}].[@{area_name}.data] (
    [Id] [uniqueidentifier] NOT NULL,
    [Version] [int] NOT NULL,
    [ContentType] [varchar](64) NOT NULL,
    [Created] [datetime2] NOT NULL,
    [Updated] [datetime2] NOT NULL,
    [CreatedBy] [nvarchar](256) NULL,
    [UpdatedBy] [nvarchar](256) NULL,
    [Data] [nvarchar](max) NOT NULL,
    [RV] [rowversion] NOT NULL,
    CONSTRAINT [PK_@{area_name}.data] PRIMARY KEY CLUSTERED ( [Id] ASC )
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];

ALTER TABLE [@{schema}].[@{area_name}.data] ADD CONSTRAINT [DF_@{area_name}.data_Id] DEFAULT (NEWSEQUENTIALID()) FOR [Id];
--ALTER TABLE [@{schema}].[@{area_name}.data] ADD CONSTRAINT [DF_@{area_name}.data_Updated] DEFAULT (getdate()) FOR [Updated];