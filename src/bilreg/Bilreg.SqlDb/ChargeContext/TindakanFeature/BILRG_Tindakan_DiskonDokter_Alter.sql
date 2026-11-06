-- Migration: Add Diskon Dokter columns to BILRG_Tindakan and BILRG_TindakanKomponen tables
-- Slice: P1-S01 (Outcome: OC-TRK-TINDAKAN, SubContext: TRK-TINDAKAN-DISKON-DOKTER)

IF COL_LENGTH('BILRG_Tindakan', 'IsDiskonDokter') IS NULL
BEGIN
    ALTER TABLE BILRG_Tindakan
        ADD IsDiskonDokter BIT NOT NULL
            CONSTRAINT DF_BILRG_Tindakan_IsDiskonDokter DEFAULT(0);
END
GO

IF COL_LENGTH('BILRG_Tindakan', 'DiskonDokter') IS NULL
BEGIN
    ALTER TABLE BILRG_Tindakan
        ADD DiskonDokter DECIMAL(18,2) NOT NULL
            CONSTRAINT DF_BILRG_Tindakan_DiskonDokter DEFAULT(0);
END
GO

IF COL_LENGTH('BILRG_TindakanKomponen', 'Diskon') IS NULL
BEGIN
    ALTER TABLE BILRG_TindakanKomponen
        ADD Diskon DECIMAL(18,2) NOT NULL
            CONSTRAINT DF_BILRG_TindakanKomponen_Diskon DEFAULT(0);
END
GO

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
