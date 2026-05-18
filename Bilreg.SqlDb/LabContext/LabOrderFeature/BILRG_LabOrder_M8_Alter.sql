-- One-time migration for existing BILRG_LabOrder (M8 — financial clearance + release).
-- Safe to run once; ignore errors if columns already exist.

IF COL_LENGTH('BILRG_LabOrder', 'FinancialClearanceDate') IS NULL
BEGIN
    ALTER TABLE BILRG_LabOrder ADD
        FinancialClearanceDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabOrder_FinancialClearanceDate DEFAULT('3000-01-01'),
        FinancialClearanceUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabOrder_FinancialClearanceUserId DEFAULT(''),
        FinancialClearanceReason VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_LabOrder_FinancialClearanceReason DEFAULT(''),
        ReleasedDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabOrder_ReleasedDate DEFAULT('3000-01-01'),
        ReleasedUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabOrder_ReleasedUserId DEFAULT(''),
        ReleaseNote VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_LabOrder_ReleaseNote DEFAULT('');
END
GO
