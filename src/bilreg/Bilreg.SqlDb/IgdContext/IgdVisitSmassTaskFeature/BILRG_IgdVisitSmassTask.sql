CREATE TABLE BILRG_IgdVisitSmassTask (
    IgdVisitSmassTaskId VARCHAR(14) NOT NULL CONSTRAINT DF_BILRG_IgdVisitSmassTask_IgdVisitSmassTaskId DEFAULT(''),
    IgdVisitId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_IgdVisitSmassTask_IgdVisitId DEFAULT(''),
    NoTriage INT NOT NULL CONSTRAINT DF_BILRG_IgdVisitSmassTask_NoTriage DEFAULT(0),
    TaskType INT NOT NULL CONSTRAINT DF_BILRG_IgdVisitSmassTask_TaskType DEFAULT(0),
    TaskStatus INT NOT NULL CONSTRAINT DF_BILRG_IgdVisitSmassTask_TaskStatus DEFAULT(0),
    AssessmentId VARCHAR(13) NOT NULL CONSTRAINT DF_BILRG_IgdVisitSmassTask_AssessmentId DEFAULT(''),
    RetryCount INT NOT NULL CONSTRAINT DF_BILRG_IgdVisitSmassTask_RetryCount DEFAULT(0),
    LastRetryDate DATETIME NOT NULL CONSTRAINT DF_BILRG_IgdVisitSmassTask_LastRetryDate DEFAULT('3000-01-01'),
    ProcessedDate DATETIME NOT NULL CONSTRAINT DF_BILRG_IgdVisitSmassTask_ProcessedDate DEFAULT('3000-01-01'),
    LastError VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_IgdVisitSmassTask_LastError DEFAULT(''),
    CrtDate DATETIME NOT NULL,

    CONSTRAINT PK_BILRG_IgdVisitSmassTask PRIMARY KEY CLUSTERED (IgdVisitSmassTaskId),
    CONSTRAINT UX_BILRG_IgdVisitSmassTask_BusinessKey UNIQUE (IgdVisitId, NoTriage, TaskType)
);
GO

CREATE INDEX IX_BILRG_IgdVisitSmassTask_Visit
    ON BILRG_IgdVisitSmassTask(IgdVisitId)
    INCLUDE (NoTriage, TaskType, TaskStatus, AssessmentId);
GO

CREATE INDEX IX_BILRG_IgdVisitSmassTask_Status
    ON BILRG_IgdVisitSmassTask(TaskStatus, CrtDate);
GO
