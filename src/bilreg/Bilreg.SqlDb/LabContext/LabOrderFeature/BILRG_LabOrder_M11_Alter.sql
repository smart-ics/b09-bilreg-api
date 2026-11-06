-- BILRG_LabOrder_M11_Alter.sql
-- Migration M11: Clinical Context & Urgency for LabOrder and LabOrderItem

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_LabOrder') AND name = 'DokterPerujukId')
BEGIN
    ALTER TABLE BILRG_LabOrder ADD DokterPerujukId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_LabOrder_DokterPerujukId DEFAULT('');
    ALTER TABLE BILRG_LabOrder ADD DokterPerujukName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabOrder_DokterPerujukName DEFAULT('');
    ALTER TABLE BILRG_LabOrder ADD LayananPerujukId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabOrder_LayananPerujukId DEFAULT('');
    ALTER TABLE BILRG_LabOrder ADD LayananPerujukName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabOrder_LayananPerujukName DEFAULT('');
    ALTER TABLE BILRG_LabOrder ADD ClinicalNote VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_LabOrder_ClinicalNote DEFAULT('');
    ALTER TABLE BILRG_LabOrder ADD Urgency INT NOT NULL CONSTRAINT DF_BILRG_LabOrder_Urgency DEFAULT(0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_LabOrderItem') AND name = 'Urgency')
BEGIN
    ALTER TABLE BILRG_LabOrderItem ADD Urgency INT NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_Urgency DEFAULT(0);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_BILRG_LabOrder_Urgency' AND object_id = OBJECT_ID('BILRG_LabOrder'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_LabOrder_Urgency 
        ON BILRG_LabOrder(Urgency, LabOrderStatus, CrtDate);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_BILRG_LabOrder_Perujuk' AND object_id = OBJECT_ID('BILRG_LabOrder'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_LabOrder_Perujuk 
        ON BILRG_LabOrder(DokterPerujukId, LayananPerujukId) 
        WHERE DokterPerujukId <> '';
END;
GO
