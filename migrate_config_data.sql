-- ============================================================
-- STEP 1: Run this entire script on the OLD server
--         Server: 45.150.188.26,4420  DB: serviceconnect
--
-- It prints INSERT statements. Copy that output and run it
-- on the NEW server: 130.49.168.109,1433  DB: Serviceconnect
-- ============================================================

SET NOCOUNT ON;

DECLARE @TableName   NVARCHAR(256)
DECLARE @SchemaName  NVARCHAR(128)
DECLARE @RowCnt      BIGINT
DECLARE @TopN        INT = 10          -- max rows to copy for large tables
DECLARE @LargeLimit  INT = 10          -- tables with more rows than this get TOP @TopN
DECLARE @sql         NVARCHAR(MAX)
DECLARE @cols        NVARCHAR(MAX)
DECLARE @insert      NVARCHAR(MAX)

-- Cursor over every user table with its live row count
DECLARE tbl_cursor CURSOR FOR
    SELECT
        s.name        AS SchemaName,
        t.name        AS TableName,
        p.rows        AS RowCnt
    FROM sys.tables           t
    JOIN sys.schemas          s ON s.schema_id = t.schema_id
    JOIN sys.indexes          i ON i.object_id = t.object_id AND i.index_id IN (0,1)
    JOIN sys.partitions       p ON p.object_id = t.object_id AND p.index_id = i.index_id
    ORDER BY p.rows DESC, s.name, t.name;

OPEN tbl_cursor;
FETCH NEXT FROM tbl_cursor INTO @SchemaName, @TableName, @RowCnt;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Build column list (skip identity / computed / timestamp columns for INSERT)
    SET @cols = NULL;
    SELECT @cols = COALESCE(@cols + ', ', '') + QUOTENAME(c.name)
    FROM sys.columns  c
    JOIN sys.tables   t ON t.object_id = c.object_id
    JOIN sys.schemas  s ON s.schema_id = t.schema_id
    WHERE t.name       = @TableName
      AND s.name       = @SchemaName
      AND c.is_computed = 0
      AND c.system_type_id <> 189  -- timestamp/rowversion
    ORDER BY c.column_id;

    IF @cols IS NULL
    BEGIN
        FETCH NEXT FROM tbl_cursor INTO @SchemaName, @TableName, @RowCnt;
        CONTINUE;
    END

    -- Check if table has an identity column (need IDENTITY_INSERT)
    DECLARE @hasIdentity BIT = 0;
    IF EXISTS (
        SELECT 1 FROM sys.columns c
        JOIN sys.tables  t ON t.object_id = c.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE t.name = @TableName AND s.name = @SchemaName AND c.is_identity = 1
    ) SET @hasIdentity = 1;

    -- Decide whether to copy all rows or just TOP @TopN
    DECLARE @topClause NVARCHAR(20) = '';
    IF @RowCnt > @LargeLimit
        SET @topClause = 'TOP ' + CAST(@TopN AS NVARCHAR) + ' ';

    SET @insert = '';

    -- Header comment
    SET @insert = @insert + CHAR(13)+CHAR(10) +
        '-- Table: [' + @SchemaName + '].[' + @TableName + ']' +
        '  rows on source: ' + CAST(@RowCnt AS NVARCHAR) +
        CASE WHEN @RowCnt > @LargeLimit THEN '  (copying TOP '+CAST(@TopN AS NVARCHAR)+')' ELSE '  (copying all)' END +
        CHAR(13)+CHAR(10);

    IF @hasIdentity = 1
        SET @insert = @insert +
            'SET IDENTITY_INSERT [' + @SchemaName + '].[' + @TableName + '] ON;' + CHAR(13)+CHAR(10);

    SET @insert = @insert +
        'INSERT INTO [' + @SchemaName + '].[' + @TableName + '] (' + @cols + ')' + CHAR(13)+CHAR(10) +
        'SELECT ' + @topClause + @cols + CHAR(13)+CHAR(10) +
        'FROM [' + @SchemaName + '].[' + @TableName + '];' + CHAR(13)+CHAR(10);

    IF @hasIdentity = 1
        SET @insert = @insert +
            'SET IDENTITY_INSERT [' + @SchemaName + '].[' + @TableName + '] OFF;' + CHAR(13)+CHAR(10);

    PRINT @insert;

    FETCH NEXT FROM tbl_cursor INTO @SchemaName, @TableName, @RowCnt;
END

CLOSE tbl_cursor;
DEALLOCATE tbl_cursor;

PRINT '-- ========== End of generated script ==========';
