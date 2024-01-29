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
  FROM [@{schema}].[@{data_table_name}]
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
  FROM [@{schema}].[@{data_table_name}]
  ORDER BY [Created]
  OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;
--end:paged