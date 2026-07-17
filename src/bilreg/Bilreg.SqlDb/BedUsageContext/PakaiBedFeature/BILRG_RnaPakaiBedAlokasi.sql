CREATE TABLE BILRG_RnaPakaiBedAlokasi
(
    PakaiBedId VARCHAR(15) NOT NULL CONSTRAINT PK_BILRG_RnaPakaiBedAlokasi PRIMARY KEY,
    Version INT NOT NULL,
    RegId VARCHAR(10) NOT NULL,
    PasienId VARCHAR(15) NOT NULL,
    BangsalId VARCHAR(2) NOT NULL,
    KamarId VARCHAR(5) NOT NULL,
    BedId VARCHAR(8) NOT NULL,
    WaitingListId VARCHAR(12) NOT NULL,
    RequestId VARCHAR(50) NOT NULL,
    PakaiBedPurpose INT NOT NULL,
    OccupantRole INT NOT NULL,
    PakaiBedStatus INT NOT NULL,
    ProposedAt DATETIME NOT NULL,
    StartedAt DATETIME NOT NULL,
    AssignedBy VARCHAR(50) NOT NULL,
    BedAssignabilityEvidenceId VARCHAR(50) NOT NULL,
    CrtUser VARCHAR(50) NOT NULL,
    CrtDate DATETIME NOT NULL,
    UpdUser VARCHAR(50) NOT NULL,
    UpdDate DATETIME NOT NULL,
    VodUser VARCHAR(50) NOT NULL,
    VodDate DATETIME NOT NULL
)
GO

CREATE UNIQUE INDEX UX_BILRG_RnaPakaiBedAlokasi_RequestId
    ON BILRG_RnaPakaiBedAlokasi(RequestId)
    WHERE RequestId <> ''
GO

CREATE INDEX IX_BILRG_RnaPakaiBedAlokasi_RegStatus
    ON BILRG_RnaPakaiBedAlokasi(RegId, PakaiBedStatus)
GO

CREATE INDEX IX_BILRG_RnaPakaiBedAlokasi_BedStatus
    ON BILRG_RnaPakaiBedAlokasi(BedId, PakaiBedStatus)
GO

CREATE INDEX IX_BILRG_RnaPakaiBedAlokasi_WaitingList
    ON BILRG_RnaPakaiBedAlokasi(WaitingListId)
    WHERE WaitingListId <> ''
GO
