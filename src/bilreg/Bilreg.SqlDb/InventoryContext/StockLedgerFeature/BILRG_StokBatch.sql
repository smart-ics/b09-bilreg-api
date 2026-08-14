-- BILRG_StokBatch.sql
-- Stock Ledger v2 — hospital-wide Stock Batch (Item + Receipt Source).
-- ADR-STL-002: omit VodUser/VodDate; voids are reverse-journal only (no soft-delete).

IF OBJECT_ID('BILRG_StokBatch', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_StokBatch
    (
        StokBatchId     VARCHAR(12)   NOT NULL CONSTRAINT DF_BILRG_StokBatch_StokBatchId DEFAULT(''),
        BrgId           VARCHAR(13)   NOT NULL CONSTRAINT DF_BILRG_StokBatch_BrgId DEFAULT(''),
        BrgMasukReffId  VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_StokBatch_BrgMasukReffId DEFAULT(''),
        QtySisa         DECIMAL(18,0) NOT NULL CONSTRAINT DF_BILRG_StokBatch_QtySisa DEFAULT(0),
        Hpp             DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_StokBatch_Hpp DEFAULT(0),
        TglMasuk        DATETIME      NOT NULL CONSTRAINT DF_BILRG_StokBatch_TglMasuk DEFAULT('3000-01-01'),
        PoReffId        VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_StokBatch_PoReffId DEFAULT(''),
        Version         BIGINT        NOT NULL CONSTRAINT DF_BILRG_StokBatch_Version DEFAULT(0),

        CrtUser         VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_StokBatch_CrtUser DEFAULT(''),
        CrtDate         DATETIME      NOT NULL CONSTRAINT DF_BILRG_StokBatch_CrtDate DEFAULT('3000-01-01'),
        UpdUser         VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_StokBatch_UpdUser DEFAULT(''),
        UpdDate         DATETIME      NOT NULL CONSTRAINT DF_BILRG_StokBatch_UpdDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_StokBatch PRIMARY KEY CLUSTERED (StokBatchId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_BILRG_StokBatch_BrgId_BrgMasukReffId'
      AND object_id = OBJECT_ID('BILRG_StokBatch'))
BEGIN
    CREATE UNIQUE INDEX UX_BILRG_StokBatch_BrgId_BrgMasukReffId
        ON BILRG_StokBatch (BrgId, BrgMasukReffId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokBatch_BrgId_TglMasuk_BrgMasukReffId'
      AND object_id = OBJECT_ID('BILRG_StokBatch'))
BEGIN
    CREATE INDEX IX_BILRG_StokBatch_BrgId_TglMasuk_BrgMasukReffId
        ON BILRG_StokBatch (BrgId, TglMasuk, BrgMasukReffId);
END
GO
