CREATE OR ALTER PROCEDURE sp_tz_parameter_no_getnextvalue
    @fs_kd_parameter VARCHAR(100),
    @fs_nm_parameter VARCHAR(100) = NULL
    AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @next_value DECIMAL(18,0);

BEGIN TRANSACTION;

BEGIN TRY
-- Try to update existing record with UPDLOCK and ROWLOCK to prevent lock escalation
UPDATE tz_parameter_no WITH (UPDLOCK, ROWLOCK, READPAST)
SET @next_value = fn_value,
    fn_value = fn_value + 1
WHERE fs_kd_parameter = @fs_kd_parameter;

-- If no rows were updated, insert new record
IF @next_value IS NULL
BEGIN
            SET @next_value = 1;

INSERT INTO tz_parameter_no (fs_kd_parameter, fs_nm_parameter, fn_value)
VALUES (@fs_kd_parameter, ISNULL(@fs_nm_parameter, @fs_kd_parameter), 2);
END

COMMIT TRANSACTION;

SELECT @next_value AS next_value;
END TRY
BEGIN CATCH
ROLLBACK TRANSACTION;
        THROW;
END CATCH
END;