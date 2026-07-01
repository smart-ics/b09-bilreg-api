CREATE TABLE BILRG_MergeRequest
(
    MergeRequestId      VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_MergeRequest_MergeRequestId DEFAULT(''),
    SourceRegId         VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_MergeRequest_SourceRegId DEFAULT(''),
    TargetRegId         VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_MergeRequest_TargetRegId DEFAULT(''),
    PatientId           VARCHAR(15) NOT NULL CONSTRAINT DF_BILRG_MergeRequest_PatientId DEFAULT(''),
    Status              INT NOT NULL CONSTRAINT DF_BILRG_MergeRequest_Status DEFAULT(0),
    Reason              NVARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_MergeRequest_Reason DEFAULT(''),

    ExecutedBy          VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_MergeRequest_ExecutedBy DEFAULT(''),
    ExecutedDate        DATETIME NOT NULL CONSTRAINT DF_BILRG_MergeRequest_ExecutedDate DEFAULT('3000-01-01'),
    CancelledBy         VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_MergeRequest_CancelledBy DEFAULT(''),
    CancelledDate       DATETIME NOT NULL CONSTRAINT DF_BILRG_MergeRequest_CancelledDate DEFAULT('3000-01-01'),

    CrtUser             VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_MergeRequest_CrtUser DEFAULT(''),
    CrtDate             DATETIME NOT NULL CONSTRAINT DF_BILRG_MergeRequest_CrtDate DEFAULT('3000-01-01'),
    UpdUser             VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_MergeRequest_UpdUser DEFAULT(''),
    UpdDate             DATETIME NOT NULL CONSTRAINT DF_BILRG_MergeRequest_UpdDate DEFAULT('3000-01-01'),
    VodUser             VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_MergeRequest_VodUser DEFAULT(''),
    VodDate             DATETIME NOT NULL CONSTRAINT DF_BILRG_MergeRequest_VodDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_MergeRequest PRIMARY KEY CLUSTERED (MergeRequestId)
)
GO

CREATE NONCLUSTERED INDEX IX_BILRG_MergeRequest_SourceRegId
    ON BILRG_MergeRequest (SourceRegId, Status)
GO

CREATE NONCLUSTERED INDEX IX_BILRG_MergeRequest_TargetRegId
    ON BILRG_MergeRequest (TargetRegId, Status)
GO

CREATE NONCLUSTERED INDEX IX_BILRG_MergeRequest_PatientId_Status
    ON BILRG_MergeRequest (PatientId, Status)
GO
