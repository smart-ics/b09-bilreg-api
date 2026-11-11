-- BILRG_MutasiBrgItem.sql
-- Table: BILRG_MutasiBrgItem (Rincian Item Pengeluaran Mutasi Barang)
-- Outcome: OC-INV-MUTASI

IF OBJECT_ID('BILRG_MutasiBrgItem', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_MutasiBrgItem
    (
        MutasiBrgId     VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_MutasiBrgId DEFAULT(''),
        NoUrut          INT             NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_NoUrut DEFAULT(0),
        BrgId           VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_BrgId DEFAULT(''),
        BrgName         VARCHAR(100)    NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_BrgName DEFAULT(''),
        BatchNo         VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_BatchNo DEFAULT(''),
        ExpiredDate     VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_ExpiredDate DEFAULT(''),
        QtyKirim        DECIMAL(18, 4)  NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_QtyKirim DEFAULT(0),
        SatuanId        VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_SatuanId DEFAULT(''),
        SatuanName      VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_SatuanName DEFAULT(''),

        CONSTRAINT PK_BILRG_MutasiBrgItem PRIMARY KEY CLUSTERED (MutasiBrgId, NoUrut)
    );
END
GO

IF OBJECT_ID('BILRG_MutasiBrg', 'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_BILRG_MutasiBrgItem_MutasiBrg'
          AND parent_object_id = OBJECT_ID('BILRG_MutasiBrgItem'))
BEGIN
    ALTER TABLE BILRG_MutasiBrgItem
        ADD CONSTRAINT FK_BILRG_MutasiBrgItem_MutasiBrg
        FOREIGN KEY (MutasiBrgId) REFERENCES BILRG_MutasiBrg(MutasiBrgId)
        ON DELETE CASCADE;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_MutasiBrgItem_BrgId'
      AND object_id = OBJECT_ID('BILRG_MutasiBrgItem'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_MutasiBrgItem_BrgId
        ON BILRG_MutasiBrgItem (BrgId);
END
GO
