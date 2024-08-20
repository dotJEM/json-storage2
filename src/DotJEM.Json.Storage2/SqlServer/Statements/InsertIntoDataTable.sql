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