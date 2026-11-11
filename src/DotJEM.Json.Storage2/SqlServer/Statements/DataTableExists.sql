SELECT TOP 1 COUNT(*)
  FROM INFORMATION_SCHEMA.TABLES
  WHERE TABLE_SCHEMA = @schema
    AND TABLE_NAME = @{area_name}.data;

--this["LogTableExists"] = Vars.Format(
--    @"SELECT TOP 1 COUNT(*) FROM INFORMATION_SCHEMA.TABLES
--                               WHERE TABLE_SCHEMA = 'dbo'
--                                 AND TABLE_NAME = '{logTableName}'");

--SELECT
--    TABLE_NAME
--FROM INFORMATION_SCHEMA.TABLES
--WHERE TABLE_SCHEMA = @schema
--  AND TABLE_TYPE = 'BASE TABLE';
