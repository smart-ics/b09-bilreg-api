-- BILRG_TerimaMutasiItem.sql
-- Table: BILRG_TerimaMutasiItem (Rincian Item Penerimaan Mutasi Barang)
-- Outcome: OC-INV-TERIMA-MUTASI

IF OBJECT_ID('BILRG_TerimaMutasiItem', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_TerimaMutasiItem
    (
        TerimaMutasiId  VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_TerimaMutasiId DEFAULT(''),
        NoUrut          INT             NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_NoUrut DEFAULT(0),
        BrgId           VARCHAR(13)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_BrgId DEFAULT(''),
        BrgName         VARCHAR(100)    NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_BrgName DEFAULT(''),
        BatchNo         VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_BatchNo DEFAULT(''),
        ExpiredDate     VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_ExpiredDate DEFAULT(''),
        QtyTerima       DECIMAL(18, 4)  NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_QtyTerima DEFAULT(0),
        SatuanId        VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_SatuanId DEFAULT(''),
        SatuanName      VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_SatuanName DEFAULT(''),

        CONSTRAINT PK_BILRG_TerimaMutasiItem PRIMARY KEY CLUSTERED (TerimaMutasiId, NoUrut)
    );
END
GO

IF OBJECT_ID('BILRG_TerimaMutasi', 'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_BILRG_TerimaMutasiItem_TerimaMutasi'
          AND parent_object_id = OBJECT_ID('BILRG_TerimaMutasiItem'))
BEGIN
    ALTER TABLE BILRG_TerimaMutasiItem
        ADD CONSTRAINT FK_BILRG_TerimaMutasiItem_TerimaMutasi
        FOREIGN KEY (TerimaMutasiId) REFERENCES BILRG_TerimaMutasi(TerimaMutasiId)
        ON DELETE CASCADE;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_TerimaMutasiItem_BrgId'
      AND object_id = OBJECT_ID('BILRG_TerimaMutasiItem'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_TerimaMutasiItem_BrgId
        ON BILRG_TerimaMutasiItem (BrgId);
END
GO
