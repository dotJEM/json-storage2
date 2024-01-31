											--start:default
UPDATE [@{schema}].[@{data_table_name}]
   SET [Version] = [Version] + 1
      ,[Updated] = @timestamp
	  ,[UpdatedBy] = @user
      ,[Data] = @data
	  OUTPUT 'U', INSERTED.[Id], INSERTED.[Version], @timestamp, @user, INSERTED.[Data]
		INTO [@{schema}].[@{log_table_name}]([Event], [Id], [Version], [Time], [User], [Data])
	  OUTPUT INSERTED.[ContentType], INSERTED.[Version], INSERTED.[Created], INSERTED.[CreatedBy]
 WHERE [Id] = @id;
 --end:default