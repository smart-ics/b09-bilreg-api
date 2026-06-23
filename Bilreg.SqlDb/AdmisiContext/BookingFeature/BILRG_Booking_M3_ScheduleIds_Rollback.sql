IF COL_LENGTH('BILRG_Booking', 'JadwalPraktekHarianId') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_Booking DROP COLUMN JadwalPraktekHarianId;
END
GO

IF COL_LENGTH('BILRG_Booking', 'JadwalPraktekId') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_Booking DROP COLUMN JadwalPraktekId;
END
GO
