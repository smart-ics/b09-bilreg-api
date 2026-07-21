IF OBJECT_ID('BILRG_AdmCoordinatedCancellationRequest', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_AdmCoordinatedCancellationRequest
    (
        RequestId VARCHAR(50) NOT NULL,
        Fingerprint VARCHAR(64) NOT NULL,
        RegId VARCHAR(50) NOT NULL,
        Lifecycle VARCHAR(12) NOT NULL,
        ResponseJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_BILRG_AdmCancelRequest_Response DEFAULT(''),
        CreatedAt DATETIME NOT NULL,
        CompletedAt DATETIME NOT NULL,
        CorrelationId VARCHAR(50) NOT NULL,
        CONSTRAINT PK_BILRG_AdmCoordinatedCancellationRequest PRIMARY KEY CLUSTERED (RequestId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_BILRG_AdmCancelRequest_RegId' AND object_id=OBJECT_ID('BILRG_AdmCoordinatedCancellationRequest'))
    CREATE INDEX IX_BILRG_AdmCancelRequest_RegId ON BILRG_AdmCoordinatedCancellationRequest(RegId, Lifecycle);
GO
