CREATE TABLE BILRG_LabOrderItemComponent
(
    OrderId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_LabOrderItemComponent_OrderId DEFAULT(''),
    ItemNo INT NOT NULL CONSTRAINT DF_BILRG_LabOrderItemComponent_ItemNo DEFAULT(0),
    ComponentNo INT NOT NULL CONSTRAINT DF_BILRG_LabOrderItemComponent_ComponentNo DEFAULT(0),

    ComponentId VARCHAR(7) NOT NULL CONSTRAINT DF_BILRG_LabOrderItemComponent_ComponentId DEFAULT(''),
    ComponentCode VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_LabOrderItemComponent_ComponentCode DEFAULT(''),
    ComponentName VARCHAR(120) NOT NULL CONSTRAINT DF_BILRG_LabOrderItemComponent_ComponentName DEFAULT(''),
    ResultType INT NOT NULL CONSTRAINT DF_BILRG_LabOrderItemComponent_ResultType DEFAULT(0),
    Unit VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_LabOrderItemComponent_Unit DEFAULT(''),
    ReferenceRangeText VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_LabOrderItemComponent_ReferenceRangeText DEFAULT(''),
    SequenceNo INT NOT NULL CONSTRAINT DF_BILRG_LabOrderItemComponent_SequenceNo DEFAULT(0),
    IsMandatory BIT NOT NULL CONSTRAINT DF_BILRG_LabOrderItemComponent_IsMandatory DEFAULT(0),

    CONSTRAINT PK_BILRG_LabOrderItemComponent PRIMARY KEY CLUSTERED (OrderId, ItemNo, ComponentNo)
)
GO
