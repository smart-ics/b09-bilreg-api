IF COL_LENGTH('dbo.FARIN_AntrianEntry', 'PasienTrackerId') IS NULL
BEGIN
    ALTER TABLE dbo.FARIN_AntrianEntry
        ADD PasienTrackerId VARCHAR(26) NOT NULL
            CONSTRAINT DF_FARIN_AntrianEntry_PasienTrackerId DEFAULT('-');
END
GO

IF COL_LENGTH('dbo.FARIN_AntrianEntry', 'ServedAt') IS NULL
BEGIN
    ALTER TABLE dbo.FARIN_AntrianEntry
        ADD ServedAt DATETIME NOT NULL
            CONSTRAINT DF_FARIN_AntrianEntry_ServedAt DEFAULT('3000-01-01');
END
GO
