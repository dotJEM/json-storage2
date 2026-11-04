--start:paged
SELECT TOP (@count) 
        logTable.[Revision]
       ,logTable.[Event]
       ,dataTable.[Id]
       ,dataTable.[Created]
       ,dataTable.[Updated]
       ,dataTable.[Version]
       ,dataTable.[ContentType]
       ,dataTable.[Created]
       ,dataTable.[Updated]
       ,dataTable.[CreatedBy]
       ,dataTable.[UpdatedBy]
       ,dataTable.[Data]
FROM (
    SELECT MAX([Revision]) as Latest
    FROM [@{schema}].[@{area_name}.log]
    WHERE [Revision] > @start
    GROUP BY [Id]
    ) temp
    JOIN [@{schema}].[@{area_name}.log] logTable ON logTable.Revision = temp.Latest
    JOIN [@{schema}].[@{area_name}.data] dataTable on logTable.Id = dataTable.Id
ORDER BY [Revision]
--end:paged

--start:latestGeneration
SELECT MAX([Revision]) FROM [@{schema}].[@{area_name}.log]
--end:latestGeneration