-- =========================================================================================
-- Script Name : 20261102_01_AddIsDraftToScheduleOp.sql
-- Description : DDL migration and idempotent backfill script for adding IsDraft column,
--               composite index, and backfilling existing active surgery schedules to official.
-- Reference   : TD-31, OC-KMO-JADWAL-OK-ARCHITECTURE.md §8 Database Design, P1-S01
-- Target DB   : HOSPITAL_PKL
-- Idempotent  : Yes
-- =========================================================================================

SET NOCOUNT ON;

-- 1. Penambahan Kolom IsDraft pada BILRG_ScheduleOp (v2.8)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_ScheduleOp') AND name = 'IsDraft')
BEGIN
    PRINT 'Adding column IsDraft to BILRG_ScheduleOp...';
    ALTER TABLE BILRG_ScheduleOp 
        ADD IsDraft BIT NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_IsDraft DEFAULT(1);
    PRINT 'Column IsDraft added successfully.';
END
ELSE
BEGIN
    PRINT 'Column IsDraft already exists on BILRG_ScheduleOp.';
END
GO

-- 2. Index Penunjang Kueri Kalender & Deteksi Bentrok Menyertakan IsDraft
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('BILRG_ScheduleOp') AND name = 'IX_BILRG_ScheduleOp_IsDraft_TglOp')
BEGIN
    PRINT 'Creating index IX_BILRG_ScheduleOp_IsDraft_TglOp on BILRG_ScheduleOp...';
    CREATE INDEX IX_BILRG_ScheduleOp_IsDraft_TglOp
        ON BILRG_ScheduleOp (TglOp, VodDate, IsDraft)
        INCLUDE (KamarId, Durasi, PpaId, OrderOpId)
        WITH (FILLFACTOR = 95);
    PRINT 'Index IX_BILRG_ScheduleOp_IsDraft_TglOp created successfully.';
END
ELSE
BEGIN
    PRINT 'Index IX_BILRG_ScheduleOp_IsDraft_TglOp already exists.';
END
GO

-- 3. Skrip Backfill Idempoten (v2.8)
-- Menjamin seluruh jadwal aktif sebelum v2.8 dimutakhirkan menjadi jadwal resmi (IsDraft = 0)
BEGIN TRY
    BEGIN TRANSACTION;

    PRINT 'Starting idempotent backfill of existing active schedules to official (IsDraft = 0)...';

    UPDATE BILRG_ScheduleOp
    SET IsDraft = 0
    WHERE VodDate = '3000-01-01'
      AND IsDraft = 1;

    DECLARE @RowsBackfilled INT = @@ROWCOUNT;
    PRINT 'Backfill completed. Rows updated: ' + CAST(@RowsBackfilled AS VARCHAR(10));

    COMMIT TRANSACTION;
    PRINT 'Backfill transaction committed successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    PRINT 'Backfill failed! Transaction rolled back.';
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
GO
