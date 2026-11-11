IF COL_LENGTH('BILRG_AdmAdmission', 'InpatientOrderId') IS NULL
BEGIN
    ALTER TABLE BILRG_AdmAdmission
        ADD InpatientOrderId VARCHAR(12) NOT NULL
            CONSTRAINT DF_BILRG_AdmAdmission_InpatientOrderId DEFAULT('-');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_bilrg_admadmission_inpatientorderid'
      AND object_id = OBJECT_ID('BILRG_AdmAdmission'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_bilrg_admadmission_inpatientorderid
        ON BILRG_AdmAdmission (InpatientOrderId, VodDate, RegId)
        WITH (FILLFACTOR = 90);
END
GO
