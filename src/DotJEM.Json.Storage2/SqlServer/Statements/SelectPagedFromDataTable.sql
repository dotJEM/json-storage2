SELECT [Id]
      ,[Version]
      ,[Created]
      ,[Updated]
      ,[Data]
      ,[RV]
  FROM [@{schema}].[@{data_table_name}]
  ORDER BY [Created]
  OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY