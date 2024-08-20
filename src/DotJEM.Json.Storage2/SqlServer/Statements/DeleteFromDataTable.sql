DELETE FROM [@{schema}].[@{area_name}.data]
	  OUTPUT 'D', DELETED.[Id], -1, @timestamp, @user, '{}' 
		INTO [dbo].[@{area_name}.log]([Event], [Id], [Version], [Time], [User], [Data])
	  OUTPUT DELETED.[Id]
            ,DELETED.[ContentType]
            ,DELETED.[Version]
            ,DELETED.[Created]
            ,DELETED.[Updated]
            ,DELETED.[CreatedBy]
            ,DELETED.[UpdatedBy]
            ,DELETED.[Data]
 WHERE [Id] = @id;