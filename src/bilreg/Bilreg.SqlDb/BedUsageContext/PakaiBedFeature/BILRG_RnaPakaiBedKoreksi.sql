CREATE TABLE BILRG_RnaPakaiBedKoreksi
(
    CorrectionId VARCHAR(12) NOT NULL CONSTRAINT PK_BILRG_RnaPakaiBedKoreksi PRIMARY KEY,
    PakaiBedId VARCHAR(15) NOT NULL,
    OriginalTransitionId VARCHAR(12) NOT NULL,
    PakaiBedCorrection INT NOT NULL,
    OriginalBangsalId VARCHAR(2) NOT NULL,
    Reason VARCHAR(500) NOT NULL,
    CorrectedBy VARCHAR(50) NOT NULL,
    OccurredAt DATETIME NOT NULL,
    RecordedAt DATETIME NOT NULL,
    ReviewReference VARCHAR(50) NOT NULL,
    CrtUser VARCHAR(50) NOT NULL,
    CrtDate DATETIME NOT NULL,
    UpdUser VARCHAR(50) NOT NULL,
    UpdDate DATETIME NOT NULL,
    VodUser VARCHAR(50) NOT NULL,
    VodDate DATETIME NOT NULL
)
GO

CREATE INDEX IX_BILRG_RnaPakaiBedKoreksi_Chronology
    ON BILRG_RnaPakaiBedKoreksi(PakaiBedId, OccurredAt, CorrectionId)
GO
