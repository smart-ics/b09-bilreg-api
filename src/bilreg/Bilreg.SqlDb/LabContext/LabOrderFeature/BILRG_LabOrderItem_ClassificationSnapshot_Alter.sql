-- BILRG_LabOrderItem_ClassificationSnapshot_Alter.sql
-- Migration: Add classification snapshot attributes to LabOrderItem

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_LabOrderItem') AND name = 'ServiceType')
BEGIN
    ALTER TABLE BILRG_LabOrderItem ADD ServiceType INT NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_ServiceType DEFAULT(1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_LabOrderItem') AND name = 'DisciplineId')
BEGIN
    ALTER TABLE BILRG_LabOrderItem ADD DisciplineId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_DisciplineId DEFAULT('');
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_LabOrderItem') AND name = 'DisciplineName')
BEGIN
    ALTER TABLE BILRG_LabOrderItem ADD DisciplineName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_DisciplineName DEFAULT('');
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_LabOrderItem') AND name = 'TestGroupId')
BEGIN
    ALTER TABLE BILRG_LabOrderItem ADD TestGroupId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_TestGroupId DEFAULT('');
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_LabOrderItem') AND name = 'TestGroupName')
BEGIN
    ALTER TABLE BILRG_LabOrderItem ADD TestGroupName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_TestGroupName DEFAULT('');
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_BILRG_LabOrderItem_Grouping' AND object_id = OBJECT_ID('BILRG_LabOrderItem'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_LabOrderItem_Grouping 
        ON BILRG_LabOrderItem(OrderId, DisciplineName, TestGroupName);
END;
GO
