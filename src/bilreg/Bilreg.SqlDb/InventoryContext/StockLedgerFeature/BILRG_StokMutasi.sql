-- BILRG_StokMutasi.sql
-- Stock Ledger v2 — append-only Stock Movement journal.
-- ADR-STL-002: omit VodUser/VodDate; voids are reverse-journal only (no soft-delete).

IF OBJECT_ID('BILRG_StokMutasi', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_StokMutasi
    (
        StokMutasiId     VARCHAR(12)   NOT NULL CONSTRAINT DF_BILRG_StokMutasi_StokMutasiId DEFAULT(''),
        StokLokasiId     VARCHAR(12)   NOT NULL CONSTRAINT DF_BILRG_StokMutasi_StokLokasiId DEFAULT(''),
        StokBatchId      VARCHAR(12)   NOT NULL CONSTRAINT DF_BILRG_StokMutasi_StokBatchId DEFAULT(''),
        BrgId            VARCHAR(13)   NOT NULL CONSTRAINT DF_BILRG_StokMutasi_BrgId DEFAULT(''),
        BrgMasukReffId   VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_StokMutasi_BrgMasukReffId DEFAULT(''),
        LayananId        VARCHAR(5)    NOT NULL CONSTRAINT DF_BILRG_StokMutasi_LayananId DEFAULT(''),
        TglEd            DATETIME      NOT NULL CONSTRAINT DF_BILRG_StokMutasi_TglEd DEFAULT('3000-01-01'),
        TrsReffId        VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_StokMutasi_TrsReffId DEFAULT(''),
        MovementKind     INT           NOT NULL CONSTRAINT DF_BILRG_StokMutasi_MovementKind DEFAULT(0),
        QtyIn            DECIMAL(18,0) NOT NULL CONSTRAINT DF_BILRG_StokMutasi_QtyIn DEFAULT(0),
        QtyOut           DECIMAL(18,0) NOT NULL CONSTRAINT DF_BILRG_StokMutasi_QtyOut DEFAULT(0),
        Hpp              DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_StokMutasi_Hpp DEFAULT(0),
        PoReffId         VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_StokMutasi_PoReffId DEFAULT(''),
        TglMutasi        DATETIME      NOT NULL CONSTRAINT DF_BILRG_StokMutasi_TglMutasi DEFAULT('3000-01-01'),
        ReversesMutasiId VARCHAR(12)   NOT NULL CONSTRAINT DF_BILRG_StokMutasi_ReversesMutasiId DEFAULT(''),

        CrtUser          VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_StokMutasi_CrtUser DEFAULT(''),
        CrtDate          DATETIME      NOT NULL CONSTRAINT DF_BILRG_StokMutasi_CrtDate DEFAULT('3000-01-01'),
        UpdUser          VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_StokMutasi_UpdUser DEFAULT(''),
        UpdDate          DATETIME      NOT NULL CONSTRAINT DF_BILRG_StokMutasi_UpdDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_StokMutasi PRIMARY KEY CLUSTERED (StokMutasiId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId'
      AND object_id = OBJECT_ID('BILRG_StokMutasi'))
BEGIN
    CREATE UNIQUE INDEX UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId
        ON BILRG_StokMutasi (TrsReffId, MovementKind, StokLokasiId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokMutasi_StokLokasiId_TglMutasi'
      AND object_id = OBJECT_ID('BILRG_StokMutasi'))
BEGIN
    CREATE INDEX IX_BILRG_StokMutasi_StokLokasiId_TglMutasi
        ON BILRG_StokMutasi (StokLokasiId, TglMutasi);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokMutasi_StokBatchId_TglMutasi'
      AND object_id = OBJECT_ID('BILRG_StokMutasi'))
BEGIN
    CREATE INDEX IX_BILRG_StokMutasi_StokBatchId_TglMutasi
        ON BILRG_StokMutasi (StokBatchId, TglMutasi);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokMutasi_TrsReffId'
      AND object_id = OBJECT_ID('BILRG_StokMutasi'))
BEGIN
    CREATE INDEX IX_BILRG_StokMutasi_TrsReffId
        ON BILRG_StokMutasi (TrsReffId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokMutasi_ReversesMutasiId'
      AND object_id = OBJECT_ID('BILRG_StokMutasi'))
BEGIN
    CREATE INDEX IX_BILRG_StokMutasi_ReversesMutasiId
        ON BILRG_StokMutasi (ReversesMutasiId)
        WHERE ReversesMutasiId <> '';
END
GO
