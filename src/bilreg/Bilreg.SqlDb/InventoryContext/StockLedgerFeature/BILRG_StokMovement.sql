SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Stock Ledger Phase 1 / P1-S5
-- Additive immutable Stock Movement header (StockMovementModel).
-- No FK to tb_stok / tb_buku. No business-logic triggers.
IF OBJECT_ID('dbo.BILRG_StokMovement', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BILRG_StokMovement
    (
        StockMovementId       VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_StokMovement_StockMovementId DEFAULT(''),
        SourceTransactionId   VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_StokMovement_SourceTransactionId DEFAULT(''),
        MovementKind          INT          NOT NULL CONSTRAINT DF_BILRG_StokMovement_MovementKind DEFAULT(0),
        EffectiveBusinessTime DATETIME     NOT NULL CONSTRAINT DF_BILRG_StokMovement_EffectiveBusinessTime DEFAULT('3000-01-01'),
        Origin                INT          NOT NULL CONSTRAINT DF_BILRG_StokMovement_Origin DEFAULT(0),
        ReversedMovementId    VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_StokMovement_ReversedMovementId DEFAULT(''),
        CorrectedMovementId   VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_StokMovement_CorrectedMovementId DEFAULT(''),

        CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokMovement_CrtUser DEFAULT(''),
        CrtDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokMovement_CrtDate DEFAULT('3000-01-01'),
        UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokMovement_UpdUser DEFAULT(''),
        UpdDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokMovement_UpdDate DEFAULT('3000-01-01'),
        VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokMovement_VodUser DEFAULT(''),
        VodDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokMovement_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_StokMovement PRIMARY KEY CLUSTERED (StockMovementId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokMovement_SourceTransactionId'
      AND object_id = OBJECT_ID('dbo.BILRG_StokMovement'))
BEGIN
    CREATE INDEX IX_BILRG_StokMovement_SourceTransactionId
        ON dbo.BILRG_StokMovement (SourceTransactionId, EffectiveBusinessTime)
        WHERE SourceTransactionId <> '';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokMovement_ReversedMovementId'
      AND object_id = OBJECT_ID('dbo.BILRG_StokMovement'))
BEGIN
    CREATE INDEX IX_BILRG_StokMovement_ReversedMovementId
        ON dbo.BILRG_StokMovement (ReversedMovementId)
        WHERE ReversedMovementId <> '';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokMovement_CorrectedMovementId'
      AND object_id = OBJECT_ID('dbo.BILRG_StokMovement'))
BEGIN
    CREATE INDEX IX_BILRG_StokMovement_CorrectedMovementId
        ON dbo.BILRG_StokMovement (CorrectedMovementId)
        WHERE CorrectedMovementId <> '';
END
GO
