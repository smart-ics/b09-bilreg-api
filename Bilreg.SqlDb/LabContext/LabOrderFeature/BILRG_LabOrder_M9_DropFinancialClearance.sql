-- Migration: remove LWF financial clearance columns (authority moved to BIL realtime validation).
-- Run after deploying code that uses BILRG_LabBillingReleaseCheck.

IF COL_LENGTH('BILRG_LabOrder', 'FinancialClearance') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_LabOrder DROP CONSTRAINT DF_BILRG_LabOrder_FinancialClearance;
    ALTER TABLE BILRG_LabOrder DROP COLUMN FinancialClearance;
END
GO

IF COL_LENGTH('BILRG_LabOrder', 'FinancialClearanceDate') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_LabOrder DROP CONSTRAINT DF_BILRG_LabOrder_FinancialClearanceDate;
    ALTER TABLE BILRG_LabOrder DROP COLUMN FinancialClearanceDate;
END
GO

IF COL_LENGTH('BILRG_LabOrder', 'FinancialClearanceUserId') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_LabOrder DROP CONSTRAINT DF_BILRG_LabOrder_FinancialClearanceUserId;
    ALTER TABLE BILRG_LabOrder DROP COLUMN FinancialClearanceUserId;
END
GO

IF COL_LENGTH('BILRG_LabOrder', 'FinancialClearanceReason') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_LabOrder DROP CONSTRAINT DF_BILRG_LabOrder_FinancialClearanceReason;
    ALTER TABLE BILRG_LabOrder DROP COLUMN FinancialClearanceReason;
END
GO
