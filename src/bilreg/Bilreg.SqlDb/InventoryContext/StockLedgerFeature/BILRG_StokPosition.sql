SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Stock Ledger Phase 1 / P1-S5
-- Additive Stock Position write-scope state (StockPositionModel).
-- Natural identity = Item + Receipt Source + Stock Location (no separate PositionId in domain).
-- Version is the optimistic concurrency token (G-06). No authority flag.
-- No FK to tb_stok / tb_buku. No business-logic triggers.
IF OBJECT_ID('dbo.BILRG_StokPosition', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BILRG_StokPosition
    (
        BrgId           VARCHAR(13) NOT NULL CONSTRAINT DF_BILRG_StokPosition_BrgId DEFAULT(''),
        ReceiptSourceId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_StokPosition_ReceiptSourceId DEFAULT(''),
        LayananId       VARCHAR(5)  NOT NULL CONSTRAINT DF_BILRG_StokPosition_LayananId DEFAULT(''),
        Version         BIGINT      NOT NULL CONSTRAINT DF_BILRG_StokPosition_Version DEFAULT(0),

        CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokPosition_CrtUser DEFAULT(''),
        CrtDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokPosition_CrtDate DEFAULT('3000-01-01'),
        UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokPosition_UpdUser DEFAULT(''),
        UpdDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokPosition_UpdDate DEFAULT('3000-01-01'),
        VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokPosition_VodUser DEFAULT(''),
        VodDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokPosition_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_StokPosition PRIMARY KEY CLUSTERED (BrgId, ReceiptSourceId, LayananId)
    );
END
GO

-- Supporting lookup by Item + Location across Receipt Sources (availability orchestration later).
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokPosition_ItemLocation'
      AND object_id = OBJECT_ID('dbo.BILRG_StokPosition'))
BEGIN
    CREATE INDEX IX_BILRG_StokPosition_ItemLocation
        ON dbo.BILRG_StokPosition (BrgId, LayananId, ReceiptSourceId);
END
GO
