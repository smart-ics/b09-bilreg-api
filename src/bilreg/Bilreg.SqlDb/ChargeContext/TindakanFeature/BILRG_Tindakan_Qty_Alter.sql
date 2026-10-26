-- Migration: Add Qty column to BILRG_Tindakan table
-- Slice: P1-S01 (Outcome: OC-RJL-TINDAKAN)

IF COL_LENGTH('BILRG_Tindakan', 'Qty') IS NULL
BEGIN
    ALTER TABLE BILRG_Tindakan
        ADD Qty INT NOT NULL
            CONSTRAINT DF_BILRG_Tindakan_Qty DEFAULT(1);
END
GO

UPDATE BILRG_Tindakan
SET Qty = 1
WHERE Qty IS NULL OR Qty <= 0;
GO
