param(
  [Parameter(Mandatory=$true)][string]$Token,
  [Parameter(Mandatory=$true)][string]$ShopId,
  [string]$SqlServer = 'SHENLUNG\SQLSERVER2022',
  [string]$SqlUser = 'sa',
  [string]$SqlPassword = '123456',
  [string]$Database = 'TopstarDb'
)

$ErrorActionPreference = 'Stop'
$headers = @{ Token = $Token; ShopId = $ShopId }
Add-Type -AssemblyName System.Net.Http

function Get-GhnJson {
    param(
        [string]$Method,
        [string]$Url
    )
    $handler = New-Object System.Net.Http.HttpClientHandler
    $client = New-Object System.Net.Http.HttpClient($handler)
    $request = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::new($Method), $Url)
    $request.Headers.Add('Token', $Token)
    $request.Headers.Add('ShopId', $ShopId)
    $response = $client.SendAsync($request).GetAwaiter().GetResult()
    $bytes = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    $client.Dispose()
    return ($text | ConvertFrom-Json)
}

Write-Host 'Calling GHN v3 provinces...'
$provinceRes = Get-GhnJson -Method 'GET' -Url 'https://online-gateway.ghn.vn/shiip/public-api/v3/master-data/province/all'
if ($provinceRes.code -ne 200) { throw "Province API failed: $($provinceRes.message)" }
$provinces = @($provinceRes.data)

$wards = New-Object System.Collections.Generic.List[object]
$idx = 0
$total = $provinces.Count
foreach ($p in $provinces) {
    $idx++
    if (($idx % 10) -eq 0 -or $idx -eq 1 -or $idx -eq $total) { Write-Host "Fetching wards by province $idx/$total..." }
    $uri = "https://online-gateway.ghn.vn/shiip/public-api/v3/master-data/ward/all-by-province-id?province_id=$($p._id)"
    $wardRes = Get-GhnJson -Method 'GET' -Url $uri
    if ($wardRes.code -ne 200) { continue }
    foreach ($w in @($wardRes.data)) { $wards.Add($w) }
}

Write-Host "Fetched provinces: $($provinces.Count), wards: $($wards.Count)"

Add-Type -AssemblyName 'System.Data'
$connStr = "Server=$SqlServer;Database=$Database;User Id=$SqlUser;Password=$SqlPassword;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$tx = $conn.BeginTransaction()

try {
    $cmd = $conn.CreateCommand()
    $cmd.Transaction = $tx
    $cmd.CommandText = @"
IF OBJECT_ID('tempdb..#TmpProvincesV3') IS NOT NULL DROP TABLE #TmpProvincesV3;
IF OBJECT_ID('tempdb..#TmpWardsV3') IS NOT NULL DROP TABLE #TmpWardsV3;

CREATE TABLE #TmpProvincesV3(
  GhnProvinceId INT NOT NULL,
  ProvinceCode INT NOT NULL,
  [Name] NVARCHAR(150) NOT NULL,
  ExtensionNamesJson NVARCHAR(MAX) NULL,
  [Type] NVARCHAR(30) NULL,
  ParentId INT NULL,
  GhnStatus INT NULL,
  UpdatedIp NVARCHAR(64) NULL,
  UpdatedEmployee INT NULL,
  UpdatedSource NVARCHAR(200) NULL,
  UpdatedDate DATETIME2(3) NULL,
  CreatedIp NVARCHAR(64) NULL,
  CreatedEmployee INT NULL,
  CreatedSource NVARCHAR(200) NULL,
  CreatedDate DATETIME2(3) NULL,
  IsActive BIT NOT NULL
);

