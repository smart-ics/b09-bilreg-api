-- Migration: Add Dual-Class (PhysicalKelasId, BillingKelasId, TipeKamarId, Tarif, IsTitipKelas, IsNaikKelas)
-- and Rooming-in (MotherPakaiBedId) support to BILRG_RnaPakaiBedAlokasi

IF COL_LENGTH('BILRG_RnaPakaiBedAlokasi', 'PhysicalKelasId') IS NULL
BEGIN
    ALTER TABLE BILRG_RnaPakaiBedAlokasi ADD
        PhysicalKelasId VARCHAR(3) NOT NULL CONSTRAINT DF_BILRG_RnaPakaiBedAlokasi_PhysicalKelasId DEFAULT('');
END
GO

IF COL_LENGTH('BILRG_RnaPakaiBedAlokasi', 'BillingKelasId') IS NULL
BEGIN
    ALTER TABLE BILRG_RnaPakaiBedAlokasi ADD
        BillingKelasId VARCHAR(3) NOT NULL CONSTRAINT DF_BILRG_RnaPakaiBedAlokasi_BillingKelasId DEFAULT('');
END
GO

IF COL_LENGTH('BILRG_RnaPakaiBedAlokasi', 'TipeKamarId') IS NULL
BEGIN
    ALTER TABLE BILRG_RnaPakaiBedAlokasi ADD
        TipeKamarId VARCHAR(1) NOT NULL CONSTRAINT DF_BILRG_RnaPakaiBedAlokasi_TipeKamarId DEFAULT('');
END
GO

IF COL_LENGTH('BILRG_RnaPakaiBedAlokasi', 'Tarif') IS NULL
BEGIN
    ALTER TABLE BILRG_RnaPakaiBedAlokasi ADD
        Tarif DECIMAL(18, 0) NOT NULL CONSTRAINT DF_BILRG_RnaPakaiBedAlokasi_Tarif DEFAULT(0);
END
GO

IF COL_LENGTH('BILRG_RnaPakaiBedAlokasi', 'MotherPakaiBedId') IS NULL
BEGIN
    ALTER TABLE BILRG_RnaPakaiBedAlokasi ADD
        MotherPakaiBedId VARCHAR(15) NOT NULL CONSTRAINT DF_BILRG_RnaPakaiBedAlokasi_MotherPakaiBedId DEFAULT('');
END
GO

IF COL_LENGTH('BILRG_RnaPakaiBedAlokasi', 'IsTitipKelas') IS NULL
BEGIN
    ALTER TABLE BILRG_RnaPakaiBedAlokasi ADD
        IsTitipKelas BIT NOT NULL CONSTRAINT DF_BILRG_RnaPakaiBedAlokasi_IsTitipKelas DEFAULT(0);
END
GO

IF COL_LENGTH('BILRG_RnaPakaiBedAlokasi', 'IsNaikKelas') IS NULL
BEGIN
    ALTER TABLE BILRG_RnaPakaiBedAlokasi ADD
        IsNaikKelas BIT NOT NULL CONSTRAINT DF_BILRG_RnaPakaiBedAlokasi_IsNaikKelas DEFAULT(0);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_RnaPakaiBedAlokasi_MotherPakaiBedId'
      AND object_id = OBJECT_ID('BILRG_RnaPakaiBedAlokasi'))
BEGIN
    CREATE INDEX IX_BILRG_RnaPakaiBedAlokasi_MotherPakaiBedId
        ON BILRG_RnaPakaiBedAlokasi(MotherPakaiBedId)
        WHERE MotherPakaiBedId <> '';
END
GO
