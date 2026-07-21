CREATE TABLE BILRG_RnaBedOperational
(
    BedId VARCHAR(8) NOT NULL CONSTRAINT PK_BILRG_RnaBedOperational PRIMARY KEY,
    BangsalId VARCHAR(2) NOT NULL,
    KamarId VARCHAR(5) NOT NULL,
    OccupancyPolicyId VARCHAR(12) NOT NULL,
    OccupancyPolicyName VARCHAR(100) NOT NULL,
    CurrentReadiness INT NULL,
    LatestReadinessTransactionId VARCHAR(12) NOT NULL,
    IsBlocked BIT NOT NULL,
    CurrentRestrictionType INT NOT NULL,
    CurrentBlockerReason VARCHAR(500) NOT NULL,
    CurrentBlockerTransactionId VARCHAR(12) NOT NULL,
    OccupancyEpoch BIGINT NOT NULL,
    Version INT NOT NULL,
    CrtUser VARCHAR(50) NOT NULL,
    CrtDate DATETIME NOT NULL,
    UpdUser VARCHAR(50) NOT NULL,
    UpdDate DATETIME NOT NULL,
    VodUser VARCHAR(50) NOT NULL,
    VodDate DATETIME NOT NULL
)
GO

CREATE INDEX IX_BILRG_RnaBedOperational_WardReadiness
    ON BILRG_RnaBedOperational(BangsalId, CurrentReadiness, IsBlocked)
GO

CREATE INDEX IX_BILRG_RnaBedOperational_Recovery
    ON BILRG_RnaBedOperational(IsBlocked, CurrentRestrictionType, UpdDate)
GO
