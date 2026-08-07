SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Stock Ledger Phase 1 / P1-S5
-- Additive unique source consequence / sync batch idempotency keys (G-07).
-- Ensures one accountable Stock Ledger consequence per source key.
-- No FK to tb_stok / tb_buku. No business-logic triggers.
IF OBJECT_ID('dbo.BILRG_StokSourceIdempotency', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BILRG_StokSourceIdempotency
    (
        IdempotencyId       VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_StokSourceIdempotency_IdempotencyId DEFAULT(''),
        IdempotencyKind     INT          NOT NULL CONSTRAINT DF_BILRG_StokSourceIdempotency_IdempotencyKind DEFAULT(0),
        IdempotencyKey      VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_StokSourceIdempotency_IdempotencyKey DEFAULT(''),
        SourceTransactionId VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_StokSourceIdempotency_SourceTransactionId DEFAULT(''),
        StockMovementId     VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_StokSourceIdempotency_StockMovementId DEFAULT(''),
        BrgId               VARCHAR(13)  NOT NULL CONSTRAINT DF_BILRG_StokSourceIdempotency_BrgId DEFAULT(''),
        ReceiptSourceId     VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_StokSourceIdempotency_ReceiptSourceId DEFAULT(''),
        ProcessedAt         DATETIME     NOT NULL CONSTRAINT DF_BILRG_StokSourceIdempotency_ProcessedAt DEFAULT('3000-01-01'),

        CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokSourceIdempotency_CrtUser DEFAULT(''),
        CrtDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokSourceIdempotency_CrtDate DEFAULT('3000-01-01'),
        UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokSourceIdempotency_UpdUser DEFAULT(''),
        UpdDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokSourceIdempotency_UpdDate DEFAULT('3000-01-01'),
        VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokSourceIdempotency_VodUser DEFAULT(''),
        VodDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokSourceIdempotency_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_StokSourceIdempotency PRIMARY KEY CLUSTERED (IdempotencyId)
    );
END
GO

-- Unique business key for source consequence / sync batch (G-07).
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_BILRG_StokSourceIdempotency_Kind_Key'
      AND object_id = OBJECT_ID('dbo.BILRG_StokSourceIdempotency'))
BEGIN
    CREATE UNIQUE INDEX UX_BILRG_StokSourceIdempotency_Kind_Key
        ON dbo.BILRG_StokSourceIdempotency (IdempotencyKind, IdempotencyKey)
        WHERE IdempotencyKey <> '';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokSourceIdempotency_SourceTransactionId'
      AND object_id = OBJECT_ID('dbo.BILRG_StokSourceIdempotency'))
BEGIN
    CREATE INDEX IX_BILRG_StokSourceIdempotency_SourceTransactionId
        ON dbo.BILRG_StokSourceIdempotency (SourceTransactionId)
        WHERE SourceTransactionId <> '';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokSourceIdempotency_StockMovementId'
      AND object_id = OBJECT_ID('dbo.BILRG_StokSourceIdempotency'))
BEGIN
    CREATE INDEX IX_BILRG_StokSourceIdempotency_StockMovementId
        ON dbo.BILRG_StokSourceIdempotency (StockMovementId)
        WHERE StockMovementId <> '';
END
GO
