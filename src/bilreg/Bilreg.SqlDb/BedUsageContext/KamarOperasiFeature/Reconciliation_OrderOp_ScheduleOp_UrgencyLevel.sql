-- =========================================================================================
-- Script Name : Reconciliation_OrderOp_ScheduleOp_UrgencyLevel.sql
-- Description : Idempotent DML reconciliation script to normalize UrgencyLevel values on
--               BILRG_OrderOp and BILRG_ScheduleOp to canonical Elective (1).
--               Converts legacy/invalid values (0 or NULL) to 1 (Elective).
-- Reference   : TD-01, TD-02, ARCHITECTURE v1.1, ISSUE-SURGERY-ORDER-POOL-002
-- Target DB   : HOSPITAL_PKL
-- Idempotent  : Yes (only updates rows where UrgencyLevel = 0 OR UrgencyLevel IS NULL)
-- =========================================================================================

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT 'Starting reconciliation for BILRG_OrderOp and BILRG_ScheduleOp UrgencyLevel...';

    -- Rekonsiliasi Order Operasi
    UPDATE BILRG_OrderOp
    SET UrgencyLevel = 1
    WHERE UrgencyLevel = 0 OR UrgencyLevel IS NULL;

    DECLARE @RowsUpdatedOrder INT = @@ROWCOUNT;
    PRINT 'BILRG_OrderOp reconciliation complete. Rows updated: ' + CAST(@RowsUpdatedOrder AS VARCHAR(10));

    -- Rekonsiliasi Jadwal Operasi
    UPDATE BILRG_ScheduleOp
    SET UrgencyLevel = 1
    WHERE UrgencyLevel = 0 OR UrgencyLevel IS NULL;

    DECLARE @RowsUpdatedSchedule INT = @@ROWCOUNT;
    PRINT 'BILRG_ScheduleOp reconciliation complete. Rows updated: ' + CAST(@RowsUpdatedSchedule AS VARCHAR(10));

    COMMIT TRANSACTION;
    PRINT 'Transaction committed successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    PRINT 'Reconciliation failed! Transaction rolled back.';
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
GO
