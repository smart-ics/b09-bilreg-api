IF OBJECT_ID('BILRG_AdmOpnameRequest', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_AdmOpnameRequest
    (
        OpnameRequestId     VARCHAR(12)  NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequest_OpnameRequestId DEFAULT(''),
        OpnameRequestStatus INT          NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequest_OpnameRequestStatus DEFAULT(0),
        PasienId            VARCHAR(15)  NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequest_PasienId DEFAULT('-'),
        DokterId            VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequest_DokterId DEFAULT('-'),
        DokterName          VARCHAR(60)  NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequest_DokterName DEFAULT(''),
        PlannedDate         DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequest_PlannedDate DEFAULT('3000-01-01'),
        ClinicalNotes       VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequest_ClinicalNotes DEFAULT(''),
        FulfilledRegId      VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequest_FulfilledRegId DEFAULT('-'),
        EmrOrderId          Varchar(14)  NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequest_EmrOrderId DEFAULT('-'),

        CrtUser             VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequest_CrtUser DEFAULT(''),
        CrtDate             DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequest_CrtDate DEFAULT('3000-01-01'),
        UpdUser             VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequest_UpdUser DEFAULT(''),
        UpdDate             DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequest_UpdDate DEFAULT('3000-01-01'),
        VodUser             VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequest_VodUser DEFAULT(''),
        VodDate             DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequest_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_AdmOpnameRequest PRIMARY KEY CLUSTERED (OpnameRequestId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_AdmOpnameRequest_Status_CrtDate'
      AND object_id = OBJECT_ID('BILRG_AdmOpnameRequest'))
BEGIN
    CREATE INDEX IX_BILRG_AdmOpnameRequest_Status_CrtDate
        ON BILRG_AdmOpnameRequest (OpnameRequestStatus, CrtDate)
        WITH (FILLFACTOR = 90);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_bilrg_admopnamerequest_EmrOrderId'
      AND object_id = OBJECT_ID('BILRG_AdmOpnameRequest'))
BEGIN
    CREATE INDEX [IX_bilrg_admopnamerequest_emrorderid]
        ON [bilrg_admopnamerequest] ([emrorderid],[opnamerequestid])
        WITH(FILLFACTOR=90);
END
GO