CREATE TABLE BILRG_LabComponentMaster (
    ComponentId VARCHAR(7) NOT NULL CONSTRAINT DF_BILRG_LabComponentMaster_ComponentId DEFAULT(''),
    LoincCode VARCHAR(20) NULL,
    ComponentCode VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_LabComponentMaster_ComponentCode DEFAULT(''),
    ComponentName VARCHAR(120) NOT NULL CONSTRAINT DF_BILRG_LabComponentMaster_ComponentName DEFAULT(''),
    ComponentNameIndonesia VARCHAR(120) NULL,
    ResultType INT NOT NULL CONSTRAINT DF_BILRG_LabComponentMaster_ResultType DEFAULT(0),
    DefaultUnit VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_LabComponentMaster_DefaultUnit DEFAULT(''),
    IsSystem BIT NOT NULL CONSTRAINT DF_BILRG_LabComponentMaster_IsSystem DEFAULT(0),
    IsActive BIT NOT NULL CONSTRAINT DF_BILRG_LabComponentMaster_IsActive DEFAULT(0),

    CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabComponentMaster_CrtUser DEFAULT(''),
    CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabComponentMaster_CrtDate DEFAULT('3000-01-01'),
    UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabComponentMaster_UpdUser DEFAULT(''),
    UpdDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabComponentMaster_UpdDate DEFAULT('3000-01-01'),
    VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabComponentMaster_VodUser DEFAULT(''),
    VodDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabComponentMaster_VodDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_LabComponentMaster PRIMARY KEY CLUSTERED (ComponentId)
);
GO

CREATE INDEX IX_LabComponentMaster_Code ON BILRG_LabComponentMaster(ComponentCode);
GO

CREATE INDEX IX_LabComponentMaster_Active ON BILRG_LabComponentMaster(IsActive, ComponentCode, ComponentName);
GO
