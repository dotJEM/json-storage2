﻿UPDATE [@{schema}].[@{area_name}.data]
   SET [Version] = [Version] + 1
      ,[Updated] = @timestamp
	  ,[UpdatedBy] = @user
      ,[Data] = @data
	  OUTPUT 'U', INSERTED.[Id], INSERTED.[Version], @timestamp, @user, INSERTED.[Data]
		INTO [@{schema}].[@{area_name}.log]([Event], [Id], [Version], [Time], [User], [Data])
	  OUTPUT INSERTED.[ContentType], INSERTED.[Version], INSERTED.[Created], INSERTED.[CreatedBy]
 WHERE [Id] = @id;