-- BILRG_TerimaMutasi.sql
-- Table: BILRG_TerimaMutasi (Header Penerimaan Mutasi Barang / Stock Receipt)
-- Outcome: OC-INV-TERIMA-MUTASI

IF OBJECT_ID('BILRG_TerimaMutasi', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_TerimaMutasi
    (
        TerimaMutasiId      VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_TerimaMutasiId DEFAULT(''),
        TerimaMutasiDate    DATETIME        NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_TerimaMutasiDate DEFAULT('3000-01-01'),
        State               INT             NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_State DEFAULT(0),

        MutasiBrgId         VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_MutasiBrgId DEFAULT(''),
        OrderMutasiId       VARCHAR(26)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_OrderMutasiId DEFAULT(''),

        LayananAsalId       VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_LayananAsalId DEFAULT(''),
        LayananAsalName     VARCHAR(100)    NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_LayananAsalName DEFAULT(''),
        LayananTujuanId     VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_LayananTujuanId DEFAULT(''),
        LayananTujuanName   VARCHAR(100)    NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_LayananTujuanName DEFAULT(''),

        ReceiverUserId      VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_ReceiverUserId DEFAULT(''),
        Note                VARCHAR(255)    NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_Note DEFAULT(''),
        RejectionReason     VARCHAR(255)    NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_RejectionReason DEFAULT(''),

        CrtDate             DATETIME        NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_CrtDate DEFAULT('3000-01-01'),
        CrtUser             VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_CrtUser DEFAULT(''),
        UpdDate             DATETIME        NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_UpdDate DEFAULT('3000-01-01'),
        UpdUser             VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_UpdUser DEFAULT(''),
        VodDate             DATETIME        NULL     CONSTRAINT DF_BILRG_TerimaMutasi_VodDate DEFAULT(NULL),
        VodUser             VARCHAR(50)     NULL     CONSTRAINT DF_BILRG_TerimaMutasi_VodUser DEFAULT(''),

        CONSTRAINT PK_BILRG_TerimaMutasi PRIMARY KEY CLUSTERED (TerimaMutasiId)
    );
END
GO

IF OBJECT_ID('BILRG_MutasiBrg', 'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_BILRG_TerimaMutasi_MutasiBrg'
          AND parent_object_id = OBJECT_ID('BILRG_TerimaMutasi'))
BEGIN
    ALTER TABLE BILRG_TerimaMutasi
        ADD CONSTRAINT FK_BILRG_TerimaMutasi_MutasiBrg
        FOREIGN KEY (MutasiBrgId) REFERENCES BILRG_MutasiBrg(MutasiBrgId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_BILRG_TerimaMutasi_MutasiBrgId'
      AND object_id = OBJECT_ID('BILRG_TerimaMutasi'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_BILRG_TerimaMutasi_MutasiBrgId
        ON BILRG_TerimaMutasi (MutasiBrgId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_TerimaMutasi_Periode'
      AND object_id = OBJECT_ID('BILRG_TerimaMutasi'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_TerimaMutasi_Periode
        ON BILRG_TerimaMutasi (TerimaMutasiDate, LayananTujuanId);
END
GO
