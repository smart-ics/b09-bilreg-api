-- ============================================================
-- TD-42b: Selective Multi-Table DML Healing for Tn. Gusdiono
-- ISS-KMO-JADWAL-004 / DEF-KMO-JADWAL-003
-- Target Database: dev.smart-ics.com / HOSPITAL_PKL
-- Isolated OrderOpId: '01M6JEEDRMR3316WM4751MV0R8'
--
-- Rekonsiliasi atomik selektif 3 tabel untuk kasus Tn. Gusdiono:
-- 1. Mengembalikan BILRG_OpCase.OrderOpState = 0 (Requested)
-- 2. Menyelaraskan BILRG_OpCaseAktif.OpCaseState = 0 (Requested)
-- 3. Menghapus entri palsu BILRG_OpCaseStateHist (NoUrut = 1, OpCaseState = 1)
-- ============================================================

BEGIN TRANSACTION;

-- 1. Pulihkan status pada entitas utama OpCase
IF COL_LENGTH('BILRG_OpCase', 'MdfDate') IS NOT NULL
BEGIN
    EXEC('UPDATE BILRG_OpCase SET OrderOpState = 0, MdfDate = GETDATE() WHERE OrderOpId = ''01M6JEEDRMR3316WM4751MV0R8'' AND OrderOpState = 1;');
END
ELSE
BEGIN
    UPDATE BILRG_OpCase 
    SET OrderOpState = 0 
    WHERE OrderOpId = '01M6JEEDRMR3316WM4751MV0R8' 
      AND OrderOpState = 1;
END

-- 2. Selaraskan status pada proyeksi tabel aktif OpCaseAktif
UPDATE BILRG_OpCaseAktif 
SET OpCaseState = 0 
WHERE OrderOpId = '01M6JEEDRMR3316WM4751MV0R8' 
  AND OpCaseState = 1;

-- 3. Hapus entri riwayat mutasi palsu yang terbit saat stale process test
DELETE FROM BILRG_OpCaseStateHist 
WHERE OrderOpId = '01M6JEEDRMR3316WM4751MV0R8' 
  AND NoUrut = 1 
  AND OpCaseState = 1;

COMMIT TRANSACTION;
GO

-- ============================================================
-- Verification Queries
-- ============================================================
-- 1. Pastikan OrderOpState = 0
SELECT OrderOpId, OrderOpState, ScheduledDate 
FROM BILRG_OpCase 
WHERE OrderOpId = '01M6JEEDRMR3316WM4751MV0R8';

-- 2. Pastikan OpCaseState = 0
SELECT OrderOpId, OpCaseState 
FROM BILRG_OpCaseAktif 
WHERE OrderOpId = '01M6JEEDRMR3316WM4751MV0R8';

-- 3. Pastikan hanya tersisa NoUrut = 0 (OpCaseState = 0), tidak ada NoUrut = 1
SELECT OrderOpId, NoUrut, OpCaseState, StateTimestamp 
FROM BILRG_OpCaseStateHist 
WHERE OrderOpId = '01M6JEEDRMR3316WM4751MV0R8' 
ORDER BY NoUrut;
