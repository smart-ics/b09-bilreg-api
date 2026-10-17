-- BILRG_DeliveryOrderItem.sql
-- Purchasing — Delivery Order (DO) detail lines.
-- Composite PK (DeliveryOrderId, ItemNo) per DATABASE.md §7/§17. QtyReceived accumulates for partial receipt.

IF OBJECT_ID('BILRG_DeliveryOrderItem', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_DeliveryOrderItem
    (
        DeliveryOrderId     VARCHAR(12)   NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_DeliveryOrderId DEFAULT(''),
        ItemNo              INT           NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_ItemNo DEFAULT(0),
        BrgId               VARCHAR(13)   NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_BrgId DEFAULT(''),
        LayananId           VARCHAR(5)    NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_LayananId DEFAULT(''),
        QtyOrder            DECIMAL(18,0) NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_QtyOrder DEFAULT(0),
        QtyReceived         DECIMAL(18,0) NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_QtyReceived DEFAULT(0),
        SatuanId            VARCHAR(13)   NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_SatuanId DEFAULT(''),
        Harga               DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_Harga DEFAULT(0),
        Diskon              DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_Diskon DEFAULT(0),
        Tax                 DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_Tax DEFAULT(0),
        Hpp                 DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_Hpp DEFAULT(0),
        TglEd               DATETIME      NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_TglEd DEFAULT('3000-01-01'),
        NoBatch             VARCHAR(15)   NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_NoBatch DEFAULT(''),
        State               INT           NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_State DEFAULT(0),

        CrtUser             VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_CrtUser DEFAULT(''),
        CrtDate             DATETIME      NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_CrtDate DEFAULT('3000-01-01'),
        UpdUser             VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_UpdUser DEFAULT(''),
        UpdDate             DATETIME      NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_UpdDate DEFAULT('3000-01-01'),
        VodUser             VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_VodUser DEFAULT(''),
        VodDate             DATETIME      NOT NULL CONSTRAINT DF_BILRG_DeliveryOrderItem_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_DeliveryOrderItem PRIMARY KEY CLUSTERED (DeliveryOrderId, ItemNo)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_DeliveryOrderItem_BrgId'
      AND object_id = OBJECT_ID('BILRG_DeliveryOrderItem'))
BEGIN
    CREATE INDEX IX_BILRG_DeliveryOrderItem_BrgId
        ON BILRG_DeliveryOrderItem (BrgId, LayananId);
END
GO