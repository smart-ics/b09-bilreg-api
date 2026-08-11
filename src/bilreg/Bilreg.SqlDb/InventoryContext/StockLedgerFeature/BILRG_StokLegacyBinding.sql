-- BILRG_StokLegacyBinding.sql
-- Stock Ledger v2 — coexistence binding Mutasi↔Buku / Lokasi↔Stok.
-- Drop at cutover. ADR-STL-002: omit VodUser/VodDate.

IF OBJECT_ID('BILRG_StokLegacyBinding', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_StokLegacyBinding
    (
        BindingId      VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_StokLegacyBinding_BindingId DEFAULT(''),
        BindingKind    INT         NOT NULL CONSTRAINT DF_BILRG_StokLegacyBinding_BindingKind DEFAULT(0),
        StokMutasiId   VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_StokLegacyBinding_StokMutasiId DEFAULT(''),
        StokLokasiId   VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_StokLegacyBinding_StokLokasiId DEFAULT(''),
        LegacyBukuId   VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_StokLegacyBinding_LegacyBukuId DEFAULT(''),
        LegacyStokId   VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_StokLegacyBinding_LegacyStokId DEFAULT(''),
        TrsReffId      VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_StokLegacyBinding_TrsReffId DEFAULT(''),

        CrtUser        VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokLegacyBinding_CrtUser DEFAULT(''),
        CrtDate        DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokLegacyBinding_CrtDate DEFAULT('3000-01-01'),
        UpdUser        VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_StokLegacyBinding_UpdUser DEFAULT(''),
        UpdDate        DATETIME    NOT NULL CONSTRAINT DF_BILRG_StokLegacyBinding_UpdDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_StokLegacyBinding PRIMARY KEY CLUSTERED (BindingId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_BILRG_StokLegacyBinding_LegacyBukuId'
      AND object_id = OBJECT_ID('BILRG_StokLegacyBinding'))
BEGIN
    CREATE UNIQUE INDEX UX_BILRG_StokLegacyBinding_LegacyBukuId
        ON BILRG_StokLegacyBinding (LegacyBukuId)
        WHERE LegacyBukuId <> '';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_BILRG_StokLegacyBinding_StokMutasiId'
      AND object_id = OBJECT_ID('BILRG_StokLegacyBinding'))
BEGIN
    CREATE UNIQUE INDEX UX_BILRG_StokLegacyBinding_StokMutasiId
        ON BILRG_StokLegacyBinding (StokMutasiId)
        WHERE StokMutasiId <> '';
END
GO
