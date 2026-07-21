CREATE TABLE BILRG_RnaBedReadinessCorrection
(
    CorrectionId VARCHAR(12) NOT NULL
        CONSTRAINT PK_BILRG_RnaBedReadinessCorrection PRIMARY KEY,
    BedId VARCHAR(8) NOT NULL,
    OriginalTransactionId VARCHAR(12) NOT NULL,
    ReplacementTransactionId VARCHAR(12) NOT NULL,
    ActorId VARCHAR(50) NOT NULL,
    Reason VARCHAR(500) NOT NULL,
    OccurredAt DATETIME NOT NULL,
    RecordedAt DATETIME NOT NULL,
    RequestId VARCHAR(50) NOT NULL,
    CrtUser VARCHAR(50) NOT NULL,
    CrtDate DATETIME NOT NULL,
    UpdUser VARCHAR(50) NOT NULL,
    UpdDate DATETIME NOT NULL,
    VodUser VARCHAR(50) NOT NULL,
    VodDate DATETIME NOT NULL
)
GO

CREATE UNIQUE INDEX UX_BILRG_RnaBedReadinessCorrection_Original
    ON BILRG_RnaBedReadinessCorrection(OriginalTransactionId)
GO

CREATE UNIQUE INDEX UX_BILRG_RnaBedReadinessCorrection_RequestId
    ON BILRG_RnaBedReadinessCorrection(RequestId)
    WHERE RequestId <> ''
GO

CREATE INDEX IX_BILRG_RnaBedReadinessCorrection_Chronology
    ON BILRG_RnaBedReadinessCorrection(BedId, OccurredAt, CorrectionId)
GO
