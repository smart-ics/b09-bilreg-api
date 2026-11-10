-- BILRG_LabOrder_ServiceType_Alter.sql
-- Migration: Add ServiceType classification to LabOrder

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_LabOrder') AND name = 'ServiceType')
BEGIN
    ALTER TABLE BILRG_LabOrder ADD ServiceType INT NOT NULL CONSTRAINT DF_BILRG_LabOrder_ServiceType DEFAULT(1);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_BILRG_LabOrder_ServiceType' AND object_id = OBJECT_ID('BILRG_LabOrder'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_LabOrder_ServiceType 
        ON BILRG_LabOrder(ServiceType, CrtDate);
END;
GO
