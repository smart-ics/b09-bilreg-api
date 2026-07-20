CREATE TABLE BILRG_RnaServiceExecutionSourceRevision
(
    ServiceExecutionId VARCHAR(12) NOT NULL, SourceContext VARCHAR(50) NOT NULL, SourceFactId VARCHAR(50) NOT NULL,
    SourceRevision INT NOT NULL, RevisionKind INT NOT NULL, EffectiveAt DATETIME NOT NULL, RecordedAt DATETIME NOT NULL,
    ActorId VARCHAR(50) NOT NULL, Reason VARCHAR(500) NOT NULL,
    CrtUser VARCHAR(50) NOT NULL, CrtDate DATETIME NOT NULL, UpdUser VARCHAR(50) NOT NULL, UpdDate DATETIME NOT NULL,
    VodUser VARCHAR(50) NOT NULL, VodDate DATETIME NOT NULL,
    CONSTRAINT PK_BILRG_RnaServiceExecutionSourceRevision PRIMARY KEY(ServiceExecutionId, SourceRevision)
)
GO
CREATE INDEX IX_BILRG_RnaServiceExecutionSourceRevision_History ON BILRG_RnaServiceExecutionSourceRevision(ServiceExecutionId, SourceRevision)
GO
