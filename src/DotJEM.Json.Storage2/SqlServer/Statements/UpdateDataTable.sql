--start:default
UPDATE [@{schema}].[@{data_table_name}]
   SET [Version] = [Version] + 1
      ,[Updated] = @timestamp
      ,[Data] = @data
	  OUTPUT 'U', DELETED.[Id], DELETED.[Version], @timestamp, DELETED.[Data] 
		INTO [@{schema}].[@{log_table_name}]([Event], [Id], [Version], [Time], [Data])
	  OUTPUT INSERTED.[ContentType], INSERTED.[Version], INSERTED.[Created]
 WHERE [Id] = @id;
 --end:default