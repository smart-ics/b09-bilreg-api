CREATE OR ALTER PROCEDURE sp_tz_parameter_no2_getnextvalue
    @fs_prefix VARCHAR(100),
    @fs_modul VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @next_value DECIMAL(18,0);

    BEGIN TRANSACTION;

    BEGIN TRY
        -- Try to update existing record with UPDLOCK and ROWLOCK to prevent lock escalation
        UPDATE tz_parameter_no2 WITH (UPDLOCK, ROWLOCK, READPAST)
        SET @next_value = fn_value,
            fn_value = fn_value + 1
        WHERE fs_prefix = @fs_prefix
          AND fs_modul = @fs_modul;

        -- If no rows were updated, insert new record
        IF @next_value IS NULL
            BEGIN
                SET @next_value = 1;

                INSERT INTO tz_parameter_no2 (fs_prefix, fs_modul, fn_value)
                VALUES (@fs_prefix, @fs_modul, 2);
            END

        COMMIT TRANSACTION;

        SELECT @next_value AS next_value;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
----
    GRANT EXECUTE ON sp_tz_parameter_no2_getnextvalue TO bilregUser;
GO