IF COL_LENGTH('BILRG_AptQueueEntry', 'RegId') IS NULL
BEGIN
    ALTER TABLE BILRG_AptQueueEntry ADD
        RegId VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_RegId DEFAULT(''),
        PasienId VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_PasienId DEFAULT('');
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_AptQueueEntry_RegId' AND object_id = OBJECT_ID('BILRG_AptQueueEntry'))
BEGIN
    CREATE INDEX IX_BILRG_AptQueueEntry_RegId ON BILRG_AptQueueEntry(RegId, TakenAt);
END;
GO
