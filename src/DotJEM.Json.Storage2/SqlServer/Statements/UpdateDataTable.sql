UPDATE [@{schema}].[@{data_table_name}]
   SET [Version] = [Version] + 1
      ,[Updated] = @timestamp
      ,[Data] = @data
	  OUTPUT 'UPDATE', DELETED.[Id], DELETED.[Version], @timestamp, DELETED.[Data] 
		INTO [dbo].[@{log_table_name}]([Event], [Id], [Version], [Time], [Data])
	  OUTPUT INSERTED.*
 WHERE [Id] = @id