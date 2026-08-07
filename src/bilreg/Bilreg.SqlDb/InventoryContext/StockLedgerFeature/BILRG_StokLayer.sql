SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Stock Ledger Phase 1 / P1-S5
-- Additive Stock Layer persistence (StockLayerModel).
-- Depleted layers (RemainingQuantity = 0) are retained — no delete-on-zero.
-- No FK to tb_stok / tb_buku. No business-logic triggers.
IF OBJECT_ID('dbo.BILRG_StokLayer', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BILRG_StokLayer
    (
        StockLayerId            VARCHAR(26)   NOT NULL CONSTRAINT DF_BILRG_StokLayer_StockLayerId DEFAULT(''),
        BrgId                   VARCHAR(13)   NOT NULL CONSTRAINT DF_BILRG_StokLayer_BrgId DEFAULT(''),
        ReceiptSourceId         VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_StokLayer_ReceiptSourceId DEFAULT(''),
        LayananId               VARCHAR(5)    NOT NULL CONSTRAINT DF_BILRG_StokLayer_LayananId DEFAULT(''),
        LayerFormingMovementId  VARCHAR(26)   NOT NULL CONSTRAINT DF_BILRG_StokLayer_LayerFormingMovementId DEFAULT(''),
        InitialQuantity         DECIMAL(18,4) NOT NULL CONSTRAINT DF_BILRG_StokLayer_InitialQuantity DEFAULT(0),
        RemainingQuantity       DECIMAL(18,4) NOT NULL CONSTRAINT DF_BILRG_StokLayer_RemainingQuantity DEFAULT(0),
        AmountPerUnit           DECIMAL(18,4) NOT NULL CONSTRAINT DF_BILRG_StokLayer_AmountPerUnit DEFAULT(0),
        ExpirationDate          DATETIME      NOT NULL CONSTRAINT DF_BILRG_StokLayer_ExpirationDate DEFAULT('3000-01-01'),
        EffectiveReceiptTime    DATETIME      NOT NULL CONSTRAINT DF_BILRG_StokLayer_EffectiveReceiptTime DEFAULT('3000-01-01'),
        Origin                  INT           NOT NULL CONSTRAINT DF_BILRG_StokLayer_Origin DEFAULT(0),
        Batch                   VARCHAR(20)   NOT NULL CONSTRAINT DF_BILRG_StokLayer_Batch DEFAULT(''),

        CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokLayer_CrtUser DEFAULT(''),
        CrtDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokLayer_CrtDate DEFAULT('3000-01-01'),
        UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokLayer_UpdUser DEFAULT(''),
        UpdDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokLayer_UpdDate DEFAULT('3000-01-01'),
        VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokLayer_VodUser DEFAULT(''),
        VodDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokLayer_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_StokLayer PRIMARY KEY CLUSTERED (StockLayerId)
    );
END
GO

-- FIFO candidate scan: Item + Location (+ optional ED) ordered by Effective Receipt Time, Layer Id.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokLayer_FifoCandidates'
      AND object_id = OBJECT_ID('dbo.BILRG_StokLayer'))
BEGIN
    CREATE INDEX IX_BILRG_StokLayer_FifoCandidates
        ON dbo.BILRG_StokLayer (BrgId, LayananId, ExpirationDate, EffectiveReceiptTime, StockLayerId)
        INCLUDE (ReceiptSourceId, RemainingQuantity, AmountPerUnit, Origin, Batch)
        WHERE RemainingQuantity > 0;
END
GO

-- Position / write-scope load including depleted layers.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokLayer_WriteScope'
      AND object_id = OBJECT_ID('dbo.BILRG_StokLayer'))
BEGIN
    CREATE INDEX IX_BILRG_StokLayer_WriteScope
        ON dbo.BILRG_StokLayer (BrgId, ReceiptSourceId, LayananId, EffectiveReceiptTime, StockLayerId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokLayer_LayerFormingMovementId'
      AND object_id = OBJECT_ID('dbo.BILRG_StokLayer'))
BEGIN
    CREATE INDEX IX_BILRG_StokLayer_LayerFormingMovementId
        ON dbo.BILRG_StokLayer (LayerFormingMovementId)
        WHERE LayerFormingMovementId <> '';
END
GO
