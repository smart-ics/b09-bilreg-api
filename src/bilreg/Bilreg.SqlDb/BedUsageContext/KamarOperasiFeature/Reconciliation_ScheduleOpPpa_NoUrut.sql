-- =========================================================================================
-- Script Name : Reconciliation_ScheduleOpPpa_NoUrut.sql
-- Description : One-off DML reconciliation script to restore the Team Leader Invariant on
--               BILRG_ScheduleOpPpa for all active surgical schedules (VodDate = '3000-01-01').
--               Ensures the active TeamLead (BILRG_ScheduleOp.PpaId) is assigned NoUrut = 1,
--               and all other team members are numbered sequentially 2..N without gaps.
-- Reference   : TD-03, TD-13, DATABASE.md §10, DEFECT-KMO-JADWAL-OK-001, ISSUE-KMO-JADWAL-OK-004
-- Target DB   : HOSPITAL_PKL
-- Idempotent  : Yes (only updates active rows where p.NoUrut <> r.DesiredNoUrut)
-- =========================================================================================

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT 'Starting reconciliation for BILRG_ScheduleOpPpa NoUrut invariant...';

    ;WITH ReorderedPpa AS (
        SELECT 
            p.ScheduleOpId,
            p.NoUrut AS CurrentNoUrut,
            ROW_NUMBER() OVER (
                PARTITION BY p.ScheduleOpId 
                ORDER BY CASE WHEN p.PpaId = s.PpaId THEN 0 ELSE 1 END, p.NoUrut
            ) AS DesiredNoUrut
        FROM BILRG_ScheduleOpPpa p
        INNER JOIN BILRG_ScheduleOp s ON p.ScheduleOpId = s.ScheduleOpId
        WHERE s.VodDate = '3000-01-01'
    )
    UPDATE p
    SET p.NoUrut = r.DesiredNoUrut
    FROM BILRG_ScheduleOpPpa p
    INNER JOIN ReorderedPpa r 
        ON p.ScheduleOpId = r.ScheduleOpId 
       AND p.NoUrut = r.CurrentNoUrut
    WHERE p.NoUrut <> r.DesiredNoUrut;

    DECLARE @RowsUpdated INT = @@ROWCOUNT;
    PRINT 'Reconciliation complete. Rows updated: ' + CAST(@RowsUpdated AS VARCHAR(10));

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
