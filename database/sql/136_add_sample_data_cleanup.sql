SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AdminCleanupSampleAccounts
    @Execute BIT = 0,
    @ActorUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    CREATE TABLE #Rows(ObjectId INT NOT NULL, KeyValue NVARCHAR(128) NOT NULL,
        CONSTRAINT PK_SampleCleanupRows PRIMARY KEY(ObjectId, KeyValue));

    INSERT #Rows(ObjectId,KeyValue)
    SELECT OBJECT_ID(N'dbo.Users'),CONVERT(NVARCHAR(128),u.Id)
    FROM dbo.Users u
    WHERE u.Id<>@ActorUserId AND
    (
        LOWER(u.Email) LIKE N'seed.koc.%@seed.topstar.local' OR
        LOWER(u.Email) LIKE N'seed.brand.%@seed.topstar.local' OR
        LOWER(u.Email) IN(N'koc@topstar.local',N'brand@topstar.local')
    );

    DECLARE @Changed INT=1,@Sql NVARCHAR(MAX),@Added INT;
    DECLARE @ChildObjectId INT,@ParentObjectId INT,@ChildSchema SYSNAME,@ChildTable SYSNAME,
            @ParentSchema SYSNAME,@ParentTable SYSNAME,@ChildFk SYSNAME,@ParentKey SYSNAME,@ChildPk SYSNAME;

    WHILE @Changed>0
    BEGIN
        SET @Changed=0;
        DECLARE fk_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT fk.parent_object_id,fk.referenced_object_id,
               SCHEMA_NAME(ct.schema_id),ct.name,SCHEMA_NAME(pt.schema_id),pt.name,
               cc.name,pc.name,cpk.name
        FROM sys.foreign_keys fk
        JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id=fk.object_id
        JOIN sys.tables ct ON ct.object_id=fk.parent_object_id
        JOIN sys.tables pt ON pt.object_id=fk.referenced_object_id
        JOIN sys.columns cc ON cc.object_id=fk.parent_object_id AND cc.column_id=fkc.parent_column_id
        JOIN sys.columns pc ON pc.object_id=fk.referenced_object_id AND pc.column_id=fkc.referenced_column_id
        JOIN sys.indexes pki ON pki.object_id=ct.object_id AND pki.is_primary_key=1
        JOIN sys.index_columns pkic ON pkic.object_id=pki.object_id AND pkic.index_id=pki.index_id
        JOIN sys.columns cpk ON cpk.object_id=pkic.object_id AND cpk.column_id=pkic.column_id
        WHERE (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id)=1
          AND (SELECT COUNT(*) FROM sys.index_columns x WHERE x.object_id=pki.object_id AND x.index_id=pki.index_id)=1
          AND ct.name NOT IN(N'AcademyCourses',N'AcademyLessons',N'NotificationTemplates',
              N'OperationalRuntimePolicies',N'OperationalSlaPolicies',N'PaymentMethodConfigurations',N'PayoutPolicies');

        OPEN fk_cursor;
        FETCH NEXT FROM fk_cursor INTO @ChildObjectId,@ParentObjectId,@ChildSchema,@ChildTable,@ParentSchema,@ParentTable,@ChildFk,@ParentKey,@ChildPk;
        WHILE @@FETCH_STATUS=0
        BEGIN
            SET @Sql=N'INSERT #Rows(ObjectId,KeyValue) '
                +N'SELECT DISTINCT @ChildId,CONVERT(NVARCHAR(128),c.'+QUOTENAME(@ChildPk)+N') '
                +N'FROM '+QUOTENAME(@ChildSchema)+N'.'+QUOTENAME(@ChildTable)+N' c '
                +N'JOIN #Rows r ON r.ObjectId=@ParentId AND r.KeyValue=CONVERT(NVARCHAR(128),c.'+QUOTENAME(@ChildFk)+N') '
                +N'WHERE c.'+QUOTENAME(@ChildFk)+N' IS NOT NULL AND NOT EXISTS(SELECT 1 FROM #Rows x WHERE x.ObjectId=@ChildId AND x.KeyValue=CONVERT(NVARCHAR(128),c.'+QUOTENAME(@ChildPk)+N')); '
                +N'SET @Added=@@ROWCOUNT;';
            SET @Added=0;
            EXEC sp_executesql @Sql,N'@ChildId INT,@ParentId INT,@Added INT OUTPUT',@ChildObjectId,@ParentObjectId,@Added OUTPUT;
            SET @Changed+=@Added;
            FETCH NEXT FROM fk_cursor INTO @ChildObjectId,@ParentObjectId,@ChildSchema,@ChildTable,@ParentSchema,@ParentTable,@ChildFk,@ParentKey,@ChildPk;
        END
        CLOSE fk_cursor;
        DEALLOCATE fk_cursor;
    END

    DECLARE @MemberAccounts INT=(SELECT COUNT(*) FROM dbo.Users u JOIN #Rows r ON r.ObjectId=OBJECT_ID(N'dbo.Users') AND r.KeyValue=CONVERT(NVARCHAR(128),u.Id) WHERE LOWER(u.Email) LIKE N'seed.koc.%@seed.topstar.local' OR LOWER(u.Email)=N'koc@topstar.local');
    DECLARE @BrandAccounts INT=(SELECT COUNT(*) FROM dbo.Users u JOIN #Rows r ON r.ObjectId=OBJECT_ID(N'dbo.Users') AND r.KeyValue=CONVERT(NVARCHAR(128),u.Id) WHERE LOWER(u.Email) LIKE N'seed.brand.%@seed.topstar.local' OR LOWER(u.Email)=N'brand@topstar.local');
    DECLARE @KocProfiles INT=(SELECT COUNT(*) FROM #Rows WHERE ObjectId=OBJECT_ID(N'dbo.KocProfiles'));
    DECLARE @BrandProfiles INT=(SELECT COUNT(*) FROM #Rows WHERE ObjectId=OBJECT_ID(N'dbo.BrandProfiles'));
    DECLARE @Campaigns INT=(SELECT COUNT(*) FROM #Rows WHERE ObjectId=OBJECT_ID(N'dbo.Campaigns'));
    DECLARE @CompositeRecords INT=
        (SELECT COUNT(*) FROM dbo.AcademyEnrollmentLessons x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.AcademyEnrollments') AND r.KeyValue=CONVERT(NVARCHAR(128),x.EnrollmentId)))+
        (SELECT COUNT(*) FROM dbo.CampaignTargetCategories x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.Campaigns') AND r.KeyValue=CONVERT(NVARCHAR(128),x.CampaignId)))+
        (SELECT COUNT(*) FROM dbo.CampaignTargetProvinces x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.Campaigns') AND r.KeyValue=CONVERT(NVARCHAR(128),x.CampaignId)))+
        (SELECT COUNT(*) FROM dbo.KocProfileCategories x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.KocProfiles') AND r.KeyValue=CONVERT(NVARCHAR(128),x.KocProfileId)))+
        (SELECT COUNT(*) FROM dbo.KocSocialAccountCategories x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.KocSocialAccounts') AND r.KeyValue=CONVERT(NVARCHAR(128),x.SocialAccountId)))+
        (SELECT COUNT(*) FROM dbo.ShortlistItems x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE (r.ObjectId=OBJECT_ID(N'dbo.Shortlists') AND r.KeyValue=CONVERT(NVARCHAR(128),x.ShortlistId)) OR (r.ObjectId=OBJECT_ID(N'dbo.Applications') AND r.KeyValue=CONVERT(NVARCHAR(128),x.ApplicationId))))+
        (SELECT COUNT(*) FROM dbo.UserDashboardConfigs x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.Users') AND r.KeyValue=CONVERT(NVARCHAR(128),x.UserId)))+
        (SELECT COUNT(*) FROM dbo.UserMenus x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.Users') AND r.KeyValue=CONVERT(NVARCHAR(128),x.UserId)))+
        (SELECT COUNT(*) FROM dbo.UserPermissions x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.Users') AND r.KeyValue=CONVERT(NVARCHAR(128),x.UserId)))+
        (SELECT COUNT(*) FROM dbo.UserRoles x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.Users') AND r.KeyValue=CONVERT(NVARCHAR(128),x.UserId)));
    DECLARE @RelatedRecords INT=(SELECT COUNT(*) FROM #Rows)-@MemberAccounts-@BrandAccounts+@CompositeRecords;

    IF @Execute=1 AND (@MemberAccounts+@BrandAccounts)>0
    BEGIN
        BEGIN TRY
            BEGIN TRANSACTION;
            DECLARE @LockResult INT;
            EXEC @LockResult=sys.sp_getapplock @Resource=N'Topstar.SampleDataCleanup',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000;
            IF @LockResult<0 THROW 51136,N'Không thể khóa tác vụ dọn dữ liệu mẫu. Vui lòng thử lại.',1;

            DELETE x FROM dbo.AcademyEnrollmentLessons x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.AcademyEnrollments') AND r.KeyValue=CONVERT(NVARCHAR(128),x.EnrollmentId));
            DELETE x FROM dbo.CampaignTargetCategories x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.Campaigns') AND r.KeyValue=CONVERT(NVARCHAR(128),x.CampaignId));
            DELETE x FROM dbo.CampaignTargetProvinces x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.Campaigns') AND r.KeyValue=CONVERT(NVARCHAR(128),x.CampaignId));
            DELETE x FROM dbo.KocProfileCategories x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.KocProfiles') AND r.KeyValue=CONVERT(NVARCHAR(128),x.KocProfileId));
            DELETE x FROM dbo.KocSocialAccountCategories x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.KocSocialAccounts') AND r.KeyValue=CONVERT(NVARCHAR(128),x.SocialAccountId));
            DELETE x FROM dbo.ShortlistItems x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE (r.ObjectId=OBJECT_ID(N'dbo.Shortlists') AND r.KeyValue=CONVERT(NVARCHAR(128),x.ShortlistId)) OR (r.ObjectId=OBJECT_ID(N'dbo.Applications') AND r.KeyValue=CONVERT(NVARCHAR(128),x.ApplicationId)));
            DELETE x FROM dbo.UserDashboardConfigs x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.Users') AND r.KeyValue=CONVERT(NVARCHAR(128),x.UserId));
            DELETE x FROM dbo.UserMenus x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.Users') AND r.KeyValue=CONVERT(NVARCHAR(128),x.UserId));
            DELETE x FROM dbo.UserPermissions x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.Users') AND r.KeyValue=CONVERT(NVARCHAR(128),x.UserId));
            DELETE x FROM dbo.UserRoles x WHERE EXISTS(SELECT 1 FROM #Rows r WHERE r.ObjectId=OBJECT_ID(N'dbo.Users') AND r.KeyValue=CONVERT(NVARCHAR(128),x.UserId));

            DECLARE table_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT DISTINCT r.ObjectId,SCHEMA_NAME(t.schema_id),t.name,pkc.name
            FROM #Rows r JOIN sys.tables t ON t.object_id=r.ObjectId
            JOIN sys.indexes pki ON pki.object_id=t.object_id AND pki.is_primary_key=1
            JOIN sys.index_columns pkic ON pkic.object_id=pki.object_id AND pkic.index_id=pki.index_id
            JOIN sys.columns pkc ON pkc.object_id=pkic.object_id AND pkc.column_id=pkic.column_id
            WHERE (SELECT COUNT(*) FROM sys.index_columns x WHERE x.object_id=pki.object_id AND x.index_id=pki.index_id)=1;

            OPEN table_cursor;
            FETCH NEXT FROM table_cursor INTO @ChildObjectId,@ChildSchema,@ChildTable,@ChildPk;
            WHILE @@FETCH_STATUS=0
            BEGIN
                SET @Sql=N'ALTER TABLE '+QUOTENAME(@ChildSchema)+N'.'+QUOTENAME(@ChildTable)+N' NOCHECK CONSTRAINT ALL;';
                EXEC sp_executesql @Sql;
                FETCH NEXT FROM table_cursor INTO @ChildObjectId,@ChildSchema,@ChildTable,@ChildPk;
            END
            CLOSE table_cursor;

            OPEN table_cursor;
            FETCH NEXT FROM table_cursor INTO @ChildObjectId,@ChildSchema,@ChildTable,@ChildPk;
            WHILE @@FETCH_STATUS=0
            BEGIN
                SET @Sql=N'DELETE t FROM '+QUOTENAME(@ChildSchema)+N'.'+QUOTENAME(@ChildTable)+N' t JOIN #Rows r ON r.ObjectId=@ObjectId AND r.KeyValue=CONVERT(NVARCHAR(128),t.'+QUOTENAME(@ChildPk)+N');';
                EXEC sp_executesql @Sql,N'@ObjectId INT',@ChildObjectId;
                FETCH NEXT FROM table_cursor INTO @ChildObjectId,@ChildSchema,@ChildTable,@ChildPk;
            END
            CLOSE table_cursor;

            OPEN table_cursor;
            FETCH NEXT FROM table_cursor INTO @ChildObjectId,@ChildSchema,@ChildTable,@ChildPk;
            WHILE @@FETCH_STATUS=0
            BEGIN
                SET @Sql=N'ALTER TABLE '+QUOTENAME(@ChildSchema)+N'.'+QUOTENAME(@ChildTable)+N' WITH CHECK CHECK CONSTRAINT ALL;';
                EXEC sp_executesql @Sql;
                FETCH NEXT FROM table_cursor INTO @ChildObjectId,@ChildSchema,@ChildTable,@ChildPk;
            END
            CLOSE table_cursor;
            DEALLOCATE table_cursor;

            INSERT dbo.AuditLogs(ActorUserId,EntityName,EntityId,ActionName,PayloadJson,CreatedAtUtc)
            VALUES(@ActorUserId,N'SampleData',N'seed-accounts',N'Delete',
                CONCAT(N'{"memberAccounts":',@MemberAccounts,N',"brandAccounts":',@BrandAccounts,N',"relatedRecords":',@RelatedRecords,N'}'),SYSUTCDATETIME());
            COMMIT;
        END TRY
        BEGIN CATCH
            IF CURSOR_STATUS('local','table_cursor')>=0 CLOSE table_cursor;
            IF CURSOR_STATUS('local','table_cursor')>-3 DEALLOCATE table_cursor;
            IF @@TRANCOUNT>0 ROLLBACK;
            THROW;
        END CATCH
    END

    SELECT @MemberAccounts MemberAccounts,@BrandAccounts BrandAccounts,@KocProfiles KocProfiles,
           @BrandProfiles BrandProfiles,@Campaigns Campaigns,@RelatedRecords RelatedRecords;
END
GO

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @ParentId UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_ADMIN_SETTINGS');
    DECLARE @MenuId UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_ADMIN_SETTINGS_SAMPLE_DATA');
    IF @MenuId IS NULL
    BEGIN
        SET @MenuId=NEWID();
        INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc)
        VALUES(@MenuId,'MENU_ADMIN_SETTINGS_SAMPLE_DATA',N'Xóa dữ liệu mẫu','/Admin/Settings/SampleData','fa-solid fa-broom',@ParentId,37,1,SYSUTCDATETIME());
    END
    ELSE UPDATE dbo.Menus SET Title=N'Xóa dữ liệu mẫu',Path='/Admin/Settings/SampleData',Icon='fa-solid fa-broom',ParentId=@ParentId,OrderNo=37,IsVisible=1 WHERE Id=@MenuId;
    INSERT dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc)
    SELECT r.Id,@MenuId,SYSUTCDATETIME() FROM dbo.Roles r WHERE UPPER(r.Code)='ADMIN'
      AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus rm WHERE rm.RoleId=r.Id AND rm.MenuId=@MenuId);
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
GO
