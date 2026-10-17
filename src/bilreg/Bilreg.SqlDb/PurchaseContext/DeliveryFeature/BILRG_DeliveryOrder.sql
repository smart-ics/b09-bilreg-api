-- BILRG_DeliveryOrder.sql
-- Purchasing — Delivery Order (DO) header.
-- Supplier delivery / goods receipt document; Stock Ledger Receipt Source (BrgMasukReffId).
-- Legacy reference: tb_trs_do (prefix "DM"). Full Crt/Upd/Vod audit per DATABASE.md (not exempted).

IF OBJECT_ID('BILRG_DeliveryOrder', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_DeliveryOrder
    (
        DeliveryOrderId     VARCHAR(12)   NOT NULL CONSTRAINT DF_BILRG_DeliveryOrder_DeliveryOrderId DEFAULT(''),
        DoNo                VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_DeliveryOrder_DoNo DEFAULT(''),
        SupplierId          VARCHAR(12)   NOT NULL CONSTRAINT DF_BILRG_DeliveryOrder_SupplierId DEFAULT(''),
        SupplierName        VARCHAR(100)  NOT NULL CONSTRAINT DF_BILRG_DeliveryOrder_SupplierName DEFAULT(''),
        PoReffId            VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_DeliveryOrder_PoReffId DEFAULT(''),
        DoDate              DATETIME      NOT NULL CONSTRAINT DF_BILRG_DeliveryOrder_DoDate DEFAULT('3000-01-01'),
        State               INT           NOT NULL CONSTRAINT DF_BILRG_DeliveryOrder_State DEFAULT(0),
        Notes               VARCHAR(500)  NOT NULL CONSTRAINT DF_BILRG_DeliveryOrder_Notes DEFAULT(''),

        CrtUser             VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_DeliveryOrder_CrtUser DEFAULT(''),
        CrtDate             DATETIME      NOT NULL CONSTRAINT DF_BILRG_DeliveryOrder_CrtDate DEFAULT('3000-01-01'),
        UpdUser             VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_DeliveryOrder_UpdUser DEFAULT(''),
        UpdDate             DATETIME      NOT NULL CONSTRAINT DF_BILRG_DeliveryOrder_UpdDate DEFAULT('3000-01-01'),
        VodUser             VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_DeliveryOrder_VodUser DEFAULT(''),
        VodDate             DATETIME      NOT NULL CONSTRAINT DF_BILRG_DeliveryOrder_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_DeliveryOrder PRIMARY KEY CLUSTERED (DeliveryOrderId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_BILRG_DeliveryOrder_DoNo'
      AND object_id = OBJECT_ID('BILRG_DeliveryOrder'))
BEGIN
    CREATE UNIQUE INDEX UX_BILRG_DeliveryOrder_DoNo
        ON BILRG_DeliveryOrder (DoNo);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_DeliveryOrder_DoDate'
      AND object_id = OBJECT_ID('BILRG_DeliveryOrder'))
BEGIN
    CREATE INDEX IX_BILRG_DeliveryOrder_DoDate
        ON BILRG_DeliveryOrder (DoDate);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_DeliveryOrder_SupplierId'
      AND object_id = OBJECT_ID('BILRG_DeliveryOrder'))
BEGIN
    CREATE INDEX IX_BILRG_DeliveryOrder_SupplierId
        ON BILRG_DeliveryOrder (SupplierId);
END
GO