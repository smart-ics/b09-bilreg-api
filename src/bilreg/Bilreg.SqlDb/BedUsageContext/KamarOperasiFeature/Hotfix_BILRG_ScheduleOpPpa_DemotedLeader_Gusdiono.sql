-- =========================================================================================
-- Script Name : Hotfix_BILRG_ScheduleOpPpa_DemotedLeader_Gusdiono.sql
-- Description : Skrip SQL DML idempoten pemulihan keanggotaan PPA untuk jadwal pasien
--               GUSDIONO, TN (ScheduleOpId = '01M6JMTNRRETHZN806MR1KJHF4') pada basis data HOSPITAL_PKL.
--               Memastikan dokter operator baru dr RAHMAN SUWARDI, Sp. B FICS (DR00133)
--               menempati NoUrut = 1, dan dokter operator lama MARTAHADINAN, dr. M. Si.Med.,
--               Sp. B,K(BD) (DR00083) dipulihkan pada NoUrut = 2.
-- Reference   : TD-52, DEF-KMO-JADWAL-006, ISS-KMO-JADWAL-008, OC-KMO-JADWAL-OK-ARCHITECTURE.md
-- Target DB   : HOSPITAL_PKL (dev.smart-ics.com)
-- Idempotent  : Yes (IF NOT EXISTS ... INSERT ELSE UPDATE)
-- =========================================================================================

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT 'Memulai rekonsiliasi keanggotaan PPA untuk ScheduleOpId: 01M6JMTNRRETHZN806MR1KJHF4...';

    -- 1. Pastikan dokter operator baru DR00133 menempati NoUrut 1
    UPDATE BILRG_ScheduleOpPpa
    SET NoUrut = 1
    WHERE ScheduleOpId = '01M6JMTNRRETHZN806MR1KJHF4'
      AND PpaId = 'DR00133';

    -- 2. Pulihkan dokter operator lama DR00083 pada NoUrut = 2 jika belum ada (idempoten)
    IF NOT EXISTS (
        SELECT 1 FROM BILRG_ScheduleOpPpa 
        WHERE ScheduleOpId = '01M6JMTNRRETHZN806MR1KJHF4' 
          AND PpaId = 'DR00083'
    )
    BEGIN
        INSERT INTO BILRG_ScheduleOpPpa (ScheduleOpId, NoUrut, PpaId, ProfesiId, GroupSpesialisId)
        VALUES ('01M6JMTNRRETHZN806MR1KJHF4', 2, 'DR00083', 'DOK', 'BDH');
        PRINT 'Dokter demosi DR00083 berhasil disisipkan pada NoUrut = 2.';
    END
    ELSE
    BEGIN
        UPDATE BILRG_ScheduleOpPpa
        SET NoUrut = 2
        WHERE ScheduleOpId = '01M6JMTNRRETHZN806MR1KJHF4'
          AND PpaId = 'DR00083';
        PRINT 'Dokter demosi DR00083 telah ada dan diperbarui pada NoUrut = 2.';
    END

    COMMIT TRANSACTION;
    PRINT 'Transaksi pemulihan keanggotaan PPA berhasil dikomit.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    PRINT 'Terjadi galat! Transaksi di-rollback.';
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
GO

-- =========================================================================================
-- Kueri Verifikasi
-- =========================================================================================
SELECT 
    p.ScheduleOpId,
    p.NoUrut,
    p.PpaId,
    ISNULL(peg.fs_nm_peg, '') AS PpaName,
    p.ProfesiId,
    p.GroupSpesialisId
FROM BILRG_ScheduleOpPpa p
LEFT JOIN td_peg peg ON p.PpaId = peg.fs_kd_peg
WHERE p.ScheduleOpId = '01M6JMTNRRETHZN806MR1KJHF4'
ORDER BY p.NoUrut;
