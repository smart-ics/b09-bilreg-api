-- One-time migration for existing BILRG_LabOrder (M9 — cancel / terminate).
-- Safe to run once; ignore errors if columns already exist.

IF COL_LENGTH('BILRG_LabOrder', 'CancelledReason') IS NULL
BEGIN
    ALTER TABLE BILRG_LabOrder ADD
        CancelledReason VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_LabOrder_CancelledReason DEFAULT(''),
        CancelledDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabOrder_CancelledDate DEFAULT('3000-01-01'),
        CancelledUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabOrder_CancelledUserId DEFAULT(''),
        TerminationReason VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_LabOrder_TerminationReason DEFAULT(''),
        TerminationDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabOrder_TerminationDate DEFAULT('3000-01-01'),
        TerminationUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabOrder_TerminationUserId DEFAULT('');
END
GO
