CREATE TABLE BILRG_RnaBedReadinessTransaction
(
    TransactionId VARCHAR(12) NOT NULL
        CONSTRAINT PK_BILRG_RnaBedReadinessTransaction PRIMARY KEY,
    BedId VARCHAR(8) NOT NULL,
    NewStatus INT NOT NULL,
    RestrictionType INT NOT NULL,
    Reason VARCHAR(500) NOT NULL,
    EvidenceReference VARCHAR(50) NOT NULL,
    ResponsibleActorId VARCHAR(50) NOT NULL,
    VerifiedByActorId VARCHAR(50) NOT NULL,
    OccurredAt DATETIME NOT NULL,
    RecordedAt DATETIME NOT NULL,
    SourceFactId VARCHAR(50) NOT NULL,
    RequestId VARCHAR(50) NOT NULL,
    CrtUser VARCHAR(50) NOT NULL,
    CrtDate DATETIME NOT NULL,
    UpdUser VARCHAR(50) NOT NULL,
    UpdDate DATETIME NOT NULL,
    VodUser VARCHAR(50) NOT NULL,
    VodDate DATETIME NOT NULL
)
GO

CREATE INDEX IX_BILRG_RnaBedReadinessTransaction_Chronology
    ON BILRG_RnaBedReadinessTransaction(BedId, OccurredAt, TransactionId)
GO

CREATE UNIQUE INDEX UX_BILRG_RnaBedReadinessTransaction_RequestId
    ON BILRG_RnaBedReadinessTransaction(RequestId)
    WHERE RequestId <> ''
GO

CREATE INDEX IX_BILRG_RnaBedReadinessTransaction_SourceFact
    ON BILRG_RnaBedReadinessTransaction(SourceFactId)
    WHERE SourceFactId <> ''
GO
