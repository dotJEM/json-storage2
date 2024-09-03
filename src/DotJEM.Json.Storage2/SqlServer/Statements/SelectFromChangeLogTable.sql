--start:byid
SELECT TOP (@count) 
       [Revision]
      ,[Id]
      ,[Event]
      ,[Time]
      ,[User]
      ,[Version]
      ,[Data]
  FROM (
	SELECT MAX([Revision]) as LatestRevision
	  FROM [@{schema}].[@{area_name}.log]
	  WHERE [Revision] > @start
      GROUP BY [Id] 
  ) temp
  JOIN [@{schema}].[@{area_name}.log] cl ON cl.[Revision] = temp.[LatestRevision]
 ORDER BY [Revision];
--end:byid

--start:paged
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
  ORDER BY [Created]
  OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;
--end:paged