-- M9: Transform financial-clearance authority columns → billing release validation trace.
-- Preserves historical row values (0=NotChecked, 1=Clear, 2=Blocked; legacy Approved/Rejected map 1:1).
-- Safe to run once; ignore errors if columns already renamed.

IF COL_LENGTH('BILRG_LabOrder', 'FinancialClearance') IS NOT NULL
   AND COL_LENGTH('BILRG_LabOrder', 'LastBillingReleaseStatus') IS NULL
BEGIN
    EXEC sp_rename 'BILRG_LabOrder.FinancialClearance', 'LastBillingReleaseStatus', 'COLUMN';
END
GO

IF COL_LENGTH('BILRG_LabOrder', 'FinancialClearanceDate') IS NOT NULL
   AND COL_LENGTH('BILRG_LabOrder', 'LastBillingReleaseCheckAt') IS NULL
BEGIN
    EXEC sp_rename 'BILRG_LabOrder.FinancialClearanceDate', 'LastBillingReleaseCheckAt', 'COLUMN';
END
GO

IF COL_LENGTH('BILRG_LabOrder', 'FinancialClearanceUserId') IS NOT NULL
   AND COL_LENGTH('BILRG_LabOrder', 'LastBillingReleaseCheckUserId') IS NULL
BEGIN
    EXEC sp_rename 'BILRG_LabOrder.FinancialClearanceUserId', 'LastBillingReleaseCheckUserId', 'COLUMN';
END
GO

IF COL_LENGTH('BILRG_LabOrder', 'FinancialClearanceReason') IS NOT NULL
   AND COL_LENGTH('BILRG_LabOrder', 'LastBillingReleaseMessage') IS NULL
BEGIN
    EXEC sp_rename 'BILRG_LabOrder.FinancialClearanceReason', 'LastBillingReleaseMessage', 'COLUMN';
END
GO
