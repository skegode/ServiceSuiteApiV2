# Cross-server data migration  SOURCE -> TARGET
# SOURCE: 45.150.188.26,4420  / CollectBox_v3  (has data)
# TARGET: 102.214.69.233,4410 / CollectBox_v3  (schema only)
# Rule  : tables <= 10 rows -> all rows ; tables > 10 rows -> TOP 10 rows

$srcStr = "Server=45.150.188.26,4420;Initial Catalog=CollectBox_v3;User ID=tester;Password=Ngong123@;MultipleActiveResultSets=True;TrustServerCertificate=True"
$dstStr = "Server=102.214.69.233,4410;Initial Catalog=CollectBox_v3;User ID=Tough;Password=Tough123!@#;MultipleActiveResultSets=True;TrustServerCertificate=True"
$topN   = 10
$limit  = 10

function Get-Table($connStr, $sql) {
    $cn  = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $da  = New-Object System.Data.SqlClient.SqlDataAdapter($sql, $cn)
    $dt  = New-Object System.Data.DataTable
    $null = $da.Fill($dt)
    $cn.Close()
    return ,$dt   # comma prevents PowerShell from enumerating rows on return
}

# ---- table list ----
Write-Host "Reading table list from source..."
$listSql = @"
SELECT s.name AS SchemaName, t.name AS TableName, p.rows AS RowCnt
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.indexes i ON i.object_id = t.object_id AND i.index_id IN (0,1)
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id = i.index_id
ORDER BY p.rows ASC, s.name, t.name
"@
$list = Get-Table $srcStr $listSql
Write-Host "Found $($list.Rows.Count) tables.`n"

foreach ($row in $list.Rows) {
    $schema   = $row["SchemaName"]
    $tbl      = $row["TableName"]
    $rowCnt   = [long]$row["RowCnt"]
    $fullName = "[$schema].[$tbl]"
    $topClause = if ($rowCnt -gt $limit) { "TOP $topN " } else { "" }
    $label     = if ($rowCnt -gt $limit) { "TOP $topN / $rowCnt rows" } else { "all $rowCnt rows" }

    # check identity column
    $idSql = "SELECT COUNT(*) AS c FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE t.name='$tbl' AND s.name='$schema' AND c.is_identity=1"
    $idDt  = Get-Table $srcStr $idSql
    $hasId = [int]$idDt.Rows[0]["c"] -gt 0

    # read data from source (exclude timestamp/rowversion columns)
    $colSql = @"
SELECT c.name
FROM sys.columns c
JOIN sys.tables  t  ON t.object_id = c.object_id
JOIN sys.schemas s  ON s.schema_id = t.schema_id
JOIN sys.types   tp ON tp.user_type_id = c.user_type_id
WHERE t.name='$tbl' AND s.name='$schema'
  AND c.is_computed=0 AND tp.name <> 'timestamp'
ORDER BY c.column_id
"@
    $colDt  = Get-Table $srcStr $colSql
    $colList = ($colDt.Rows | ForEach-Object { "[$($_["name"])]" }) -join ", "

    if (-not $colList) {
        Write-Host ("  {0,-55} no columns, skipped" -f $fullName)
        continue
    }

    $dataSql = "SELECT $topClause$colList FROM $fullName"
    try {
        $data = Get-Table $srcStr $dataSql
    } catch {
        Write-Warning "  $fullName read failed: $_"
        continue
    }

    if ($data.Rows.Count -eq 0) {
        Write-Host ("  {0,-55} 0 rows on source, skipped" -f $fullName)
        continue
    }

    # bulk copy into target
    $opts = if ($hasId) { [System.Data.SqlClient.SqlBulkCopyOptions]::KeepIdentity } `
            else         { [System.Data.SqlClient.SqlBulkCopyOptions]::Default }
    try {
        $bc = New-Object System.Data.SqlClient.SqlBulkCopy($dstStr, $opts)
        $bc.DestinationTableName = $fullName
        $bc.BulkCopyTimeout      = 120
        $bc.WriteToServer($data)
        $bc.Close()
        Write-Host ("  {0,-55} {1,4} rows inserted  [{2}]" -f $fullName, $data.Rows.Count, $label) -ForegroundColor Green
    } catch {
        $err = $_.Exception.Message -split "`n" | Select-Object -First 1
        Write-Host ("  {0,-55} SKIPPED: {1}" -f $fullName, $err) -ForegroundColor Yellow
    }
}

Write-Host "`nDone." -ForegroundColor Cyan