CREATE TABLE #TmpWardsV3(
  GhnWardId INT NOT NULL,
  WardCode NVARCHAR(50) NOT NULL,
  GhnParentProvinceId INT NOT NULL,
  [Name] NVARCHAR(150) NOT NULL,
  ExtensionNamesJson NVARCHAR(MAX) NULL,
  [Type] NVARCHAR(30) NULL,
  GhnStatus INT NULL,
  UpdatedIp NVARCHAR(64) NULL,
  UpdatedEmployee INT NULL,
  UpdatedSource NVARCHAR(200) NULL,
  UpdatedDate DATETIME2(3) NULL,
  CreatedIp NVARCHAR(64) NULL,
  CreatedEmployee INT NULL,
  CreatedSource NVARCHAR(200) NULL,
  CreatedDate DATETIME2(3) NULL,
  IsActive BIT NOT NULL
);
"@
    [void]$cmd.ExecuteNonQuery()

    $dtP = New-Object System.Data.DataTable
    [void]$dtP.Columns.Add('GhnProvinceId', [int])
    [void]$dtP.Columns.Add('ProvinceCode', [int])
    [void]$dtP.Columns.Add('Name', [string])
    [void]$dtP.Columns.Add('ExtensionNamesJson', [string])
    [void]$dtP.Columns.Add('Type', [string])
    [void]$dtP.Columns.Add('ParentId', [int])
    [void]$dtP.Columns.Add('GhnStatus', [int])
    [void]$dtP.Columns.Add('UpdatedIp', [string])
    [void]$dtP.Columns.Add('UpdatedEmployee', [int])
    [void]$dtP.Columns.Add('UpdatedSource', [string])
    [void]$dtP.Columns.Add('UpdatedDate', [datetime])
    [void]$dtP.Columns.Add('CreatedIp', [string])
    [void]$dtP.Columns.Add('CreatedEmployee', [int])
    [void]$dtP.Columns.Add('CreatedSource', [string])
    [void]$dtP.Columns.Add('CreatedDate', [datetime])
    [void]$dtP.Columns.Add('IsActive', [bool])

    foreach ($p in $provinces) {
        $row = $dtP.NewRow()
        $row['GhnProvinceId'] = [int]$p._id
        $row['ProvinceCode'] = [int]$p._id
        $row['Name'] = [string]$p.name
        $row['ExtensionNamesJson'] = (($p.extension_names | ConvertTo-Json -Compress) -as [string])
        $row['Type'] = [string]$p.type
        if ($null -ne $p.parent_id) { $row['ParentId'] = [int]$p.parent_id }
        if ($null -ne $p.status) { $row['GhnStatus'] = [int]$p.status }
        $row['UpdatedIp'] = [string]$p.updated_ip
        if ($null -ne $p.updated_employee) { $row['UpdatedEmployee'] = [int]$p.updated_employee }
        $row['UpdatedSource'] = [string]$p.updated_source
        if ($null -ne $p.updated_date -and [datetime]::TryParse([string]$p.updated_date, [ref]([datetime]$tmp = [datetime]::MinValue))) { $row['UpdatedDate'] = [datetime]$p.updated_date }
        $row['CreatedIp'] = [string]$p.created_ip
        if ($null -ne $p.created_employee) { $row['CreatedEmployee'] = [int]$p.created_employee }
        $row['CreatedSource'] = [string]$p.created_source
        if ($null -ne $p.created_date -and [datetime]::TryParse([string]$p.created_date, [ref]([datetime]$tmp2 = [datetime]::MinValue))) { $row['CreatedDate'] = [datetime]$p.created_date }
        $row['IsActive'] = ([int]$p.status -eq 1)
        $dtP.Rows.Add($row)
    }

    $dtW = New-Object System.Data.DataTable
    [void]$dtW.Columns.Add('GhnWardId', [int])
    [void]$dtW.Columns.Add('WardCode', [string])
    [void]$dtW.Columns.Add('GhnParentProvinceId', [int])
    [void]$dtW.Columns.Add('Name', [string])
    [void]$dtW.Columns.Add('ExtensionNamesJson', [string])
    [void]$dtW.Columns.Add('Type', [string])
    [void]$dtW.Columns.Add('GhnStatus', [int])
    [void]$dtW.Columns.Add('UpdatedIp', [string])
    [void]$dtW.Columns.Add('UpdatedEmployee', [int])
    [void]$dtW.Columns.Add('UpdatedSource', [string])
    [void]$dtW.Columns.Add('UpdatedDate', [datetime])
    [void]$dtW.Columns.Add('CreatedIp', [string])
    [void]$dtW.Columns.Add('CreatedEmployee', [int])
    [void]$dtW.Columns.Add('CreatedSource', [string])
    [void]$dtW.Columns.Add('CreatedDate', [datetime])
    [void]$dtW.Columns.Add('IsActive', [bool])

    foreach ($w in $wards) {
        $row = $dtW.NewRow()
        $row['GhnWardId'] = [int]$w._id
        $row['WardCode'] = [string]$w._id
        $row['GhnParentProvinceId'] = [int]$w.parent_id
        $row['Name'] = [string]$w.name
        $row['ExtensionNamesJson'] = (($w.extension_names | ConvertTo-Json -Compress) -as [string])
        $row['Type'] = [string]$w.type
        if ($null -ne $w.status) { $row['GhnStatus'] = [int]$w.status }
        $row['UpdatedIp'] = [string]$w.updated_ip
        if ($null -ne $w.updated_employee) { $row['UpdatedEmployee'] = [int]$w.updated_employee }
        $row['UpdatedSource'] = [string]$w.updated_source
        if ($null -ne $w.updated_date -and [datetime]::TryParse([string]$w.updated_date, [ref]([datetime]$tmp3 = [datetime]::MinValue))) { $row['UpdatedDate'] = [datetime]$w.updated_date }
        $row['CreatedIp'] = [string]$w.created_ip
        if ($null -ne $w.created_employee) { $row['CreatedEmployee'] = [int]$w.created_employee }
        $row['CreatedSource'] = [string]$w.created_source
        if ($null -ne $w.created_date -and [datetime]::TryParse([string]$w.created_date, [ref]([datetime]$tmp4 = [datetime]::MinValue))) { $row['CreatedDate'] = [datetime]$w.created_date }
        $row['IsActive'] = ([int]$w.status -eq 1)
        $dtW.Rows.Add($row)
    }

    $bulkP = New-Object System.Data.SqlClient.SqlBulkCopy($conn, [System.Data.SqlClient.SqlBulkCopyOptions]::Default, $tx)
    $bulkP.DestinationTableName = '#TmpProvincesV3'
    foreach($c in $dtP.Columns){ [void]$bulkP.ColumnMappings.Add($c.ColumnName,$c.ColumnName) }
    $bulkP.WriteToServer($dtP)

    $bulkW = New-Object System.Data.SqlClient.SqlBulkCopy($conn, [System.Data.SqlClient.SqlBulkCopyOptions]::Default, $tx)
    $bulkW.DestinationTableName = '#TmpWardsV3'
    foreach($c in $dtW.Columns){ [void]$bulkW.ColumnMappings.Add($c.ColumnName,$c.ColumnName) }
    $bulkW.WriteToServer($dtW)

    $cmd2 = $conn.CreateCommand()
    $cmd2.Transaction = $tx
    $cmd2.CommandText = @"
