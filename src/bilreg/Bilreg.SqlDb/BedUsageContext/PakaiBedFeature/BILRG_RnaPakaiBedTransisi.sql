CREATE TABLE BILRG_RnaPakaiBedTransisi
(
    TransitionId VARCHAR(12) NOT NULL CONSTRAINT PK_BILRG_RnaPakaiBedTransisi PRIMARY KEY,
    PakaiBedId VARCHAR(15) NOT NULL,
    PakaiBedTransition INT NOT NULL,
    OccurredAt DATETIME NOT NULL,
    RecordedAt DATETIME NOT NULL,
    ActorId VARCHAR(50) NOT NULL,
    Reason VARCHAR(500) NOT NULL,
    RequestId VARCHAR(50) NOT NULL,
    WaitingListId VARCHAR(12) NOT NULL,
    BedAssignabilityEvidenceId VARCHAR(50) NOT NULL,
    CrtUser VARCHAR(50) NOT NULL,
    CrtDate DATETIME NOT NULL,
    UpdUser VARCHAR(50) NOT NULL,
    UpdDate DATETIME NOT NULL,
    VodUser VARCHAR(50) NOT NULL,
    VodDate DATETIME NOT NULL
)
GO

CREATE INDEX IX_BILRG_RnaPakaiBedTransisi_Chronology
    ON BILRG_RnaPakaiBedTransisi(PakaiBedId, OccurredAt, TransitionId)
GO
