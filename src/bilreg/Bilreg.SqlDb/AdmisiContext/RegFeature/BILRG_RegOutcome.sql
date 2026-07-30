CREATE TABLE BILRG_RegOutcome
(
    OutcomeId VARCHAR(26) NOT NULL,
    AntrianId VARCHAR(26) NOT NULL,
    NoUrut INT NOT NULL,
    OutcomeType INT NOT NULL,
    RegId VARCHAR(20) NOT NULL,
    ReasonCode VARCHAR(50) NOT NULL,
    CrtUser VARCHAR(50) NOT NULL,
    CrtDate DATETIME NOT NULL,
    UpdUser VARCHAR(50) NOT NULL,
    UpdDate DATETIME NOT NULL,
    VodUser VARCHAR(50) NOT NULL,
    VodDate DATETIME NOT NULL,
    CONSTRAINT PK_BILRG_RegOutcome PRIMARY KEY CLUSTERED(OutcomeId),
    CONSTRAINT UX_BILRG_RegOutcome_QueueEntry UNIQUE(AntrianId,NoUrut),
    CONSTRAINT CK_BILRG_RegOutcome_Type CHECK(OutcomeType IN(1,2)),
    CONSTRAINT CK_BILRG_RegOutcome_Combination CHECK(
      (OutcomeType=1 AND RegId<>'' AND ReasonCode='') OR
      (OutcomeType=2 AND RegId='' AND ReasonCode<>''))
);
