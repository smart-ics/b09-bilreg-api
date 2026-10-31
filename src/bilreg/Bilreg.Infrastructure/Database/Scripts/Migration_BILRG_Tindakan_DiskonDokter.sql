-- Migration: Add Diskon Dokter columns to BILRG_Tindakan and BILRG_TindakanKomponen tables
-- Slice: P1-S01 (Outcome: OC-TRK-TINDAKAN, SubContext: TRK-TINDAKAN-DISKON-DOKTER)
-- Target Database: HOSPITAL_PKL

-- 1. Add IsDiskonDokter column to BILRG_Tindakan
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_Tindakan') 
      AND name = 'IsDiskonDokter'
)
BEGIN
    ALTER TABLE BILRG_Tindakan
    ADD IsDiskonDokter BIT NOT NULL CONSTRAINT DF_BILRG_Tindakan_IsDiskonDokter DEFAULT(0);
END
GO

-- 2. Add DiskonDokter column to BILRG_Tindakan
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_Tindakan') 
      AND name = 'DiskonDokter'
)
BEGIN
    ALTER TABLE BILRG_Tindakan
    ADD DiskonDokter DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_Tindakan_DiskonDokter DEFAULT(0);
END
GO

-- 3. Add Diskon column to BILRG_TindakanKomponen
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_TindakanKomponen') 
      AND name = 'Diskon'
)
BEGIN
    ALTER TABLE BILRG_TindakanKomponen
    ADD Diskon DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TindakanKomponen_Diskon DEFAULT(0);
END
GO

-- Backfill existing rows to ensure non-null defaults
UPDATE BILRG_Tindakan
SET IsDiskonDokter = 0
WHERE IsDiskonDokter IS NULL;
GO

UPDATE BILRG_Tindakan
SET DiskonDokter = 0
WHERE DiskonDokter IS NULL;
GO

UPDATE BILRG_TindakanKomponen
SET Diskon = 0
WHERE Diskon IS NULL;
GO
