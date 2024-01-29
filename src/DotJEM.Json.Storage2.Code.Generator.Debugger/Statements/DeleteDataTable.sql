DELETE FROM [@{schema}].[@{data_table_name}]
	  OUTPUT 'D', DELETED.[Id], -1, @timestamp, @user, '{}' 
		INTO [dbo].[@{log_table_name}]([Event], [Id], [Version], [Time], [User], [Data])
	  OUTPUT DELETED.[Id]
            ,DELETED.[ContentType]
            ,DELETED.[Version]
            ,DELETED.[Created]
            ,DELETED.[Updated]
            ,DELETED.[CreatedBy]
            ,DELETED.[UpdatedBy]
            ,DELETED.[Data]
 WHERE [Id] = @id;