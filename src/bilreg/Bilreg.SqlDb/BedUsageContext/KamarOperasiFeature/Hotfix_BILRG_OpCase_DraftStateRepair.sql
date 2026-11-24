-- ============================================================
-- TD-42: One-Off Data Healing DML
-- ISS-KMO-JADWAL-003 — Draft Reschedule Disappeared Bug
-- Jalankan SATU KALI di environment produksi/staging
-- Setelah deploy backend fix (P1-S01, P1-S02)
-- ============================================================

BEGIN TRANSACTION;

-- Step 1: Identifikasi kasus corrupt (audit dulu sebelum update)
SELECT 
    oc.OrderOpId,
    oc.OrderOpState          AS CurrentState,
    so.ScheduleOpId,
    so.IsDraft               AS IsDraft,
    so.VodDate               AS VodDate
FROM BILRG_OpCase oc
JOIN BILRG_ScheduleOp so ON so.OrderOpId = oc.OrderOpId
WHERE 
    oc.OrderOpState = 1              -- Scheduled
    AND so.IsDraft = 1               -- Tapi jadwalnya masih draf
    AND so.VodDate = '3000-01-01'    -- Dan belum di-void
ORDER BY oc.OrderOpId;

-- Step 2: Healing — rollback OrderOpState ke Requested (0)
UPDATE BILRG_OpCase
SET OrderOpState = 0   -- kembali ke Requested
WHERE OrderOpId IN (
    SELECT oc.OrderOpId
    FROM BILRG_OpCase oc
    JOIN BILRG_ScheduleOp so ON so.OrderOpId = oc.OrderOpId
    WHERE 
        oc.OrderOpState = 1
        AND so.IsDraft = 1
        AND so.VodDate = '3000-01-01'
);

-- Step 3: Verifikasi — pastikan sudah tidak ada kasus corrupt
SELECT COUNT(*) AS JumlahKasusCorrupt
FROM BILRG_OpCase oc
JOIN BILRG_ScheduleOp so ON so.OrderOpId = oc.OrderOpId
WHERE 
    oc.OrderOpState = 1
    AND so.IsDraft = 1
    AND so.VodDate = '3000-01-01';
-- Expected: 0

-- COMMIT hanya jika Step 3 = 0 dan Step 1 audit sesuai ekspektasi
-- ROLLBACK jika ada keraguan
COMMIT;
