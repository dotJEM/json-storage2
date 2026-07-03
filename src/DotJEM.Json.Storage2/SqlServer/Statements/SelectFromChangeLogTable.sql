--start:paged
SELECT TOP (@count) [Revision]
        ,[Id]
        ,[Event]
        ,[Time]
        ,[User]
        ,[Version]
        ,[Data]
FROM (
    SELECT MAX([Revision]) as Latest
    FROM [@{schema}].[@{area_name}.log]
    WHERE [Revision] > @start
    GROUP BY [Id]
    ) temp
    JOIN [@{schema}].[@{area_name}.log] cl ON cl.Revision = temp.Latest
ORDER BY [Revision]
--end:paged

--start:latestGeneration
SELECT MAX([Revision]) FROM [@{schema}].[@{area_name}.log]
--end:latestGeneration