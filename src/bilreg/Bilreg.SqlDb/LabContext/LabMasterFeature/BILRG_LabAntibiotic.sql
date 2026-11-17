IF OBJECT_ID('BILRG_LabAntibiotic', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_LabAntibiotic (
        AntibioticId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabAntibiotic_AntibioticId DEFAULT(''),
        AntibioticName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabAntibiotic_AntibioticName DEFAULT(''),
        DiskConcentration VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_LabAntibiotic_DiskConcentration DEFAULT(''),
        ResistenRange VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_LabAntibiotic_ResistenRange DEFAULT(''),
        IntermediateRange VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_LabAntibiotic_IntermediateRange DEFAULT(''),
        SensitifRange VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_LabAntibiotic_SensitifRange DEFAULT(''),
        DisplayOrder INT NOT NULL CONSTRAINT DF_BILRG_LabAntibiotic_DisplayOrder DEFAULT(0),
        CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabAntibiotic_CrtUser DEFAULT(''),
        CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabAntibiotic_CrtDate DEFAULT('3000-01-01'),
        UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabAntibiotic_UpdUser DEFAULT(''),
        UpdDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabAntibiotic_UpdDate DEFAULT('3000-01-01'),
        VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabAntibiotic_VodUser DEFAULT(''),
        VodDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabAntibiotic_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_LabAntibiotic PRIMARY KEY CLUSTERED (AntibioticId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_BILRG_LabAntibiotic_Name' AND object_id = OBJECT_ID('BILRG_LabAntibiotic'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_LabAntibiotic_Name ON BILRG_LabAntibiotic(AntibioticName);
END
GO
