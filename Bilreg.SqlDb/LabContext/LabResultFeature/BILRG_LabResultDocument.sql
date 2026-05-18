CREATE TABLE BILRG_LabResultDocument (
    ResultDocumentId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_ResultDocumentId DEFAULT(''),
    OrderId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_OrderId DEFAULT(''),
    VersionNo INT NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_VersionNo DEFAULT(1),

    ResultSource INT NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_ResultSource DEFAULT(1),
    ResultStatus INT NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_ResultStatus DEFAULT(1),

    RecordedDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_RecordedDate DEFAULT('3000-01-01'),
    RecordedUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_RecordedUserId DEFAULT(''),

    VerifiedDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_VerifiedDate DEFAULT('3000-01-01'),
    VerifiedUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_VerifiedUserId DEFAULT(''),

    IsCurrentVersion BIT NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_IsCurrentVersion DEFAULT(1),

    CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_CrtUser DEFAULT(''),
    CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_CrtDate DEFAULT('3000-01-01'),
    UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_UpdUser DEFAULT(''),
    UpdDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_UpdDate DEFAULT('3000-01-01'),
    VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_VodUser DEFAULT(''),
    VodDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_VodDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_LabResultDocument PRIMARY KEY CLUSTERED (ResultDocumentId)
);
GO

CREATE UNIQUE INDEX UX_BILRG_LabResultDocument_OrderId ON BILRG_LabResultDocument(OrderId) WHERE OrderId <> '';
GO
