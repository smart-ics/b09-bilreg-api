CREATE TABLE BILRG_AptSalesOrderItemComponent (
    SalesOrderId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AptSalesOrderItemComponent_SalesOrderId DEFAULT(''),
    ItemNo INT NOT NULL CONSTRAINT DF_BILRG_AptSalesOrderItemComponent_ItemNo DEFAULT(0),
    ComponentNo INT NOT NULL CONSTRAINT DF_BILRG_AptSalesOrderItemComponent_ComponentNo DEFAULT(0),
    BrgId VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_AptSalesOrderItemComponent_BrgId DEFAULT(''),
    BrgName VARCHAR(80) NOT NULL CONSTRAINT DF_BILRG_AptSalesOrderItemComponent_BrgName DEFAULT(''),
    Qty DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_AptSalesOrderItemComponent_Qty DEFAULT(0),
    CONSTRAINT PK_BILRG_AptSalesOrderItemComponent PRIMARY KEY CLUSTERED (SalesOrderId, ItemNo, ComponentNo)
);
GO
