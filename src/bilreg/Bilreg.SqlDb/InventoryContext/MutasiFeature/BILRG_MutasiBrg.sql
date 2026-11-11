-- BILRG_MutasiBrg.sql
-- Table: BILRG_MutasiBrg (Header Pengeluaran Mutasi Barang / Stock Dispatch)
-- Outcome: OC-INV-MUTASI

IF OBJECT_ID('BILRG_MutasiBrg', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_MutasiBrg
    (
        MutasiBrgId         VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrg_MutasiBrgId DEFAULT(''),
        MutasiBrgDate       DATETIME        NOT NULL CONSTRAINT DF_BILRG_MutasiBrg_MutasiBrgDate DEFAULT('3000-01-01'),
        State               INT             NOT NULL CONSTRAINT DF_BILRG_MutasiBrg_State DEFAULT(0),

        LayananAsalId       VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrg_LayananAsalId DEFAULT(''),
        LayananAsalName     VARCHAR(100)    NOT NULL CONSTRAINT DF_BILRG_MutasiBrg_LayananAsalName DEFAULT(''),
        LayananTujuanId     VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrg_LayananTujuanId DEFAULT(''),
        LayananTujuanName   VARCHAR(100)    NOT NULL CONSTRAINT DF_BILRG_MutasiBrg_LayananTujuanName DEFAULT(''),

        OrderMutasiId       VARCHAR(26)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrg_OrderMutasiId DEFAULT(''),
        DispatchNote        VARCHAR(255)    NOT NULL CONSTRAINT DF_BILRG_MutasiBrg_DispatchNote DEFAULT(''),
        IssuerUserId        VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrg_IssuerUserId DEFAULT(''),

        CrtDate             DATETIME        NOT NULL CONSTRAINT DF_BILRG_MutasiBrg_CrtDate DEFAULT('3000-01-01'),
        CrtUser             VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrg_CrtUser DEFAULT(''),
        UpdDate             DATETIME        NOT NULL CONSTRAINT DF_BILRG_MutasiBrg_UpdDate DEFAULT('3000-01-01'),
        UpdUser             VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrg_UpdUser DEFAULT(''),
        VodDate             DATETIME        NULL     CONSTRAINT DF_BILRG_MutasiBrg_VodDate DEFAULT(NULL),
        VodUser             VARCHAR(50)     NULL     CONSTRAINT DF_BILRG_MutasiBrg_VodUser DEFAULT(''),

        CONSTRAINT PK_BILRG_MutasiBrg PRIMARY KEY CLUSTERED (MutasiBrgId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_MutasiBrg_InTransit'
      AND object_id = OBJECT_ID('BILRG_MutasiBrg'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_MutasiBrg_InTransit
        ON BILRG_MutasiBrg (State, LayananTujuanId)
        INCLUDE (MutasiBrgDate, LayananAsalId, LayananAsalName);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_MutasiBrg_Periode'
      AND object_id = OBJECT_ID('BILRG_MutasiBrg'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_MutasiBrg_Periode
        ON BILRG_MutasiBrg (MutasiBrgDate, LayananAsalId);
END
GO
