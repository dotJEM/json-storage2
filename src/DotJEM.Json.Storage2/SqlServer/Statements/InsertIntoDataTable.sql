--start:normal
INSERT INTO [@{schema}].[@{data_table_name}]
           ([ContentType]
           ,[Version]
           ,[Created]
           ,[Updated]
           ,[Data])
     OUTPUT 'CREATE', INSERTED.[Id], INSERTED.[Version], @timestamp, INSERTED.[Data] 
		INTO [@{schema}].[@{log_table_name}]([Event], [Id], [Version], [Time], [Data])
     OUTPUT 
            INSERTED.[Id]
     VALUES
           (@contentType
           ,0
           ,@timestamp
           ,@timestamp
           ,@data);
--end:normal