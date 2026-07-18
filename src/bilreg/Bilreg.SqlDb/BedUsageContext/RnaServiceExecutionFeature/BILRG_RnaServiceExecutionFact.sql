CREATE TABLE BILRG_RnaServiceExecutionFact
(
    ServiceExecutionFactId VARCHAR(12) NOT NULL CONSTRAINT PK_BILRG_RnaServiceExecutionFact PRIMARY KEY,
    ServiceExecutionId VARCHAR(12) NOT NULL, ExecutionRevision INT NOT NULL, BillableClassification INT NOT NULL,
    TarifServiceId VARCHAR(50) NOT NULL, TarifServiceName VARCHAR(200) NOT NULL, NonBillableDescription VARCHAR(500) NOT NULL,
    PerformerId VARCHAR(50) NOT NULL, PerformedAt DATETIME NOT NULL, RecordedAt DATETIME NOT NULL,
    RecorderActorId VARCHAR(50) NOT NULL, LateEntryReason VARCHAR(500) NOT NULL,
    CrtUser VARCHAR(50) NOT NULL, CrtDate DATETIME NOT NULL, UpdUser VARCHAR(50) NOT NULL, UpdDate DATETIME NOT NULL,
    VodUser VARCHAR(50) NOT NULL, VodDate DATETIME NOT NULL
)
GO
CREATE UNIQUE INDEX UX_BILRG_RnaServiceExecutionFact_Revision ON BILRG_RnaServiceExecutionFact(ServiceExecutionId, ExecutionRevision)
GO
CREATE INDEX IX_BILRG_RnaServiceExecutionFact_History ON BILRG_RnaServiceExecutionFact(ServiceExecutionId, PerformedAt, ServiceExecutionFactId)
GO
