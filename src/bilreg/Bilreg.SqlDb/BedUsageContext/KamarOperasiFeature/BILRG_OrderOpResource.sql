CREATE TABLE BILRG_OrderOpResource
(
    OrderOpResourceId VARCHAR(36)  NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_OrderOpResourceId DEFAULT(''),
    OrderOpId         VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_OrderOpId         DEFAULT(''),
    ScheduleOpId      VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_ScheduleOpId      DEFAULT(''),
    ResourceType      INT          NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_ResourceType      DEFAULT(1),
    Category          VARCHAR(30)  NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Category          DEFAULT(''),
    ItemName          VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_ItemName          DEFAULT(''),
    Jumlah            INT          NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Jumlah            DEFAULT(1),
    Satuan            VARCHAR(20)  NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Satuan            DEFAULT(''),
    Keterangan        VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Keterangan        DEFAULT(''),

    CrtUser           VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_CrtUser           DEFAULT(''),
    CrtDate           DATETIME     NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_CrtDate           DEFAULT('3000-01-01'),
    UpdUser           VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_UpdUser           DEFAULT(''),
    UpdDate           DATETIME     NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_UpdDate           DEFAULT('3000-01-01'),
    VodUser           VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_VodUser           DEFAULT(''),
    VodDate           DATETIME     NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_VodDate           DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_OrderOpResource PRIMARY KEY CLUSTERED(OrderOpResourceId)
);
GO

CREATE INDEX IX_BILRG_OrderOpResource_OrderOpId
    ON BILRG_OrderOpResource (OrderOpId, VodDate)
    INCLUDE (OrderOpResourceId, ScheduleOpId, ResourceType, Category, ItemName, Jumlah, Satuan)
    WITH (FILLFACTOR = 95);
GO

CREATE INDEX IX_BILRG_OrderOpResource_ScheduleOpId
    ON BILRG_OrderOpResource (ScheduleOpId, VodDate)
    INCLUDE (OrderOpResourceId, OrderOpId)
    WITH (FILLFACTOR = 95);
GO
