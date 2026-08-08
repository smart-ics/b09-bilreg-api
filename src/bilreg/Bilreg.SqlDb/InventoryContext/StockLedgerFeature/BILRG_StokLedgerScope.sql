SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Stock Ledger Phase 1 / P1-S5
-- Additive coexistence state for Reconstruction Scope (StockLedgerScopeStateModel).
-- Scope identity = Item + Receipt Source (across all Stock Locations).
-- Synchronization Position is mechanism-neutral opaque bytes + algorithm version.
-- No IsAuthoritative / authority semantics. No FK to tb_stok / tb_buku.
IF OBJECT_ID('dbo.BILRG_StokLedgerScope', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BILRG_StokLedgerScope
    (
        BrgId                         VARCHAR(13)   NOT NULL CONSTRAINT DF_BILRG_StokLedgerScope_BrgId DEFAULT(''),
        ReceiptSourceId               VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_StokLedgerScope_ReceiptSourceId DEFAULT(''),
        ReconstructionStatus          INT           NOT NULL CONSTRAINT DF_BILRG_StokLedgerScope_ReconstructionStatus DEFAULT(0),
        SynchronizationState          INT           NOT NULL CONSTRAINT DF_BILRG_StokLedgerScope_SynchronizationState DEFAULT(0),
        SynchronizationPositionOpaque VARBINARY(512) NOT NULL CONSTRAINT DF_BILRG_StokLedgerScope_SynchronizationPositionOpaque DEFAULT(0x),
        AlgorithmVersion              VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_StokLedgerScope_AlgorithmVersion DEFAULT(''),
        ReconstructionBasisVersion    VARCHAR(100)  NOT NULL CONSTRAINT DF_BILRG_StokLedgerScope_ReconstructionBasisVersion DEFAULT(''),
        InconsistencyReason           VARCHAR(500)  NOT NULL CONSTRAINT DF_BILRG_StokLedgerScope_InconsistencyReason DEFAULT(''),

        CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokLedgerScope_CrtUser DEFAULT(''),
        CrtDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokLedgerScope_CrtDate DEFAULT('3000-01-01'),
        UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokLedgerScope_UpdUser DEFAULT(''),
        UpdDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokLedgerScope_UpdDate DEFAULT('3000-01-01'),
        VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokLedgerScope_VodUser DEFAULT(''),
        VodDate DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokLedgerScope_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_StokLedgerScope PRIMARY KEY CLUSTERED (BrgId, ReceiptSourceId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_StokLedgerScope_ReconstructionStatus'
      AND object_id = OBJECT_ID('dbo.BILRG_StokLedgerScope'))
BEGIN
    CREATE INDEX IX_BILRG_StokLedgerScope_ReconstructionStatus
        ON dbo.BILRG_StokLedgerScope (ReconstructionStatus, SynchronizationState);
END
GO
