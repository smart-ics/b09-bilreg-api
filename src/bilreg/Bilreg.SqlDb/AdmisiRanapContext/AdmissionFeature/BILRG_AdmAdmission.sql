IF OBJECT_ID('BILRG_AdmAdmission', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_AdmAdmission
    (
        RegId             VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_RegId DEFAULT(''),
        AdmissionStatus   INT         NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_AdmissionStatus DEFAULT(0),
        AdmissionSource   INT         NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_AdmissionSource DEFAULT(0),
        PasienId          VARCHAR(15) NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_PasienId DEFAULT('-'),
        OpnameRequestId   VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_OpnameRequestId DEFAULT('-'),
        ReservationId     VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_ReservationId DEFAULT('-'),
        KelasDkId         VARCHAR(1)  NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_KelasDkId DEFAULT('-'),
        KelasDkName       VARCHAR(15) NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_KelasDkName DEFAULT(''),
        BangsalId         VARCHAR(5)  NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_BangsalId DEFAULT('-'),
        BangsalName       VARCHAR(40) NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_BangsalName DEFAULT(''),
        AdmissionDate     DATETIME    NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_AdmissionDate DEFAULT('3000-01-01'),

        CrtUser           VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_CrtUser DEFAULT(''),
        CrtDate           DATETIME    NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_CrtDate DEFAULT('3000-01-01'),
        UpdUser           VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_UpdUser DEFAULT(''),
        UpdDate           DATETIME    NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_UpdDate DEFAULT('3000-01-01'),
        VodUser           VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_VodUser DEFAULT(''),
        VodDate           DATETIME    NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_AdmAdmission PRIMARY KEY CLUSTERED (RegId)
    );
END
GO

IF COL_LENGTH('BILRG_AdmAdmission', 'AdmissionSource') IS NULL
BEGIN
    ALTER TABLE BILRG_AdmAdmission
        ADD AdmissionSource INT NOT NULL
            CONSTRAINT DF_BILRG_AdmAdmission_AdmissionSource DEFAULT(0);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_AdmAdmission_Status_CrtDate'
      AND object_id = OBJECT_ID('BILRG_AdmAdmission'))
BEGIN
    CREATE INDEX IX_BILRG_AdmAdmission_Status_CrtDate
        ON BILRG_AdmAdmission (AdmissionStatus, CrtDate)
        WITH (FILLFACTOR = 90);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_AdmAdmission_PasienId_Status'
      AND object_id = OBJECT_ID('BILRG_AdmAdmission'))
BEGIN
    CREATE INDEX IX_BILRG_AdmAdmission_PasienId_Status
        ON BILRG_AdmAdmission (PasienId, AdmissionStatus)
        WITH (FILLFACTOR = 90);
END
GO
