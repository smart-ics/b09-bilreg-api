IF COL_LENGTH('BILRG_AptQueueMapping', 'PasienHadir') IS NULL
BEGIN
    ALTER TABLE BILRG_AptQueueMapping ADD
        PasienHadir BIT NOT NULL CONSTRAINT DF_BILRG_AptQueueMapping_PasienHadir DEFAULT(0),
        PasienHadirAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AptQueueMapping_PasienHadirAt DEFAULT('3000-01-01');
END;
GO
