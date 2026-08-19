CREATE TABLE BILRG_AptIntegrationTask (
    IntegrationTaskId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AptIntegrationTask_IntegrationTaskId DEFAULT(''),
    TaskType INT NOT NULL CONSTRAINT DF_BILRG_AptIntegrationTask_TaskType DEFAULT(0),
    SourceKind INT NOT NULL CONSTRAINT DF_BILRG_AptIntegrationTask_SourceKind DEFAULT(0),
    SourceId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AptIntegrationTask_SourceId DEFAULT(''),
    IdempotencyKey VARCHAR(80) NOT NULL CONSTRAINT DF_BILRG_AptIntegrationTask_IdempotencyKey DEFAULT(''),
    Destination INT NOT NULL CONSTRAINT DF_BILRG_AptIntegrationTask_Destination DEFAULT(0),
    PayloadJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_BILRG_AptIntegrationTask_PayloadJson DEFAULT(''),
    TaskStatus INT NOT NULL CONSTRAINT DF_BILRG_AptIntegrationTask_TaskStatus DEFAULT(0),
    RetryCount INT NOT NULL CONSTRAINT DF_BILRG_AptIntegrationTask_RetryCount DEFAULT(0),
    LastError VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_AptIntegrationTask_LastError DEFAULT(''),
    LastRetryDate DATETIME NOT NULL CONSTRAINT DF_BILRG_AptIntegrationTask_LastRetryDate DEFAULT('3000-01-01'),
    ProcessedDate DATETIME NOT NULL CONSTRAINT DF_BILRG_AptIntegrationTask_ProcessedDate DEFAULT('3000-01-01'),
    CorrelationId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_AptIntegrationTask_CorrelationId DEFAULT(''),
    CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_AptIntegrationTask_CrtDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_AptIntegrationTask PRIMARY KEY CLUSTERED (IntegrationTaskId)
);
GO

CREATE UNIQUE INDEX UX_BILRG_AptIntegrationTask_Idempotency
    ON BILRG_AptIntegrationTask(IdempotencyKey);
GO

CREATE INDEX IX_BILRG_AptIntegrationTask_Pending
    ON BILRG_AptIntegrationTask(TaskStatus, CrtDate);
GO
