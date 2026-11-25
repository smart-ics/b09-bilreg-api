-- BILRG_StockOpnameAdjustment.sql
-- Table: BILRG_StockOpnameAdjustment (Audit Linking between Stock Opname Session & Stock Ledger Adjustment)
-- Outcome: OC-INV-STOK-OPNAME
-- Decisions: TD-03, GAP-003

IF OBJECT_ID('dbo.BILRG_StockOpnameAdjustment', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BILRG_StockOpnameAdjustment
    (
        AdjustmentId        VARCHAR(12)     NOT NULL,
        SessionId           VARCHAR(30)     NOT NULL,
        BrgId               VARCHAR(13)     NOT NULL,
        BatchNo             VARCHAR(30)     NOT NULL,
        ExpiredDate         VARCHAR(10)     NOT NULL,
        LayananId           VARCHAR(10)     NOT NULL,
        AdjustmentQty       DECIMAL(18, 0)  NOT NULL,
        BaseSatuanId        VARCHAR(10)     NOT NULL,
        Hpp                 DECIMAL(18, 2)  NOT NULL,
        NilaiFinansial      DECIMAL(18, 2)  NOT NULL,
        StokMutasiId        VARCHAR(12)     NOT NULL,
        LegacyBukuId        VARCHAR(10)     NOT NULL,
        MovementKind        INT             NOT NULL,
        ExecutedAt          DATETIME        NOT NULL,
        ExecutedBy          VARCHAR(50)     NOT NULL,

        CONSTRAINT PK_BILRG_StockOpnameAdjustment PRIMARY KEY CLUSTERED (AdjustmentId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_BILRG_StockOpnameAdjustment_SessionTuple'
      AND object_id = OBJECT_ID('dbo.BILRG_StockOpnameAdjustment'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_BILRG_StockOpnameAdjustment_SessionTuple
        ON dbo.BILRG_StockOpnameAdjustment (SessionId, BrgId, BatchNo, ExpiredDate);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StockOpnameAdjustment_SessionId'
      AND object_id = OBJECT_ID('dbo.BILRG_StockOpnameAdjustment'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_StockOpnameAdjustment_SessionId
        ON dbo.BILRG_StockOpnameAdjustment (SessionId);
END
GO
