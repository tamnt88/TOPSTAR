param(
  [string]$Token = '260a0c75-27fb-11f1-a162-aaf47df3acaf',
  [string]$ShopId = '6341472',
  [string]$SqlServer = 'SHENLUNG\SQLSERVER2022',
  [string]$SqlUser = 'sa',
  [string]$SqlPassword = '123456',
  [string]$Database = 'TopstarDb'
)

$ErrorActionPreference = 'Stop'

$headers = @{ Token = $Token; ShopId = $ShopId }

Write-Host 'Calling GHN provinces...'
$provinceRes = Invoke-RestMethod -Method Get -Uri 'https://online-gateway.ghn.vn/shiip/public-api/master-data/province' -Headers $headers
if ($provinceRes.code -ne 200) { throw "Province API failed: $($provinceRes.message)" }
$provinces = @($provinceRes.data)

Write-Host 'Calling GHN districts...'
$districtRes = Invoke-RestMethod -Method Get -Uri 'https://online-gateway.ghn.vn/shiip/public-api/master-data/district' -Headers $headers
if ($districtRes.code -ne 200) { throw "District API failed: $($districtRes.message)" }
$districts = @($districtRes.data)

$districtProvinceMap = @{}
foreach($d in $districts){ $districtProvinceMap[[int]$d.DistrictID] = [int]$d.ProvinceID }

$wardsByCode = @{}
$idx = 0
$total = $districts.Count
foreach($d in $districts){
  $idx++
  if(($idx % 30) -eq 0 -or $idx -eq 1 -or $idx -eq $total){ Write-Host "Fetching wards $idx/$total..." }
  $body = @{ district_id = [int]$d.DistrictID } | ConvertTo-Json
  $wardRes = Invoke-RestMethod -Method Post -Uri 'https://online-gateway.ghn.vn/shiip/public-api/master-data/ward' -Headers $headers -ContentType 'application/json' -Body $body
  if($wardRes.code -ne 200){ continue }
  foreach($w in @($wardRes.data)){
    $code = [string]$w.WardCode
    if([string]::IsNullOrWhiteSpace($code)){ continue }
    $code = $code.Trim()
    if(-not $wardsByCode.ContainsKey($code)){
      $provinceCode = $districtProvinceMap[[int]$w.DistrictID]
      $wardsByCode[$code] = [PSCustomObject]@{
        WardCode = $code
        ProvinceCode = [int]$provinceCode
        Name = [string]$w.WardName
        IsActive = ([int]$w.Status -eq 1)
      }
    }
  }
}

Write-Host "Collected provinces: $($provinces.Count), wards(unique): $($wardsByCode.Count)"

Add-Type -AssemblyName 'System.Data'

$connStr = "Server=$SqlServer;Database=$Database;User Id=$SqlUser;Password=$SqlPassword;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$tx = $conn.BeginTransaction()

try {
  $cmd = $conn.CreateCommand()
  $cmd.Transaction = $tx
  $cmd.CommandText = @"
IF OBJECT_ID('tempdb..#TmpProvinces') IS NOT NULL DROP TABLE #TmpProvinces;
IF OBJECT_ID('tempdb..#TmpWards') IS NOT NULL DROP TABLE #TmpWards;

CREATE TABLE #TmpProvinces(
  ProvinceCode INT NOT NULL,
  [Name] NVARCHAR(150) NOT NULL,
  IsActive BIT NOT NULL
);

