CREATE TABLE BILRG_LabOrderItem
(
    OrderId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_OrderId DEFAULT(''),
    ItemNo INT NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_ItemNo DEFAULT(0),

    TestDefinitionId VARCHAR(7) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_TestDefinitionId DEFAULT(''),
    LabTestCode VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_LabTestCode DEFAULT(''),
    LabTestName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_LabTestName DEFAULT(''),

    TarifId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_TarifId DEFAULT(''),
    TarifCode VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_TarifCode DEFAULT(''),
    TarifName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_TarifName DEFAULT(''),

    TubeType INT NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_TubeType DEFAULT(0),
    SpecimenType VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_SpecimenType DEFAULT(''),
    RequiredTubeCount INT NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_RequiredTubeCount DEFAULT(0),
    Urgency INT NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_Urgency DEFAULT(0),

    ServiceType INT NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_ServiceType DEFAULT(1),
    DisciplineId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_DisciplineId DEFAULT(''),
    DisciplineName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_DisciplineName DEFAULT(''),
    TestGroupId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_TestGroupId DEFAULT(''),
    TestGroupName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_TestGroupName DEFAULT(''),

    CONSTRAINT PK_BILRG_LabOrderItem PRIMARY KEY CLUSTERED (OrderId, ItemNo)
);
GO

CREATE INDEX IX_BILRG_LabOrderItem_Grouping ON BILRG_LabOrderItem(OrderId, DisciplineName, TestGroupName);
GO
