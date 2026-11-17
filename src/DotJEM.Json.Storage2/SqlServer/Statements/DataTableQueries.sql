--start:CreateTable
CREATE TABLE [@{schema}].[@{area_name}.data] (
                                                 [Id] [uniqueidentifier] NOT NULL,
                                                 [Version] [int] NOT NULL,
                                                 [ContentType] [varchar](64) NOT NULL,
                                                 [Created] [datetime2] NOT NULL,
                                                 [Updated] [datetime2] NOT NULL,
                                                 [CreatedBy] [nvarchar](256) NULL,
                                                 [UpdatedBy] [nvarchar](256) NULL,
                                                 [Meta] [nvarchar](max) NOT NULL,
                                                 [Data] [nvarchar](max) NOT NULL,
                                                 [RV] [rowversion] NOT NULL,
                                                 CONSTRAINT [PK_@{area_name}.data] PRIMARY KEY CLUSTERED ( [Id] ASC )
                                                     WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];

ALTER TABLE [@{schema}].[@{area_name}.data] ADD CONSTRAINT [DF_@{area_name}.data_Id] DEFAULT (NEWSEQUENTIALID()) FOR [Id];
--ALTER TABLE [@{schema}].[@{area_name}.data] ADD CONSTRAINT [DF_@{area_name}.data_Updated] DEFAULT (getdate()) FOR [Updated];
--end

--start:SelectById
SELECT [Id]
      ,[ContentType]
      ,[Version]
      ,[Created]
      ,[Updated]
      ,[CreatedBy]
      ,[UpdatedBy]
      ,[Data]
      ,[RV]
  FROM [@{schema}].[@{area_name}.data]
  WHERE [Id] = @id;
--end

--start:SelectPaged
SELECT [Id]
      ,[ContentType]
      ,[Version]
      ,[Created]
      ,[Updated]
      ,[CreatedBy]
      ,[UpdatedBy]
      ,[Data]
      ,[RV]
  FROM [@{schema}].[@{area_name}.data]
  ORDER BY [Created]
  OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;
--end

--start:Insert
INSERT INTO [@{schema}].[@{area_name}.data]
           ([ContentType]
           ,[Version]
           ,[Created]
           ,[Updated]
           ,[CreatedBy]
           ,[UpdatedBy]
           ,[Data])
     OUTPUT 'C', INSERTED.[Id], INSERTED.[Version], @timestamp, INSERTED.[CreatedBy], INSERTED.[Data] 
		INTO [@{schema}].[@{area_name}.log]([Event], [Id], [Version], [Time], [User], [Data])
     OUTPUT 
            INSERTED.[Id]
     VALUES
           (@contentType
           ,0
           ,@timestamp
           ,@timestamp
           ,@user
           ,@user
           ,@data);
--end

--start:Update
UPDATE [@{schema}].[@{area_name}.data]
   SET [Version] = [Version] + 1
      ,[Updated] = @timestamp
	  ,[UpdatedBy] = @user
      ,[Data] = @data
	  OUTPUT 'U', INSERTED.[Id], INSERTED.[Version], @timestamp, @user, INSERTED.[Data]
		INTO [@{schema}].[@{area_name}.log]([Event], [Id], [Version], [Time], [User], [Data])
	  OUTPUT INSERTED.[ContentType], INSERTED.[Version], INSERTED.[Created], INSERTED.[CreatedBy]
 WHERE [Id] = @id;
--end

--start:Delete
 DELETE FROM [@{schema}].[@{area_name}.data]
	  OUTPUT 'D', DELETED.[Id], -1, @timestamp, @user, '{}' 
		INTO [@{schema}].[@{area_name}.log]([Event], [Id], [Version], [Time], [User], [Data])
	  OUTPUT DELETED.[Id]
            ,DELETED.[ContentType]
            ,DELETED.[Version]
            ,DELETED.[Created]
            ,DELETED.[Updated]
            ,DELETED.[CreatedBy]
            ,DELETED.[UpdatedBy]
            ,DELETED.[Data]
 WHERE [Id] = @id;
--end

--start:Exists
SELECT TOP 1 COUNT(*)
  FROM INFORMATION_SCHEMA.TABLES
  WHERE TABLE_SCHEMA = '@{schema}'
    AND TABLE_NAME = '@{area_name}.data';
--end