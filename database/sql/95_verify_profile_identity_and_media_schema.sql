/*
    95_verify_profile_identity_and_media_schema.sql
    Kiểm tra bằng metadata, không tham chiếu trực tiếp tới cột có thể còn thiếu.
*/
SET NOCOUNT ON;

DECLARE @Expected TABLE
(
    SchemaName SYSNAME NOT NULL,
    TableName SYSNAME NOT NULL,
    ColumnName SYSNAME NULL
);

INSERT INTO @Expected(SchemaName, TableName, ColumnName)
VALUES
    (N'dbo', N'Users', N'DateOfBirth'),
    (N'dbo', N'KocProfiles', N'AvatarUrl'),
    (N'dbo', N'BrandProfiles', N'TaxCompanyName'),
    (N'dbo', N'BrandProfiles', N'TaxCode'),
    (N'dbo', N'BrandProfiles', N'TaxInvoiceAddress'),
    (N'dbo', N'BrandProfiles', N'BusinessLicenseFileUrl'),
    (N'dbo', N'KocMediaAssets', NULL);

SELECT
    CONCAT(e.SchemaName, N'.', e.TableName) AS ObjectName,
    COALESCE(e.ColumnName, N'(table)') AS MemberName,
    CASE
        WHEN e.ColumnName IS NULL
            THEN IIF(OBJECT_ID(QUOTENAME(e.SchemaName) + N'.' + QUOTENAME(e.TableName), N'U') IS NOT NULL, N'OK', N'MISSING')
        WHEN COL_LENGTH(e.SchemaName + N'.' + e.TableName, e.ColumnName) IS NOT NULL
            THEN N'OK'
        ELSE N'MISSING'
    END AS VerificationStatus
FROM @Expected e
ORDER BY e.TableName, e.ColumnName;

IF EXISTS
(
    SELECT 1
    FROM @Expected e
    WHERE
        (e.ColumnName IS NULL
         AND OBJECT_ID(QUOTENAME(e.SchemaName) + N'.' + QUOTENAME(e.TableName), N'U') IS NULL)
        OR
        (e.ColumnName IS NOT NULL
         AND COL_LENGTH(e.SchemaName + N'.' + e.TableName, e.ColumnName) IS NULL)
)
BEGIN
    THROW 51003, N'Schema hồ sơ vẫn còn thành phần MISSING. Hãy chạy script 94 trước.', 1;
END;

SELECT N'PASS: schema hồ sơ đã đầy đủ.' AS Result;
