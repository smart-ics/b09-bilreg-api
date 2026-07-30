CREATE TABLE BILRG_AntrianEntry(
    AntrianId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_AntrianId DEFAULT(''),
    NoUrut INT NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_NoUrut DEFAULT(0),
    PersonName VARCHAR(40) NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_PersonName DEFAULT(''),
    PasienTrackerId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_PasienTrackerId DEFAULT(''),
    AntrianStatus INT NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_AntrianStatus DEFAULT(0),
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_CreatedAt DEFAULT('3000-01-01'),
    ServedAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_ServedAt DEFAULT('3000-01-01'),
    DoneAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_DoneAt DEFAULT('3000-01-01'),
    ReffId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_ReffId DEFAULT(''),
    ReffDesc VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_ReffDesc DEFAULT(''),
    Priority BIT NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_Priority DEFAULT(0),
    CreationReason INT NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_CreationReason DEFAULT(0),
    CallCount INT NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_CallCount DEFAULT(0),
    SourceAntrianId VARCHAR(26) NULL,
    SourceNoUrut INT NULL,
    WithdrawalReason VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_WithdrawalReason DEFAULT(''),
    WithdrawalUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_WithdrawalUserId DEFAULT(''),
    WithdrawnAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_WithdrawnAt DEFAULT('3000-01-01'),
    CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_CrtUser DEFAULT(''),
    CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_CrtDate DEFAULT('3000-01-01'),
    UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_UpdUser DEFAULT(''),
    UpdDate DATETIME NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_UpdDate DEFAULT('3000-01-01'),
    VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_VodUser DEFAULT(''),
    VodDate DATETIME NOT NULL CONSTRAINT DF_BILRG_AntrianEntry_VodDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_AntrianEntry PRIMARY KEY CLUSTERED (AntrianId, NoUrut),
    CONSTRAINT CK_BILRG_AntrianEntry_CreationReason CHECK (CreationReason IN (0,1,2)),
    CONSTRAINT CK_BILRG_AntrianEntry_CallCount CHECK (CallCount >= 0),
    CONSTRAINT CK_BILRG_AntrianEntry_SourcePair CHECK (
        (SourceAntrianId IS NULL AND SourceNoUrut IS NULL)
        OR (SourceAntrianId IS NOT NULL AND SourceNoUrut IS NOT NULL AND SourceNoUrut > 0))
)

CREATE INDEX IX_BILRG_AntrianEntry_OperationalWorklist
    ON BILRG_AntrianEntry(AntrianId, AntrianStatus, Priority DESC, CreatedAt, NoUrut)
    INCLUDE (CreationReason, CallCount, PasienTrackerId, ServedAt, DoneAt)
