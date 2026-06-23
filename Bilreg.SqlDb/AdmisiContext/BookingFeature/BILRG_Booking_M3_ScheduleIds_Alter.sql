IF COL_LENGTH('BILRG_Booking', 'JadwalPraktekId') IS NULL
BEGIN
    ALTER TABLE BILRG_Booking
        ADD JadwalPraktekId       VARCHAR(7)  NULL,
            JadwalPraktekHarianId VARCHAR(12) NULL;
END
GO
