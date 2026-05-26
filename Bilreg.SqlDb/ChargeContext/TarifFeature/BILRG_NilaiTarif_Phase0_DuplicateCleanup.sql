-- Phase 0: remove duplicate BILRG_NilaiTarif variants.
-- Winner per (TarifId, TipeTarifId, KelasId): row with MAX(NilaiTarifId) (ULID — newest import wins).
-- Losers: delete komponen children first, then header rows.
-- Run BILRG_NilaiTarif_Phase0_DuplicateReport.sql first and archive results.

;WITH ranked AS (
    SELECT
        NilaiTarifId,
        ROW_NUMBER() OVER (
            PARTITION BY TarifId, TipeTarifId, KelasId
            ORDER BY NilaiTarifId DESC) AS rn
    FROM BILRG_NilaiTarif
)
DELETE k
FROM BILRG_NilaiTarifKomponen k
INNER JOIN ranked r ON k.NilaiTarifId = r.NilaiTarifId
WHERE r.rn > 1;
GO

;WITH ranked AS (
    SELECT
        NilaiTarifId,
        ROW_NUMBER() OVER (
            PARTITION BY TarifId, TipeTarifId, KelasId
            ORDER BY NilaiTarifId DESC) AS rn
    FROM BILRG_NilaiTarif
)
DELETE n
FROM BILRG_NilaiTarif n
INNER JOIN ranked r ON n.NilaiTarifId = r.NilaiTarifId
WHERE r.rn > 1;
GO
