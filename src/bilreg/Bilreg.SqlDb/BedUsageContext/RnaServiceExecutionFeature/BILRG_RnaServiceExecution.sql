CREATE TABLE BILRG_RnaServiceExecution
(
    ServiceExecutionId VARCHAR(12) NOT NULL CONSTRAINT PK_BILRG_RnaServiceExecution PRIMARY KEY,
    RegistrationId VARCHAR(50) NOT NULL, PatientId VARCHAR(50) NOT NULL, CareContextId VARCHAR(50) NOT NULL,
    ResponsibleWardId VARCHAR(50) NOT NULL, ExecutionSource INT NOT NULL,
    ClinicalOrderId VARCHAR(50) NOT NULL, OrderOccurrenceId VARCHAR(50) NOT NULL,
    FulfilmentObligationId VARCHAR(50) NOT NULL, SourceContext VARCHAR(50) NOT NULL,
    SourceFactId VARCHAR(50) NOT NULL, SourceRevision INT NOT NULL,
    AssignedPerformerId VARCHAR(50) NOT NULL, WorkStatus INT NOT NULL,
    HasExecutionAuthority BIT NOT NULL, AuthorityBasisReference VARCHAR(50) NOT NULL,
    RequiresSubsequentAuthorization BIT NOT NULL, AuthorizationDueAt DATETIME NULL,
    SubsequentAuthorizationStatus INT NOT NULL, Version INT NOT NULL,
    CrtUser VARCHAR(50) NOT NULL, CrtDate DATETIME NOT NULL, UpdUser VARCHAR(50) NOT NULL,
    UpdDate DATETIME NOT NULL, VodUser VARCHAR(50) NOT NULL, VodDate DATETIME NOT NULL
)
GO
CREATE UNIQUE INDEX UX_BILRG_RnaServiceExecution_Source ON BILRG_RnaServiceExecution(SourceContext, SourceFactId) WHERE SourceFactId <> ''
GO
CREATE UNIQUE INDEX UX_BILRG_RnaServiceExecution_Obligation ON BILRG_RnaServiceExecution(FulfilmentObligationId) WHERE FulfilmentObligationId <> ''
GO
CREATE INDEX IX_BILRG_RnaServiceExecution_Worklist ON BILRG_RnaServiceExecution(ResponsibleWardId, WorkStatus, UpdDate)
GO
