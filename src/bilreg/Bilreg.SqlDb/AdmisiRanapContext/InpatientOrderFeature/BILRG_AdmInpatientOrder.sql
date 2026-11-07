IF OBJECT_ID('BILRG_AdmInpatientOrder', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_AdmInpatientOrder
    (
        InpatientOrderId     VARCHAR(12)  NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_InpatientOrderId DEFAULT(''),
        OrderSource          INT          NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_OrderSource DEFAULT(1),
        OrderStatus          INT          NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_OrderStatus DEFAULT(0),
        PasienId             VARCHAR(15)  NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_PasienId DEFAULT('-'),
        PasienName           VARCHAR(60)  NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_PasienName DEFAULT(''),
        TglLahir             DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_TglLahir DEFAULT('3000-01-01'),
        Gender               VARCHAR(1)   NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_Gender DEFAULT(''),
        DokterId             VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_DokterId DEFAULT('-'),
        DokterName           VARCHAR(60)  NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_DokterName DEFAULT(''),
        PlannedDate          DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_PlannedDate DEFAULT('3000-01-01'),
        KelasId              VARCHAR(3)   NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_KelasId DEFAULT('-'),
        KelasName            VARCHAR(30)  NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_KelasName DEFAULT(''),
        BangsalId            VARCHAR(5)   NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_BangsalId DEFAULT('-'),
        BangsalName          VARCHAR(40)  NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_BangsalName DEFAULT(''),
        ClinicalNotes        VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_ClinicalNotes DEFAULT(''),
        TipeJaminanId        VARCHAR(5)   NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_TipeJaminanId DEFAULT('-'),
        TipeJaminanName      VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_TipeJaminanName DEFAULT(''),
        JaminanReffId        VARCHAR(12)  NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_JaminanReffId DEFAULT('-'),
        FulfilledRegId       VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_FulfilledRegId DEFAULT('-'),
        ExternalReferenceId  VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_ExternalReferenceId DEFAULT('-'),
        ExternalSystemSource VARCHAR(30)  NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_ExternalSystemSource DEFAULT(''),

        CrtUser              VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_CrtUser DEFAULT(''),
        CrtDate              DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_CrtDate DEFAULT('3000-01-01'),
        UpdUser              VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_UpdUser DEFAULT(''),
        UpdDate              DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_UpdDate DEFAULT('3000-01-01'),
        VodUser              VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_VodUser DEFAULT(''),
        VodDate              DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmInpatientOrder_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_AdmInpatientOrder PRIMARY KEY CLUSTERED (InpatientOrderId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_AdmInpatientOrder_Status_PlannedDate'
      AND object_id = OBJECT_ID('BILRG_AdmInpatientOrder'))
BEGIN
    CREATE INDEX IX_BILRG_AdmInpatientOrder_Status_PlannedDate
        ON BILRG_AdmInpatientOrder (OrderStatus, PlannedDate, VodDate)
        WITH (FILLFACTOR = 90);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_AdmInpatientOrder_ExtRef'
      AND object_id = OBJECT_ID('BILRG_AdmInpatientOrder'))
BEGIN
    CREATE INDEX IX_BILRG_AdmInpatientOrder_ExtRef
        ON BILRG_AdmInpatientOrder (ExternalReferenceId, ExternalSystemSource)
        WITH (FILLFACTOR = 90);
END
GO
