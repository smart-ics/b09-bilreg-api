-- Migration: IGD-TINDAKAN-BHP-ALIGNMENT (P1-S01)
-- Adds stock tracking columns (LayananId, PakaiBrgId) to BILRG_TindakanIgd and BILRG_BhpIgd,
-- and creates child tables BILRG_TindakanIgdPpa and BILRG_TindakanIgdBarang.

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BILRG_TindakanIgd' AND COLUMN_NAME = 'LayananId')
BEGIN
    ALTER TABLE BILRG_TindakanIgd ADD
        LayananId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_TindakanIgd_LayananId DEFAULT('');
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BILRG_TindakanIgd' AND COLUMN_NAME = 'PakaiBrgId')
BEGIN
    ALTER TABLE BILRG_TindakanIgd ADD
        PakaiBrgId VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_TindakanIgd_PakaiBrgId DEFAULT('');
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BILRG_BhpIgd' AND COLUMN_NAME = 'LayananId')
BEGIN
    ALTER TABLE BILRG_BhpIgd ADD
        LayananId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_BhpIgd_LayananId DEFAULT('');
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BILRG_BhpIgd' AND COLUMN_NAME = 'PakaiBrgId')
BEGIN
    ALTER TABLE BILRG_BhpIgd ADD
        PakaiBrgId VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_BhpIgd_PakaiBrgId DEFAULT('');
END
GO

IF OBJECT_ID('BILRG_TindakanIgdPpa', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_TindakanIgdPpa (
        TindakanIgdId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdPpa_TindakanIgdId DEFAULT(''),
        KomponenId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdPpa_KomponenId DEFAULT(''),
        PpaId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdPpa_PpaId DEFAULT(''),
        PpaName VARCHAR(45) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdPpa_PpaName DEFAULT(''),

        CONSTRAINT PK_BILRG_TindakanIgdPpa PRIMARY KEY CLUSTERED (TindakanIgdId, KomponenId, PpaId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('BILRG_TindakanIgdPpa')
      AND name = 'IX_BILRG_TindakanIgdPpa_TindakanIgdId'
)
BEGIN
    CREATE INDEX IX_BILRG_TindakanIgdPpa_TindakanIgdId ON BILRG_TindakanIgdPpa(TindakanIgdId);
END
GO

IF OBJECT_ID('BILRG_TindakanIgdBarang', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_TindakanIgdBarang (
        TindakanIgdId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_TindakanIgdId DEFAULT(''),
        NoUrut INT NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_NoUrut DEFAULT(0),
        BrgId VARCHAR(13) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_BrgId DEFAULT(''),
        SatuanBomId VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_SatuanBomId DEFAULT(''),
        QtyBom DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_QtyBom DEFAULT(0),
        SatuanKecilId VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_SatuanKecilId DEFAULT(''),
        QtyKecilRequested DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_QtyKecilRequested DEFAULT(0),
        QtyKecilDeducted DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_QtyKecilDeducted DEFAULT(0),
        QtyKecilPending DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_QtyKecilPending DEFAULT(0),
        IsChargeBarang BIT NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_IsChargeBarang DEFAULT(0),
        PakaiBrgId VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_PakaiBrgId DEFAULT(''),
        TrsBillingId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_TrsBillingId DEFAULT(''),
        IsVoid BIT NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_IsVoid DEFAULT(0),

        CONSTRAINT PK_BILRG_TindakanIgdBarang PRIMARY KEY CLUSTERED (TindakanIgdId, NoUrut)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('BILRG_TindakanIgdBarang')
      AND name = 'IX_BILRG_TindakanIgdBarang_TindakanIgdId'
)
BEGIN
    CREATE INDEX IX_BILRG_TindakanIgdBarang_TindakanIgdId ON BILRG_TindakanIgdBarang(TindakanIgdId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('BILRG_TindakanIgdBarang')
      AND name = 'IX_BILRG_TindakanIgdBarang_PakaiBrgId'
)
BEGIN
    CREATE INDEX IX_BILRG_TindakanIgdBarang_PakaiBrgId ON BILRG_TindakanIgdBarang(PakaiBrgId);
END
GO
