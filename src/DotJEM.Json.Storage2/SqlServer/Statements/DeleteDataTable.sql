DELETE FROM [@{schema}].[@{data_table_name}]
	  OUTPUT 'D', DELETED.[Id], DELETED.[Version], @timestamp, DELETED.[Data] 
		INTO [dbo].[@{log_table_name}]([Event], [Id], [Version], [Time], [Data])
	  OUTPUT DELETED.*
 WHERE [Id] = @id;