-- Phase 3: EmrOrderId correlation for EMR integration.

IF COL_LENGTH('BILRG_LabOrder', 'EmrOrderId') IS NULL
BEGIN
    ALTER TABLE BILRG_LabOrder ADD
        EmrOrderId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabOrder_EmrOrderId DEFAULT('');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_LabOrder_EmrOrderId' AND object_id = OBJECT_ID('BILRG_LabOrder'))
BEGIN
    CREATE INDEX IX_BILRG_LabOrder_EmrOrderId ON BILRG_LabOrder(EmrOrderId)
        WHERE EmrOrderId <> '';
END
GO
