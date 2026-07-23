IF COL_LENGTH('BILRG_AntrianEntry', 'Priority') IS NULL
BEGIN
    ALTER TABLE BILRG_AntrianEntry ADD
        Priority BIT NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_Priority DEFAULT(0),
        CreationReason INT NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_CreationReason DEFAULT(0),
        CallCount INT NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_CallCount DEFAULT(0),
        SourceAntrianId VARCHAR(26) NULL,
        SourceNoUrut INT NULL,
        WithdrawalReason VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_WithdrawalReason DEFAULT(''),
        WithdrawalUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_WithdrawalUserId DEFAULT(''),
        WithdrawnAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_WithdrawnAt DEFAULT('3000-01-01');

    ALTER TABLE BILRG_AntrianEntry ADD
        CONSTRAINT CK_BILRG_AntrianEntry_CreationReason CHECK (CreationReason IN (0, 1, 2)),
        CONSTRAINT CK_BILRG_AntrianEntry_CallCount CHECK (CallCount >= 0),
        CONSTRAINT CK_BILRG_AntrianEntry_SourcePair CHECK (
            (SourceAntrianId IS NULL AND SourceNoUrut IS NULL)
            OR (SourceAntrianId IS NOT NULL AND SourceNoUrut IS NOT NULL AND SourceNoUrut > 0));
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_AntrianEntry_OperationalWorklist'
    AND object_id = OBJECT_ID('BILRG_AntrianEntry'))
BEGIN
    CREATE INDEX IX_BILRG_AntrianEntry_OperationalWorklist
        ON BILRG_AntrianEntry(AntrianId, AntrianStatus, Priority DESC, CreatedAt, NoUrut)
        INCLUDE (CreationReason, CallCount, PasienTrackerId, ServedAt, DoneAt);
END;
