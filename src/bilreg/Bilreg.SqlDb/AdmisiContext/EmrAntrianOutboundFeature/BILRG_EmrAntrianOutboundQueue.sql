CREATE TABLE BILRG_EmrAntrianOutboundQueue (
    QueueId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_EmrAntrianOutboundQueue_QueueId DEFAULT(''),
    SourceId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_EmrAntrianOutboundQueue_SourceId DEFAULT(''),
    MessageType VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_EmrAntrianOutboundQueue_MessageType DEFAULT(''),
    PayloadJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_BILRG_EmrAntrianOutboundQueue_PayloadJson DEFAULT(''),
    QueueStatus INT NOT NULL CONSTRAINT DF_BILRG_EmrAntrianOutboundQueue_QueueStatus DEFAULT(0),
    RetryCount INT NOT NULL CONSTRAINT DF_BILRG_EmrAntrianOutboundQueue_RetryCount DEFAULT(0),
    LastRetryDate DATETIME NOT NULL CONSTRAINT DF_BILRG_EmrAntrianOutboundQueue_LastRetryDate DEFAULT('3000-01-01'),
    ProcessedDate DATETIME NOT NULL CONSTRAINT DF_BILRG_EmrAntrianOutboundQueue_ProcessedDate DEFAULT('3000-01-01'),
    LastError VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_EmrAntrianOutboundQueue_LastError DEFAULT(''),
    CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_EmrAntrianOutboundQueue_CrtDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_EmrAntrianOutboundQueue PRIMARY KEY CLUSTERED (QueueId)
);
GO

CREATE INDEX IX_BILRG_EmrAntrianOutboundQueue_Status ON BILRG_EmrAntrianOutboundQueue(QueueStatus, CrtDate);
GO

CREATE UNIQUE INDEX UX_BILRG_EmrAntrianOutboundQueue_ActiveSource
    ON BILRG_EmrAntrianOutboundQueue(SourceId, MessageType)
    WHERE QueueStatus IN (0, 1, 2);
GO