UPDATE dbo.BrandProfiles SET WardId = NULL, ProvinceId = NULL, WardCodeV2 = NULL;
UPDATE dbo.KocProfiles SET WardId = NULL, ProvinceId = NULL, WardCodeV2 = NULL;
UPDATE dbo.Shipments SET WardId = NULL, ProvinceId = NULL, WardCodeV2 = NULL;

DELETE FROM dbo.Wards;
DELETE FROM dbo.Provinces;

INSERT INTO dbo.Provinces
(
  Id, ProvinceCode, Name, CodeName, IsActive, CreatedAtUtc, UpdatedAtUtc,
  GhnProvinceId, ExtensionNamesJson, Type, ParentId, GhnStatus,
  UpdatedIp, UpdatedEmployee, UpdatedSource, UpdatedDate,
  CreatedIp, CreatedEmployee, CreatedSource, CreatedDate
)
SELECT
  NEWID(), t.ProvinceCode, t.Name, NULL, t.IsActive, SYSUTCDATETIME(), NULL,
  t.GhnProvinceId, t.ExtensionNamesJson, t.Type, t.ParentId, t.GhnStatus,
  t.UpdatedIp, t.UpdatedEmployee, t.UpdatedSource, t.UpdatedDate,
  t.CreatedIp, t.CreatedEmployee, t.CreatedSource, t.CreatedDate
FROM #TmpProvincesV3 t;

INSERT INTO dbo.Wards
(
  Id, WardCode, ProvinceId, Name, CodeName, IsActive, CreatedAtUtc, UpdatedAtUtc,
  GhnWardId, ExtensionNamesJson, Type, GhnParentProvinceId, GhnStatus,
  UpdatedIp, UpdatedEmployee, UpdatedSource, UpdatedDate,
  CreatedIp, CreatedEmployee, CreatedSource, CreatedDate
)
SELECT
  NEWID(), t.WardCode, p.Id, t.Name, NULL, t.IsActive, SYSUTCDATETIME(), NULL,
  t.GhnWardId, t.ExtensionNamesJson, t.Type, t.GhnParentProvinceId, t.GhnStatus,
  t.UpdatedIp, t.UpdatedEmployee, t.UpdatedSource, t.UpdatedDate,
  t.CreatedIp, t.CreatedEmployee, t.CreatedSource, t.CreatedDate
FROM #TmpWardsV3 t
INNER JOIN dbo.Provinces p ON p.GhnProvinceId = t.GhnParentProvinceId;

SELECT COUNT(*) AS ProvincesCount FROM dbo.Provinces;
SELECT COUNT(*) AS WardsCount FROM dbo.Wards;
SELECT TOP 5 Name FROM dbo.Provinces ORDER BY Name;
SELECT TOP 5 Name FROM dbo.Wards ORDER BY Name;
"@

    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd2)
    $ds = New-Object System.Data.DataSet
    [void]$adapter.Fill($ds)

    $tx.Commit()

    $pc = $ds.Tables[0].Rows[0][0]
    $wc = $ds.Tables[1].Rows[0][0]
    Write-Host "Sync v3 done. Provinces in DB: $pc; Wards in DB: $wc"
}
catch {
    try { $tx.Rollback() } catch {}
    throw
}
finally {
    $conn.Close()
}
