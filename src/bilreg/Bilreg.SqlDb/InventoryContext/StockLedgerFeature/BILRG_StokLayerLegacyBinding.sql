SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Stock Ledger Phase 5 / P5-S3
-- Additive coexistence projection: StockLayerId <-> LegacyRowId (tb_stok.fs_kd_trs).
-- Compatibility metadata only — not Domain identity, not authority transfer.
-- No FK to tb_stok / tb_buku. No business-logic triggers.
IF OBJECT_ID('dbo.BILRG_StokLayerLegacyBinding', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BILRG_StokLayerLegacyBinding
    (
        StockLayerId    VARCHAR(26)   NOT NULL CONSTRAINT DF_BILRG_StokLayerLegacyBinding_StockLayerId DEFAULT(''),
        LegacyRowId     VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_StokLayerLegacyBinding_LegacyRowId DEFAULT(''),
        BrgId           VARCHAR(13)   NOT NULL CONSTRAINT DF_BILRG_StokLayerLegacyBinding_BrgId DEFAULT(''),
        ReceiptSourceId VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_StokLayerLegacyBinding_ReceiptSourceId DEFAULT(''),
        LayananId       VARCHAR(5)    NOT NULL CONSTRAINT DF_BILRG_StokLayerLegacyBinding_LayananId DEFAULT(''),
        AmountPerUnit   DECIMAL(18,4) NOT NULL CONSTRAINT DF_BILRG_StokLayerLegacyBinding_AmountPerUnit DEFAULT(0),
        ExpirationDate  DATETIME      NOT NULL CONSTRAINT DF_BILRG_StokLayerLegacyBinding_ExpirationDate DEFAULT('3000-01-01'),
        Batch           VARCHAR(20)   NOT NULL CONSTRAINT DF_BILRG_StokLayerLegacyBinding_Batch DEFAULT(''),
        BoundAt         DATETIME      NOT NULL CONSTRAINT DF_BILRG_StokLayerLegacyBinding_BoundAt DEFAULT('3000-01-01'),

        CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokLayerLegacyBinding_CrtUser DEFAULT(''),
        CrtDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokLayerLegacyBinding_CrtDate DEFAULT('3000-01-01'),
        UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokLayerLegacyBinding_UpdUser DEFAULT(''),
        UpdDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokLayerLegacyBinding_UpdDate DEFAULT('3000-01-01'),
        VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokLayerLegacyBinding_VodUser DEFAULT(''),
        VodDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokLayerLegacyBinding_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_StokLayerLegacyBinding PRIMARY KEY CLUSTERED (StockLayerId)
    );
END
GO

-- Lookup by physical legacy row (void / sync / diagnostics).
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokLayerLegacyBinding_LegacyRowId'
      AND object_id = OBJECT_ID('dbo.BILRG_StokLayerLegacyBinding'))
BEGIN
    CREATE INDEX IX_BILRG_StokLayerLegacyBinding_LegacyRowId
        ON dbo.BILRG_StokLayerLegacyBinding (LegacyRowId, LayananId)
        WHERE LegacyRowId <> '';
END
GO

-- Write-scope scan for lazy unique establishment / sync anchors.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokLayerLegacyBinding_WriteScope'
      AND object_id = OBJECT_ID('dbo.BILRG_StokLayerLegacyBinding'))
BEGIN
    CREATE INDEX IX_BILRG_StokLayerLegacyBinding_WriteScope
        ON dbo.BILRG_StokLayerLegacyBinding (BrgId, ReceiptSourceId, LayananId, StockLayerId);
END
GO
