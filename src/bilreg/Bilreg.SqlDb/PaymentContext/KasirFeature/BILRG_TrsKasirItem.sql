IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BILRG_TrsKasirItem' AND type = 'U')
BEGIN
    CREATE TABLE BILRG_TrsKasirItem (
        KasirItemId VARCHAR(36) NOT NULL CONSTRAINT DF_BILRG_TrsKasirItem_KasirItemId DEFAULT(''),
        TrsKasirId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_TrsKasirItem_TrsKasirId DEFAULT(''),
        NoUrut INT NOT NULL CONSTRAINT DF_BILRG_TrsKasirItem_NoUrut DEFAULT(1),
        BillId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_TrsKasirItem_BillId DEFAULT(''),
        RegId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_TrsKasirItem_RegId DEFAULT(''),
        LayananId VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_TrsKasirItem_LayananId DEFAULT(''),
        Keterangan VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_TrsKasirItem_Keterangan DEFAULT(''),
        NilaiBill DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TrsKasirItem_NilaiBill DEFAULT(0),
        NilaiBayar DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TrsKasirItem_NilaiBayar DEFAULT(0),
        crttgl VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_TrsKasirItem_crttgl DEFAULT(''),
        crtjam VARCHAR(8) NOT NULL CONSTRAINT DF_BILRG_TrsKasirItem_crtjam DEFAULT(''),
        crtusr VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_TrsKasirItem_crtusr DEFAULT(''),

        CONSTRAINT PK_BILRG_TrsKasirItem PRIMARY KEY CLUSTERED (KasirItemId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_TrsKasirItem_TrsKasirId' AND object_id = OBJECT_ID('BILRG_TrsKasirItem'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_TrsKasirItem_TrsKasirId
        ON BILRG_TrsKasirItem (TrsKasirId, NoUrut);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_TrsKasirItem_BillId' AND object_id = OBJECT_ID('BILRG_TrsKasirItem'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_TrsKasirItem_BillId
        ON BILRG_TrsKasirItem (BillId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_TrsKasirItem_RegId' AND object_id = OBJECT_ID('BILRG_TrsKasirItem'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_TrsKasirItem_RegId
        ON BILRG_TrsKasirItem (RegId);
END
GO
