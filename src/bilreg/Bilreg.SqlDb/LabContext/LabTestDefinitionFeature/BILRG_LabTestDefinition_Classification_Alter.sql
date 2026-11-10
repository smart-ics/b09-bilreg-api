-- BILRG_LabTestDefinition_Classification_Alter.sql
-- Migration: Add Laboratory ServiceType, DisciplineId, TestGroupId classification attributes

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_LabTestDefinition') AND name = 'ServiceType')
BEGIN
    ALTER TABLE BILRG_LabTestDefinition ADD ServiceType INT NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_ServiceType DEFAULT(1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_LabTestDefinition') AND name = 'DisciplineId')
BEGIN
    ALTER TABLE BILRG_LabTestDefinition ADD DisciplineId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_DisciplineId DEFAULT('');
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_LabTestDefinition') AND name = 'TestGroupId')
BEGIN
    ALTER TABLE BILRG_LabTestDefinition ADD TestGroupId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_TestGroupId DEFAULT('');
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_BILRG_LabTestDefinition_ServiceDiscipline' AND object_id = OBJECT_ID('BILRG_LabTestDefinition'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_LabTestDefinition_ServiceDiscipline 
        ON BILRG_LabTestDefinition(ServiceType, DisciplineId, TestGroupId);
END;
GO
