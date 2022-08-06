--start:full
INSERT INTO [@{schema}].[@{data_table_name}]
           ([Id]
           ,[Version]
           ,[ContentType]
           ,[Created]
           ,[Updated]
           ,[Data])
     VALUES
           (@id
           ,@version
           ,@contentType
           ,@created
           ,@updated
           ,@data);
--end:full

--start:normal
INSERT INTO [@{schema}].[@{data_table_name}]
           ([ContentType]
           ,[Version]
           ,[Created]
           ,[Updated]
           ,[Data])
     OUTPUT 
            INSERTED.[Id]
     VALUES
           (@contentType
           ,0
           ,@timestamp
           ,@timestamp
           ,@data);
--end:normal