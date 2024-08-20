--start:byid
  SELECT [Id]
      ,[ContentType]
      ,[Version]
      ,[Created]
      ,[Updated]
      ,[CreatedBy]
      ,[UpdatedBy]
      ,[Data]
      ,[RV]
  FROM [@{schema}].[@{area_name}.data]
  WHERE [Id] = @id;
--end:byid

--start:paged
SELECT [Id]
      ,[Version]
      ,[Created]
      ,[Updated]
      ,[CreatedBy]
      ,[UpdatedBy]
      ,[Data]
      ,[RV]
  FROM [@{schema}].[@{area_name}.data]
  ORDER BY [Created]
  OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;
--end:paged