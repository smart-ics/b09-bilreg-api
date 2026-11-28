-- Migration: Tambah kolom ClearanceId pada BILRG_StartOp
-- Merealisasikan keputusan GAP-002, GAP-003 & OQ-003

IF COL_LENGTH('BILRG_StartOp', 'ClearanceId') IS NULL
BEGIN
    ALTER TABLE BILRG_StartOp
        ADD ClearanceId VARCHAR(26) NOT NULL
            CONSTRAINT DF_BILRG_StartOp_ClearanceId DEFAULT('')
END
GO
