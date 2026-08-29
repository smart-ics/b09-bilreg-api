CREATE TABLE BILRG_AptDispensingItem (
    DispensingId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AptDispensingItem_DispensingId DEFAULT(''),
    ItemNo INT NOT NULL CONSTRAINT DF_BILRG_AptDispensingItem_ItemNo DEFAULT(0),
    SalesOrderItemNo INT NOT NULL CONSTRAINT DF_BILRG_AptDispensingItem_SalesOrderItemNo DEFAULT(0),
    BrgId VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_AptDispensingItem_BrgId DEFAULT(''),
    Qty DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_AptDispensingItem_Qty DEFAULT(0),
    ReserveMutasiReff VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AptDispensingItem_ReserveMutasiReff DEFAULT(''),
    RemoveStockMutasiReff VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AptDispensingItem_RemoveStockMutasiReff DEFAULT(''),
    ReturnMutasiReff VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AptDispensingItem_ReturnMutasiReff DEFAULT(''),
    ItemOutcome INT NOT NULL CONSTRAINT DF_BILRG_AptDispensingItem_ItemOutcome DEFAULT(0),
    CONSTRAINT PK_BILRG_AptDispensingItem PRIMARY KEY CLUSTERED (DispensingId, ItemNo)
);
GO
