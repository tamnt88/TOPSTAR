-- 20260406_enforce_unique_login_identity.sql
-- Enforce unique login identity: Email + Phone

-- 1) Data quality checks before adding constraints
IF EXISTS (
    SELECT 1
    FROM dbo.Users
    WHERE Phone IS NOT NULL AND LTRIM(RTRIM(Phone)) <> ''
    GROUP BY Phone
    HAVING COUNT(*) > 1
)
BEGIN
    RAISERROR (N'Không thể tạo ràng buộc unique cho Phone vì đang có dữ liệu trùng trong bảng Users.', 16, 1);
    RETURN;
END

IF EXISTS (
    SELECT 1
    FROM dbo.Users
    GROUP BY Email
    HAVING COUNT(*) > 1
)
BEGIN
    RAISERROR (N'Không thể tạo ràng buộc unique cho Email vì đang có dữ liệu trùng trong bảng Users.', 16, 1);
    RETURN;
END

-- 2) Unique index for Email (if no unique index currently covers Email)
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.Users')
      AND i.is_unique = 1
      AND c.name = N'Email'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Users_Email] ON [dbo].[Users]([Email]);
END

-- 3) Unique filtered index for Phone (allow NULL/empty)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Users')
      AND name = N'UX_Users_Phone_NotNull'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Users_Phone_NotNull]
    ON [dbo].[Users]([Phone])
    WHERE [Phone] IS NOT NULL AND [Phone] <> '';
END
