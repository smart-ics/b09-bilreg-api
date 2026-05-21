CREATE TABLE BILRG_LabTestComponent (
    TestDefinitionId VARCHAR(7) NOT NULL CONSTRAINT DF_BILRG_LabTestComponent_TestDefinitionId DEFAULT(''),
    SequenceNo INT NOT NULL CONSTRAINT DF_BILRG_LabTestComponent_SequenceNo DEFAULT(0),
    ComponentId VARCHAR(7) NOT NULL CONSTRAINT DF_BILRG_LabTestComponent_ComponentId DEFAULT(''),
    ReferenceRangeOverride VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_LabTestComponent_ReferenceRangeOverride DEFAULT(''),
    RequiredFlagging BIT NOT NULL CONSTRAINT DF_BILRG_LabTestComponent_RequiredFlagging DEFAULT(0),
    IsMandatory BIT NOT NULL CONSTRAINT DF_BILRG_LabTestComponent_IsMandatory DEFAULT(0),

    CONSTRAINT PK_BILRG_LabTestComponent PRIMARY KEY CLUSTERED (TestDefinitionId, SequenceNo)
);
GO

CREATE INDEX IX_LabTestComponent_ComponentId ON BILRG_LabTestComponent(ComponentId);
GO
