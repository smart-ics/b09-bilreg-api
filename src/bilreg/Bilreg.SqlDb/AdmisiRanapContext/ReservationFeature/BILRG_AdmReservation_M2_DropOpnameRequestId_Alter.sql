IF COL_LENGTH('BILRG_AdmReservation', 'OpnameRequestId') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_AdmReservation DROP CONSTRAINT DF_BILRG_AdmReservation_OpnameRequestId;
    ALTER TABLE BILRG_AdmReservation DROP COLUMN OpnameRequestId;
END
GO
