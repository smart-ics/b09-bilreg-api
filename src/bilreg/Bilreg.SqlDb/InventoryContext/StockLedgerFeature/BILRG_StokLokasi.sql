-- BILRG_StokLokasi.sql
-- Stock Ledger v2 — Location Stock Balance (batch + layanan + TglEd).
-- ADR-STL-002: omit VodUser/VodDate; voids are reverse-journal only (no soft-delete).
-- Amendment: denormalized TglMasuk for FEFO/FIFO candidate scans without join.
-- GAP-STL-005: NoBatch is stored but excluded from L1 uniqueness.

IF OBJECT_ID('BILRG_StokLokasi', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_StokLokasi
    (
        StokLokasiId    VARCHAR(12)   NOT NULL CONSTRAINT DF_BILRG_StokLokasi_StokLokasiId DEFAULT(''),
        StokBatchId     VARCHAR(12)   NOT NULL CONSTRAINT DF_BILRG_StokLokasi_StokBatchId DEFAULT(''),
        BrgId           VARCHAR(13)   NOT NULL CONSTRAINT DF_BILRG_StokLokasi_BrgId DEFAULT(''),
        BrgMasukReffId  VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_StokLokasi_BrgMasukReffId DEFAULT(''),
        LayananId       VARCHAR(5)    NOT NULL CONSTRAINT DF_BILRG_StokLokasi_LayananId DEFAULT(''),
        TglEd           DATETIME      NOT NULL CONSTRAINT DF_BILRG_StokLokasi_TglEd DEFAULT('3000-01-01'),
        NoBatch         VARCHAR(15)   NOT NULL CONSTRAINT DF_BILRG_StokLokasi_NoBatch DEFAULT(''),
        QtySisa         DECIMAL(18,0) NOT NULL CONSTRAINT DF_BILRG_StokLokasi_QtySisa DEFAULT(0),
        Version         BIGINT        NOT NULL CONSTRAINT DF_BILRG_StokLokasi_Version DEFAULT(0),
        TglMasuk        DATETIME      NOT NULL CONSTRAINT DF_BILRG_StokLokasi_TglMasuk DEFAULT('3000-01-01'),

        CrtUser         VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_StokLokasi_CrtUser DEFAULT(''),
        CrtDate         DATETIME      NOT NULL CONSTRAINT DF_BILRG_StokLokasi_CrtDate DEFAULT('3000-01-01'),
        UpdUser         VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_StokLokasi_UpdUser DEFAULT(''),
        UpdDate         DATETIME      NOT NULL CONSTRAINT DF_BILRG_StokLokasi_UpdDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_StokLokasi PRIMARY KEY CLUSTERED (StokLokasiId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_BILRG_StokLokasi_StokBatchId_LayananId_TglEd'
      AND object_id = OBJECT_ID('BILRG_StokLokasi'))
BEGIN
    CREATE UNIQUE INDEX UX_BILRG_StokLokasi_StokBatchId_LayananId_TglEd
        ON BILRG_StokLokasi (StokBatchId, LayananId, TglEd);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokLokasi_FifoFefo'
      AND object_id = OBJECT_ID('BILRG_StokLokasi'))
BEGIN
    CREATE INDEX IX_BILRG_StokLokasi_FifoFefo
        ON BILRG_StokLokasi (BrgId, LayananId, TglEd, TglMasuk)
        INCLUDE (StokLokasiId, StokBatchId, QtySisa, Version)
        WHERE QtySisa > 0;
END
GO
