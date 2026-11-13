CREATE TABLE BILRG_LabTestDefinition (
    TestDefinitionId VARCHAR(7) NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_TestDefinitionId DEFAULT(''),
    TarifId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_TarifId DEFAULT(''),
    TarifCode VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_TarifCode DEFAULT(''),
    TarifName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_TarifName DEFAULT(''),
    LabTestCode VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_LabTestCode DEFAULT(''),
    LabTestName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_LabTestName DEFAULT(''),
    SpecimenType VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_SpecimenType DEFAULT(''),
    VacutainerType INT NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_VacutainerType DEFAULT(0),
    IsActive BIT NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_IsActive DEFAULT(0),
    ServiceType INT NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_ServiceType DEFAULT(1),
    DisciplineId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_DisciplineId DEFAULT(''),
    TestGroupId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_TestGroupId DEFAULT(''),

    CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_CrtUser DEFAULT(''),
    CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_CrtDate DEFAULT('3000-01-01'),
    UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_UpdUser DEFAULT(''),
    UpdDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_UpdDate DEFAULT('3000-01-01'),
    VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_VodUser DEFAULT(''),
    VodDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_VodDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_LabTestDefinition PRIMARY KEY CLUSTERED (TestDefinitionId)
);
GO

CREATE INDEX IX_LabTestDefinition_TarifId ON BILRG_LabTestDefinition(TarifId, IsActive);
GO

CREATE INDEX IX_LabTestDefinition_Active ON BILRG_LabTestDefinition(IsActive, LabTestCode, LabTestName);
GO

CREATE INDEX IX_BILRG_LabTestDefinition_ServiceDiscipline ON BILRG_LabTestDefinition(ServiceType, DisciplineId, TestGroupId);
GO
