IF OBJECT_ID('BILRG_AdmDigitalSign', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_AdmDigitalSign
    (
        SigningRequestId    VARCHAR(36) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_SigningRequestId DEFAULT(''),
        RegId               VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_RegId DEFAULT(''),
        HisReference        VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_HisReference DEFAULT(''),
        DokumenId           VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_DokumenId DEFAULT(''),
        PasienId            VARCHAR(15) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_PasienId DEFAULT('-'),
        SignerId            VARCHAR(36) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_SignerId DEFAULT(''),
        FileName            VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_FileName DEFAULT(''),

        CrtUser             VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_CrtUser DEFAULT(''),
        CrtDate             DATETIME    NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_CrtDate DEFAULT('3000-01-01'),
        UpdUser             VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_UpdUser DEFAULT(''),
        UpdDate             DATETIME    NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_UpdDate DEFAULT('3000-01-01'),
        VodUser             VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_VodUser DEFAULT(''),
        VodDate             DATETIME    NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_AdmDigitalSign PRIMARY KEY CLUSTERED (SigningRequestId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_BILRG_AdmDigitalSign_RegId_DokumenId'
      AND object_id = OBJECT_ID('BILRG_AdmDigitalSign'))
BEGIN
    CREATE UNIQUE INDEX UQ_BILRG_AdmDigitalSign_RegId_DokumenId
        ON BILRG_AdmDigitalSign (RegId, DokumenId)
        WITH (FILLFACTOR = 90);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_AdmDigitalSign_RegId'
      AND object_id = OBJECT_ID('BILRG_AdmDigitalSign'))
BEGIN
    CREATE INDEX IX_BILRG_AdmDigitalSign_RegId
        ON BILRG_AdmDigitalSign (RegId)
        WITH (FILLFACTOR = 90);
END
GO
        