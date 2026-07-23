CREATE TABLE BILRG_AdmLoketCurrentCall
(
    LoketKey VARCHAR(50) NOT NULL,
    AntrianId VARCHAR(26) NOT NULL,
    NoUrut INT NOT NULL,
    ClaimState INT NOT NULL,
    IsActive BIT NOT NULL,
    AnnouncementVersion BIGINT NOT NULL,
    CalledAt DATETIME NOT NULL,
    ServiceStartedAt DATETIME NOT NULL,
    ReleasedAt DATETIME NOT NULL,
    CrtUser VARCHAR(50) NOT NULL,
    CrtDate DATETIME NOT NULL,
    UpdUser VARCHAR(50) NOT NULL,
    UpdDate DATETIME NOT NULL,
    VodUser VARCHAR(50) NOT NULL,
    VodDate DATETIME NOT NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT PK_BILRG_AdmLoketCurrentCall PRIMARY KEY CLUSTERED (LoketKey),
    CONSTRAINT CK_BILRG_AdmLoketCurrentCall_ClaimState CHECK (ClaimState IN (0,1,2)),
    CONSTRAINT CK_BILRG_AdmLoketCurrentCall_ActiveState CHECK (
        (ClaimState=0 AND IsActive=0) OR (ClaimState IN (1,2) AND IsActive=1)),
    CONSTRAINT CK_BILRG_AdmLoketCurrentCall_Entry CHECK (AntrianId<>'' AND NoUrut>0),
    CONSTRAINT CK_BILRG_AdmLoketCurrentCall_AnnouncementVersion CHECK (AnnouncementVersion>=0)
);

CREATE UNIQUE INDEX UX_BILRG_AdmLoketCurrentCall_ActiveEntry
    ON BILRG_AdmLoketCurrentCall(AntrianId, NoUrut) WHERE IsActive=1;

CREATE INDEX IX_BILRG_AdmLoketCurrentCall_ActiveDisplay
    ON BILRG_AdmLoketCurrentCall(IsActive, LoketKey)
    INCLUDE (AntrianId, NoUrut, ClaimState, AnnouncementVersion, CalledAt, ServiceStartedAt, RowVersion);
