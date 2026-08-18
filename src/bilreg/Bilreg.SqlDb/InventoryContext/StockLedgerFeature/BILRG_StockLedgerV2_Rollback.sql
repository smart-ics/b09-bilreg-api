-- BILRG_StockLedgerV2_Rollback.sql
-- DESTRUCTIVE: drops Stock Ledger v2 tables and indexes (dev/test only).
-- Use only when no production data must be retained, or after full backup.
-- Reverse order: Binding -> Scope -> Mutasi -> Lokasi -> Batch

-- BILRG_StokLegacyBinding
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BILRG_StokLegacyBinding_StokMutasiId'
           AND object_id = OBJECT_ID('BILRG_StokLegacyBinding'))
    DROP INDEX UX_BILRG_StokLegacyBinding_StokMutasiId ON BILRG_StokLegacyBinding;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BILRG_StokLegacyBinding_LegacyBukuId'
           AND object_id = OBJECT_ID('BILRG_StokLegacyBinding'))
    DROP INDEX UX_BILRG_StokLegacyBinding_LegacyBukuId ON BILRG_StokLegacyBinding;
GO

IF OBJECT_ID('BILRG_StokLegacyBinding', 'U') IS NOT NULL
    DROP TABLE BILRG_StokLegacyBinding;
GO

-- BILRG_StokLegacyScope
IF OBJECT_ID('BILRG_StokLegacyScope', 'U') IS NOT NULL
    DROP TABLE BILRG_StokLegacyScope;
GO

-- BILRG_StokMutasi
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_StokMutasi_ReversesMutasiId'
           AND object_id = OBJECT_ID('BILRG_StokMutasi'))
    DROP INDEX IX_BILRG_StokMutasi_ReversesMutasiId ON BILRG_StokMutasi;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_StokMutasi_TrsReffId'
           AND object_id = OBJECT_ID('BILRG_StokMutasi'))
    DROP INDEX IX_BILRG_StokMutasi_TrsReffId ON BILRG_StokMutasi;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_StokMutasi_StokBatchId_TglMutasi'
           AND object_id = OBJECT_ID('BILRG_StokMutasi'))
    DROP INDEX IX_BILRG_StokMutasi_StokBatchId_TglMutasi ON BILRG_StokMutasi;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_StokMutasi_StokLokasiId_TglMutasi'
           AND object_id = OBJECT_ID('BILRG_StokMutasi'))
    DROP INDEX IX_BILRG_StokMutasi_StokLokasiId_TglMutasi ON BILRG_StokMutasi;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId'
           AND object_id = OBJECT_ID('BILRG_StokMutasi'))
    DROP INDEX UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId ON BILRG_StokMutasi;
GO

IF OBJECT_ID('BILRG_StokMutasi', 'U') IS NOT NULL
    DROP TABLE BILRG_StokMutasi;
GO

-- BILRG_StokLokasi
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_StokLokasi_FifoFefo'
           AND object_id = OBJECT_ID('BILRG_StokLokasi'))
    DROP INDEX IX_BILRG_StokLokasi_FifoFefo ON BILRG_StokLokasi;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BILRG_StokLokasi_StokBatchId_LayananId_TglEd'
           AND object_id = OBJECT_ID('BILRG_StokLokasi'))
    DROP INDEX UX_BILRG_StokLokasi_StokBatchId_LayananId_TglEd ON BILRG_StokLokasi;
GO

IF OBJECT_ID('BILRG_StokLokasi', 'U') IS NOT NULL
    DROP TABLE BILRG_StokLokasi;
GO

-- BILRG_StokBatch
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_StokBatch_BrgId_TglMasuk_BrgMasukReffId'
           AND object_id = OBJECT_ID('BILRG_StokBatch'))
    DROP INDEX IX_BILRG_StokBatch_BrgId_TglMasuk_BrgMasukReffId ON BILRG_StokBatch;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BILRG_StokBatch_BrgId_BrgMasukReffId'
           AND object_id = OBJECT_ID('BILRG_StokBatch'))
    DROP INDEX UX_BILRG_StokBatch_BrgId_BrgMasukReffId ON BILRG_StokBatch;
GO

IF OBJECT_ID('BILRG_StokBatch', 'U') IS NOT NULL
    DROP TABLE BILRG_StokBatch;
GO