CREATE TABLE #TmpWards(
  WardCode NVARCHAR(50) NOT NULL,
  ProvinceCode INT NOT NULL,
  [Name] NVARCHAR(150) NOT NULL,
  IsActive BIT NOT NULL
);
"@
  [void]$cmd.ExecuteNonQuery()

  $dtP = New-Object System.Data.DataTable
  [void]$dtP.Columns.Add('ProvinceCode', [int])
  [void]$dtP.Columns.Add('Name', [string])
  [void]$dtP.Columns.Add('IsActive', [bool])
  foreach($p in $provinces){
    $row = $dtP.NewRow()
    $row['ProvinceCode'] = [int]$p.ProvinceID
    $row['Name'] = [string]$p.ProvinceName
    $row['IsActive'] = ([int]$p.Status -eq 1)
    $dtP.Rows.Add($row)
  }

  $bulkP = New-Object System.Data.SqlClient.SqlBulkCopy($conn, [System.Data.SqlClient.SqlBulkCopyOptions]::Default, $tx)
  $bulkP.DestinationTableName = '#TmpProvinces'
  [void]$bulkP.ColumnMappings.Add('ProvinceCode','ProvinceCode')
  [void]$bulkP.ColumnMappings.Add('Name','Name')
  [void]$bulkP.ColumnMappings.Add('IsActive','IsActive')
  $bulkP.WriteToServer($dtP)

  $dtW = New-Object System.Data.DataTable
  [void]$dtW.Columns.Add('WardCode', [string])
  [void]$dtW.Columns.Add('ProvinceCode', [int])
  [void]$dtW.Columns.Add('Name', [string])
  [void]$dtW.Columns.Add('IsActive', [bool])
  foreach($w in $wardsByCode.Values){
    $row = $dtW.NewRow()
    $row['WardCode'] = [string]$w.WardCode
    $row['ProvinceCode'] = [int]$w.ProvinceCode
    $row['Name'] = [string]$w.Name
    $row['IsActive'] = [bool]$w.IsActive
    $dtW.Rows.Add($row)
  }

  $bulkW = New-Object System.Data.SqlClient.SqlBulkCopy($conn, [System.Data.SqlClient.SqlBulkCopyOptions]::Default, $tx)
  $bulkW.DestinationTableName = '#TmpWards'
  [void]$bulkW.ColumnMappings.Add('WardCode','WardCode')
  [void]$bulkW.ColumnMappings.Add('ProvinceCode','ProvinceCode')
  [void]$bulkW.ColumnMappings.Add('Name','Name')
  [void]$bulkW.ColumnMappings.Add('IsActive','IsActive')
  $bulkW.WriteToServer($dtW)

  $cmd2 = $conn.CreateCommand()
  $cmd2.Transaction = $tx
  $cmd2.CommandText = @"
MERGE dbo.Provinces AS target
USING #TmpProvinces AS src
ON target.ProvinceCode = src.ProvinceCode
WHEN MATCHED THEN UPDATE SET
  target.[Name] = src.[Name],
  target.IsActive = src.IsActive,
  target.UpdatedAtUtc = SYSUTCDATETIME()
WHEN NOT MATCHED BY TARGET THEN
  INSERT (Id, ProvinceCode, [Name], CodeName, IsActive, CreatedAtUtc)
  VALUES (NEWID(), src.ProvinceCode, src.[Name], NULL, src.IsActive, SYSUTCDATETIME())
WHEN NOT MATCHED BY SOURCE THEN
  UPDATE SET target.IsActive = 0, target.UpdatedAtUtc = SYSUTCDATETIME();

MERGE dbo.Wards AS target
USING (
  SELECT w.WardCode, p.Id AS ProvinceId, w.[Name], w.IsActive
  FROM #TmpWards w
  INNER JOIN dbo.Provinces p ON p.ProvinceCode = w.ProvinceCode
) AS src
ON target.WardCode = src.WardCode
WHEN MATCHED THEN UPDATE SET
  target.ProvinceId = src.ProvinceId,
  target.[Name] = src.[Name],
  target.IsActive = src.IsActive,
  target.UpdatedAtUtc = SYSUTCDATETIME()
WHEN NOT MATCHED BY TARGET THEN
  INSERT (Id, WardCode, ProvinceId, [Name], CodeName, IsActive, CreatedAtUtc)
  VALUES (NEWID(), src.WardCode, src.ProvinceId, src.[Name], NULL, src.IsActive, SYSUTCDATETIME())
WHEN NOT MATCHED BY SOURCE THEN
  UPDATE SET target.IsActive = 0, target.UpdatedAtUtc = SYSUTCDATETIME();

SELECT COUNT(*) AS ProvinceCount FROM dbo.Provinces;
SELECT COUNT(*) AS WardCount FROM dbo.Wards;
"@

  $reader = $cmd2.ExecuteReader()
  $counts = @()
  do {
    while($reader.Read()){
      $counts += $reader.GetInt32(0)
    }
  } while($reader.NextResult())
  $reader.Close()

  $tx.Commit()
  Write-Host "Sync done. Provinces in DB: $($counts[0]); Wards in DB: $($counts[1])"
}
catch {
  try { $tx.Rollback() } catch {}
  throw
}
finally {
  $conn.Close()
}

