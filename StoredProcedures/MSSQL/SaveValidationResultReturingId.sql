CREATE PROCEDURE SaveValidationResultReturingId
    @RawRequest VARCHAR(MAX),
    @DT DATETIME2,
    @ValidationResultCode INT,
    @InsertedID BIGINT OUTPUT -- Add an OUTPUT parameter to capture the inserted ID
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Insert into the ValidationResults table
        INSERT INTO dbo.ValidationResults (Raw_request, DT, Validation_result)
        VALUES (@RawRequest, @DT, @ValidationResultCode);

        -- Capture the inserted ID using SCOPE_IDENTITY()
        SET @InsertedID = SCOPE_IDENTITY();

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000), @ErrorSeverity INT, @ErrorState INT;
        SELECT 
            @ErrorMessage = ERROR_MESSAGE(), 
            @ErrorSeverity = ERROR_SEVERITY(), 
            @ErrorState = ERROR_STATE();
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END