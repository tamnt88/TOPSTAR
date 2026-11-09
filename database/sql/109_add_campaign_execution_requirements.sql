SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.Campaigns', 'ExecutionMode') IS NULL
        ALTER TABLE dbo.Campaigns ADD ExecutionMode nvarchar(40) NOT NULL
            CONSTRAINT DF_Campaigns_ExecutionMode DEFAULT N'RemoteProductReview';
    IF COL_LENGTH('dbo.Campaigns', 'VenueName') IS NULL
        ALTER TABLE dbo.Campaigns ADD VenueName nvarchar(200) NULL;
    IF COL_LENGTH('dbo.Campaigns', 'VenueAddress') IS NULL
        ALTER TABLE dbo.Campaigns ADD VenueAddress nvarchar(500) NULL;
    IF COL_LENGTH('dbo.Campaigns', 'ContactName') IS NULL
        ALTER TABLE dbo.Campaigns ADD ContactName nvarchar(100) NULL;
    IF COL_LENGTH('dbo.Campaigns', 'ContactPhone') IS NULL
        ALTER TABLE dbo.Campaigns ADD ContactPhone nvarchar(20) NULL;
    IF COL_LENGTH('dbo.Campaigns', 'ExecutionSchedule') IS NULL
        ALTER TABLE dbo.Campaigns ADD ExecutionSchedule nvarchar(500) NULL;
    IF COL_LENGTH('dbo.Campaigns', 'BookingInstructions') IS NULL
        ALTER TABLE dbo.Campaigns ADD BookingInstructions nvarchar(1000) NULL;
    IF COL_LENGTH('dbo.Campaigns', 'ShippingInstructions') IS NULL
        ALTER TABLE dbo.Campaigns ADD ShippingInstructions nvarchar(1000) NULL;
    IF COL_LENGTH('dbo.Campaigns', 'SupportDetails') IS NULL
        ALTER TABLE dbo.Campaigns ADD SupportDetails nvarchar(1000) NULL;
    IF COL_LENGTH('dbo.Campaigns', 'ExecutionRequirements') IS NULL
        ALTER TABLE dbo.Campaigns ADD ExecutionRequirements nvarchar(2000) NULL;

    EXEC(N'UPDATE dbo.Campaigns
        SET ExecutionMode = N''StoreReviewOnsite''
        WHERE RequiresBooking = 1
          AND ExecutionMode = N''RemoteProductReview'';');

    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints
        WHERE name = 'CK_Campaigns_ExecutionMode'
          AND parent_object_id = OBJECT_ID('dbo.Campaigns')
    )
        EXEC(N'ALTER TABLE dbo.Campaigns WITH CHECK ADD CONSTRAINT CK_Campaigns_ExecutionMode
            CHECK (ExecutionMode IN (
                N''RemoteProductReview'',
                N''FoodReviewOnsite'',
                N''StoreReviewOnsite'',
                N''ServiceReviewOnsite'',
                N''EventOnsite'',
                N''LivestreamOnline''
            ));');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
