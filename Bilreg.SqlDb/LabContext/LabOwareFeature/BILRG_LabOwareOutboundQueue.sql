CREATE TABLE BILRG_LabOwareOutboundQueue (
    QueueId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_LabOwareOutboundQueue_QueueId DEFAULT(''),
    OrderId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_LabOwareOutboundQueue_OrderId DEFAULT(''),
    MessageType VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_LabOwareOutboundQueue_MessageType DEFAULT(''),
    PayloadJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_BILRG_LabOwareOutboundQueue_PayloadJson DEFAULT(''),
    QueueStatus INT NOT NULL CONSTRAINT DF_BILRG_LabOwareOutboundQueue_QueueStatus DEFAULT(0),
    RetryCount INT NOT NULL CONSTRAINT DF_BILRG_LabOwareOutboundQueue_RetryCount DEFAULT(0),
    LastRetryDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabOwareOutboundQueue_LastRetryDate DEFAULT('3000-01-01'),
    ProcessedDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabOwareOutboundQueue_ProcessedDate DEFAULT('3000-01-01'),
    LastError VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_LabOwareOutboundQueue_LastError DEFAULT(''),
    CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabOwareOutboundQueue_CrtDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_LabOwareOutboundQueue PRIMARY KEY CLUSTERED (QueueId)
);
GO

CREATE INDEX IX_BILRG_LabOwareOutboundQueue_Status ON BILRG_LabOwareOutboundQueue(QueueStatus, CrtDate);
GO
