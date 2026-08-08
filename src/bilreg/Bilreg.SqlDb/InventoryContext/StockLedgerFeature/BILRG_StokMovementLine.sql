SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Stock Ledger Phase 1 / P1-S5
-- Additive Stock Movement lines (StockMovementLineType).
-- Detail ownership: composite PK (StockMovementId, [LineNo]).
-- [LineNo] is delimited because LINENO is a T-SQL reserved keyword.
-- No FK constraints (logical ownership only). No business-logic triggers.
IF OBJECT_ID('dbo.BILRG_StokMovementLine', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BILRG_StokMovementLine
    (
        StockMovementId   VARCHAR(26)   NOT NULL CONSTRAINT DF_BILRG_StokMovementLine_StockMovementId DEFAULT(''),
        [LineNo]          INT           NOT NULL CONSTRAINT DF_BILRG_StokMovementLine_LineNo DEFAULT(0),
        BrgId             VARCHAR(13)   NOT NULL CONSTRAINT DF_BILRG_StokMovementLine_BrgId DEFAULT(''),
        ReceiptSourceId   VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_StokMovementLine_ReceiptSourceId DEFAULT(''),
        LayananId         VARCHAR(5)    NOT NULL CONSTRAINT DF_BILRG_StokMovementLine_LayananId DEFAULT(''),
        Direction         INT           NOT NULL CONSTRAINT DF_BILRG_StokMovementLine_Direction DEFAULT(0),
        Quantity          DECIMAL(18,4) NOT NULL CONSTRAINT DF_BILRG_StokMovementLine_Quantity DEFAULT(0),
        AmountPerUnit     DECIMAL(18,4) NOT NULL CONSTRAINT DF_BILRG_StokMovementLine_AmountPerUnit DEFAULT(0),
        Origin            INT           NOT NULL CONSTRAINT DF_BILRG_StokMovementLine_Origin DEFAULT(0),
        StockLayerId      VARCHAR(26)   NOT NULL CONSTRAINT DF_BILRG_StokMovementLine_StockLayerId DEFAULT(''),

        CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokMovementLine_CrtUser DEFAULT(''),
        CrtDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokMovementLine_CrtDate DEFAULT('3000-01-01'),
        UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokMovementLine_UpdUser DEFAULT(''),
        UpdDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokMovementLine_UpdDate DEFAULT('3000-01-01'),
        VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokMovementLine_VodUser DEFAULT(''),
        VodDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokMovementLine_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_StokMovementLine PRIMARY KEY CLUSTERED (StockMovementId, [LineNo])
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokMovementLine_WriteScope'
      AND object_id = OBJECT_ID('dbo.BILRG_StokMovementLine'))
BEGIN
    CREATE INDEX IX_BILRG_StokMovementLine_WriteScope
        ON dbo.BILRG_StokMovementLine (BrgId, ReceiptSourceId, LayananId, StockMovementId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokMovementLine_StockLayerId'
      AND object_id = OBJECT_ID('dbo.BILRG_StokMovementLine'))
BEGIN
    CREATE INDEX IX_BILRG_StokMovementLine_StockLayerId
        ON dbo.BILRG_StokMovementLine (StockLayerId)
        WHERE StockLayerId <> '';
END
GO
