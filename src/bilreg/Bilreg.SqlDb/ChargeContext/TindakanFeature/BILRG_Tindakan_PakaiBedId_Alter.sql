-- Migration: Add PakaiBedId column to BILRG_Tindakan table
-- Slice: P1-S01 (Outcome: OC-TRK-TINDAKAN)

IF COL_LENGTH('BILRG_Tindakan', 'PakaiBedId') IS NULL
BEGIN
    ALTER TABLE BILRG_Tindakan
        ADD PakaiBedId VARCHAR(30) NOT NULL
            CONSTRAINT DF_BILRG_Tindakan_PakaiBedId DEFAULT('');
END
GO

UPDATE BILRG_Tindakan
SET PakaiBedId = ''
WHERE PakaiBedId IS NULL;
GO
