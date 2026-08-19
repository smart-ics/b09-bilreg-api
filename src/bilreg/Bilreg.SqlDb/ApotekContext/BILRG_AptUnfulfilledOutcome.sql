CREATE TABLE BILRG_AptUnfulfilledOutcome (
    SalesOrderId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AptUnfulfilledOutcome_SalesOrderId DEFAULT(''),
    OutcomeNo INT NOT NULL CONSTRAINT DF_BILRG_AptUnfulfilledOutcome_OutcomeNo DEFAULT(0),
    SalesOrderItemNo INT NOT NULL CONSTRAINT DF_BILRG_AptUnfulfilledOutcome_SalesOrderItemNo DEFAULT(0),
    Qty DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_AptUnfulfilledOutcome_Qty DEFAULT(0),
    Reason INT NOT NULL CONSTRAINT DF_BILRG_AptUnfulfilledOutcome_Reason DEFAULT(0),
    CopyResepId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AptUnfulfilledOutcome_CopyResepId DEFAULT(''),
    ActorId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptUnfulfilledOutcome_ActorId DEFAULT(''),
    EffectiveAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AptUnfulfilledOutcome_EffectiveAt DEFAULT('3000-01-01'),
    CONSTRAINT PK_BILRG_AptUnfulfilledOutcome PRIMARY KEY CLUSTERED (SalesOrderId, OutcomeNo)
);
GO
