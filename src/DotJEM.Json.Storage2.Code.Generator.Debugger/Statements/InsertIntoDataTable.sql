--start:default
INSERT INTO [@{schema}].[@{data_table_name}]
           ([ContentType]
           ,[Version]
           ,[Created]
           ,[Updated]
           ,[CreatedBy]
           ,[UpdatedBy]
           ,[Data])
     OUTPUT 'C', INSERTED.[Id], INSERTED.[Version], @timestamp, INSERTED.[CreatedBy], INSERTED.[Data] 
		INTO [@{schema}].[@{log_table_name}]([Event], [Id], [Version], [Time], [User], [Data])
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
--end:default
