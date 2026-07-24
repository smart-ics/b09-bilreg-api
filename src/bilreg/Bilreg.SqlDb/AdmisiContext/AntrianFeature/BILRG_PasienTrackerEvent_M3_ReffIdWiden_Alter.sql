-- Widen ReffId to hold Queue Evidence Reference ({AntrianId}/No.{NoUrut}).
IF COL_LENGTH('dbo.BILRG_PasienTrackerEvent', 'ReffId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.BILRG_PasienTrackerEvent
        ALTER COLUMN ReffId VARCHAR(40) NOT NULL;
END
GO
