-- Stock Ledger Phase 1 / P1-S5
-- Disposable-DB rollback only. Never run against HOSPITAL_HPL / production.
-- Drops additive Stock Ledger tables created by P1-S5 scripts.
-- Does not touch tb_stok, tb_buku, or FARIN_* tables.

IF OBJECT_ID('dbo.BILRG_StokSourceIdempotency', 'U') IS NOT NULL
    DROP TABLE dbo.BILRG_StokSourceIdempotency;
GO

IF OBJECT_ID('dbo.BILRG_StokLedgerScope', 'U') IS NOT NULL
    DROP TABLE dbo.BILRG_StokLedgerScope;
GO

IF OBJECT_ID('dbo.BILRG_StokPosition', 'U') IS NOT NULL
    DROP TABLE dbo.BILRG_StokPosition;
GO

IF OBJECT_ID('dbo.BILRG_StokLayer', 'U') IS NOT NULL
    DROP TABLE dbo.BILRG_StokLayer;
GO

IF OBJECT_ID('dbo.BILRG_StokMovementLine', 'U') IS NOT NULL
    DROP TABLE dbo.BILRG_StokMovementLine;
GO

IF OBJECT_ID('dbo.BILRG_StokMovement', 'U') IS NOT NULL
    DROP TABLE dbo.BILRG_StokMovement;
GO
