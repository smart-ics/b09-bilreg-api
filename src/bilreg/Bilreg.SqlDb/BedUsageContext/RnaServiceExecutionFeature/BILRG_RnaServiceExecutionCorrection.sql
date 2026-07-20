CREATE TABLE BILRG_RnaServiceExecutionCorrection
(
    CorrectionFactId VARCHAR(12) NOT NULL CONSTRAINT PK_BILRG_RnaServiceExecutionCorrection PRIMARY KEY,
    ServiceExecutionId VARCHAR(12) NOT NULL, OriginalServiceExecutionFactId VARCHAR(12) NOT NULL,
    PreviousRevision INT NOT NULL, CorrectionRevision INT NOT NULL, CorrectionKind INT NOT NULL,
    ReplacementServiceExecutionFactId VARCHAR(12) NOT NULL, CorrectionReason VARCHAR(500) NOT NULL,
    CorrectingActorId VARCHAR(50) NOT NULL, SecondReviewerId VARCHAR(50) NOT NULL, EvidenceReference VARCHAR(50) NOT NULL,
    CorrectedAt DATETIME NOT NULL, RecordedAt DATETIME NOT NULL,
    CrtUser VARCHAR(50) NOT NULL, CrtDate DATETIME NOT NULL, UpdUser VARCHAR(50) NOT NULL, UpdDate DATETIME NOT NULL,
    VodUser VARCHAR(50) NOT NULL, VodDate DATETIME NOT NULL
)
GO
CREATE UNIQUE INDEX UX_BILRG_RnaServiceExecutionCorrection_Revision ON BILRG_RnaServiceExecutionCorrection(ServiceExecutionId, CorrectionRevision)
GO
CREATE INDEX IX_BILRG_RnaServiceExecutionCorrection_History ON BILRG_RnaServiceExecutionCorrection(ServiceExecutionId, CorrectedAt, CorrectionFactId)
GO
