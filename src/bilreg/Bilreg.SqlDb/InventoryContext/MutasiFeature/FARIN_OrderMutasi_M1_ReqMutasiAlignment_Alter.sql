-- Migration: FARIN_OrderMutasi_M1_ReqMutasiAlignment_Alter.sql
-- Alignment for OC-INV-REQ-MUTASI (Priority, OrderNote(200), StokCurrent, QtyTerpenuhi, Indexes)

-- 1. Header Table: FARIN_OrderMutasi
IF OBJECT_ID('FARIN_OrderMutasi', 'U') IS NULL
BEGIN
    CREATE TABLE FARIN_OrderMutasi(
        OrderMutasiId VARCHAR(26) NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_OrderMutasiId DEFAULT(''),
        OrderMutasiDate DATETIME NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_OrderMutasiDate DEFAULT('3000-01-01'),
        State INT NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_State DEFAULT(0),
        Priority INT NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_Priority DEFAULT(0),

        LayananOrderId VARCHAR(5) NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_LayananOrderId DEFAULT(''),
        LayananOrderName VARCHAR(50) NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_LayananOrderName DEFAULT(''),
        LayananTujuanId VARCHAR(5) NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_LayananTujuanId DEFAULT(''),
        LayananTujuanName VARCHAR(50) NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_LayananTujuanName DEFAULT(''),

        ApprovalUserId VARCHAR(50) NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_ApprovalUserId DEFAULT(''),
        ApprovalDate DATETIME NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_ApprovalDate DEFAULT('3000-01-01'),
        RejectionUserId VARCHAR(50) NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_RejectionUserId DEFAULT(''),
        RejectionDate DATETIME NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_RejectionDate DEFAULT('3000-01-01'),
        OrderNote VARCHAR(200) NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_OrderNote DEFAULT(''),

        CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_CrtUser DEFAULT(''),
        CrtDate DATETIME NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_CrtDate DEFAULT('3000-01-01'),
        UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_UpdUser DEFAULT(''),
        UpdDate DATETIME NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_UpdDate DEFAULT('3000-01-01'),
        VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_VodUser DEFAULT(''),
        VodDate DATETIME NOT NULL CONSTRAINT DF_FARIN_OrderMutasi_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_FARIN_OrderMutasi PRIMARY KEY CLUSTERED(OrderMutasiId)
    );
END
ELSE
BEGIN
    IF COL_LENGTH('FARIN_OrderMutasi', 'Priority') IS NULL
    BEGIN
        ALTER TABLE FARIN_OrderMutasi
            ADD Priority INT NOT NULL
                CONSTRAINT DF_FARIN_OrderMutasi_Priority DEFAULT(0);
    END

    -- Widen OrderNote from VARCHAR(50) to VARCHAR(200)
    ALTER TABLE FARIN_OrderMutasi
        ALTER COLUMN OrderNote VARCHAR(200) NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_FARIN_OrderMutasi_OrderMutasiDate' 
      AND object_id = OBJECT_ID('FARIN_OrderMutasi')
)
BEGIN
    CREATE INDEX IX_FARIN_OrderMutasi_OrderMutasiDate
        ON FARIN_OrderMutasi (OrderMutasiDate, OrderMutasiId)
        WITH (FILLFACTOR = 95);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_FARIN_OrderMutasi_LayananOrder' 
      AND object_id = OBJECT_ID('FARIN_OrderMutasi')
)
BEGIN
    CREATE INDEX IX_FARIN_OrderMutasi_LayananOrder
        ON FARIN_OrderMutasi (LayananOrderId, OrderMutasiDate)
        WITH (FILLFACTOR = 95);
END
GO

-- 2. Detail Table: FARIN_OrderMutasiItem
IF OBJECT_ID('FARIN_OrderMutasiItem', 'U') IS NULL
BEGIN
    CREATE TABLE FARIN_OrderMutasiItem(
        OrderMutasiId VARCHAR(26) NOT NULL CONSTRAINT DF_FARIN_OrderMutasiItem_OrderMutasiId DEFAULT(''),
        NoUrut INT NOT NULL CONSTRAINT DF_FARIN_OrderMutasiItem_NoUrut DEFAULT(0),
        BrgId VARCHAR(13) NOT NULL CONSTRAINT DF_FARIN_OrderMutasiItem_BrgId DEFAULT(''),
        BrgName VARCHAR(50) NOT NULL CONSTRAINT DF_FARIN_OrderMutasiItem_BrgName DEFAULT(''),
        StokCurrent DECIMAL(18,2) NOT NULL CONSTRAINT DF_FARIN_OrderMutasiItem_StokCurrent DEFAULT(0),
        Qty DECIMAL(18,2) NOT NULL CONSTRAINT DF_FARIN_OrderMutasiItem_Qty DEFAULT(0),
        QtyTerpenuhi DECIMAL(18,2) NOT NULL CONSTRAINT DF_FARIN_OrderMutasiItem_QtyTerpenuhi DEFAULT(0),
        SatuanId VARCHAR(13) NOT NULL CONSTRAINT DF_FARIN_OrderMutasiItem_SatuanId DEFAULT(''),
        SatuanName VARCHAR(50) NOT NULL CONSTRAINT DF_FARIN_OrderMutasiItem_SatuanName DEFAULT(''),

        CONSTRAINT PK_FARIN_OrderMutasiItem PRIMARY KEY CLUSTERED(OrderMutasiId, NoUrut)
    );
END
ELSE
BEGIN
    IF COL_LENGTH('FARIN_OrderMutasiItem', 'StokCurrent') IS NULL
    BEGIN
        ALTER TABLE FARIN_OrderMutasiItem
            ADD StokCurrent DECIMAL(18,2) NOT NULL
                CONSTRAINT DF_FARIN_OrderMutasiItem_StokCurrent DEFAULT(0);
    END

    IF COL_LENGTH('FARIN_OrderMutasiItem', 'QtyTerpenuhi') IS NULL
    BEGIN
        ALTER TABLE FARIN_OrderMutasiItem
            ADD QtyTerpenuhi DECIMAL(18,2) NOT NULL
                CONSTRAINT DF_FARIN_OrderMutasiItem_QtyTerpenuhi DEFAULT(0);
    END

    ALTER TABLE FARIN_OrderMutasiItem
        ALTER COLUMN Qty DECIMAL(18,2) NOT NULL;
END
GO
