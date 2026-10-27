IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_bilrg_admadmission_reservationid'
      AND object_id = OBJECT_ID('BILRG_AdmAdmission'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_bilrg_admadmission_reservationid
        ON BILRG_AdmAdmission (ReservationId, VodDate, RegId)
        WITH (FILLFACTOR = 90);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_bilrg_admadmission_opnamerequestid'
      AND object_id = OBJECT_ID('BILRG_AdmAdmission'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_bilrg_admadmission_opnamerequestid
        ON BILRG_AdmAdmission (OpnameRequestId, VodDate, RegId)
        WITH (FILLFACTOR = 90);
END
GO
