-- Migration: Add Qty column to BILRG_Tindakan table
-- Slice: P1-S01 (Outcome: OC-RJL-TINDAKAN)
-- Target Database: HOSPITAL_PKL

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_Tindakan') 
      AND name = 'Qty'
)
BEGIN
    ALTER TABLE BILRG_Tindakan
    ADD Qty INT NOT NULL CONSTRAINT DF_BILRG_Tindakan_Qty DEFAULT(1);
END
GO

-- Backfill existing rows to ensure Qty >= 1
UPDATE BILRG_Tindakan
SET Qty = 1
WHERE Qty IS NULL OR Qty <= 0;
GO
