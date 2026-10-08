IF OBJECT_ID('BILRG_AdmDigitalSign', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_AdmDigitalSign
    (
        SigningRequestId    VARCHAR(36)  NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_SigningRequestId DEFAULT(''),
        RegId               VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_RegId DEFAULT(''),
        HisReference        VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_HisReference DEFAULT(''),
        DokumenId           VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_DokumenId DEFAULT(''),
        PasienId            VARCHAR(15)  NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_PasienId DEFAULT('-'),
        SignerId            VARCHAR(36)  NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_SignerId DEFAULT(''),
        FileName            VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_FileName DEFAULT(''),
        PatientSignState    VARCHAR(30)  NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_PatientSignState DEFAULT(''),

        OftaDocId           VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_OftaDocId DEFAULT(''),
        OftaDocState        VARCHAR(30)  NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_OftaDocState DEFAULT(''),
        OftaSignState       VARCHAR(30)  NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_OftaSignState DEFAULT(''),
        OfficerRef          VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_OfficerRef DEFAULT(''),
        OfficerEmail        VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_OfficerEmail DEFAULT(''),
        OfficerName         VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_OfficerName DEFAULT(''),
        ExternalDocumentId  VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_ExternalDocumentId DEFAULT(''),
        SignedDocUrl        VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_SignedDocUrl DEFAULT(''),
        IsArchived          BIT          NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_IsArchived DEFAULT(0),
        ArchiveId           VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_ArchiveId DEFAULT(''),
        ArchiveDate         DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_ArchiveDate DEFAULT('3000-01-01'),

        CrtUser             VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_CrtUser DEFAULT(''),
        CrtDate             DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_CrtDate DEFAULT('3000-01-01'),
        UpdUser             VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_UpdUser DEFAULT(''),
        UpdDate             DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_UpdDate DEFAULT('3000-01-01'),
        VodUser             VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_VodUser DEFAULT(''),
        VodDate             DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_AdmDigitalSign PRIMARY KEY CLUSTERED (SigningRequestId)
    );
END
GO

IF OBJECT_ID('BILRG_AdmDigitalSign', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AdmDigitalSign') AND name = 'PatientSignState')
        ALTER TABLE BILRG_AdmDigitalSign ADD PatientSignState VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_PatientSignState DEFAULT('');
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AdmDigitalSign') AND name = 'OftaDocId')
        ALTER TABLE BILRG_AdmDigitalSign ADD OftaDocId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_OftaDocId DEFAULT('');
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AdmDigitalSign') AND name = 'OftaDocState')
        ALTER TABLE BILRG_AdmDigitalSign ADD OftaDocState VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_OftaDocState DEFAULT('');
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AdmDigitalSign') AND name = 'OftaSignState')
        ALTER TABLE BILRG_AdmDigitalSign ADD OftaSignState VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_OftaSignState DEFAULT('');
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AdmDigitalSign') AND name = 'OfficerRef')
        ALTER TABLE BILRG_AdmDigitalSign ADD OfficerRef VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_OfficerRef DEFAULT('');
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AdmDigitalSign') AND name = 'OfficerEmail')
        ALTER TABLE BILRG_AdmDigitalSign ADD OfficerEmail VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_OfficerEmail DEFAULT('');
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AdmDigitalSign') AND name = 'OfficerName')
        ALTER TABLE BILRG_AdmDigitalSign ADD OfficerName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_OfficerName DEFAULT('');
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AdmDigitalSign') AND name = 'ExternalDocumentId')
        ALTER TABLE BILRG_AdmDigitalSign ADD ExternalDocumentId VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_ExternalDocumentId DEFAULT('');
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AdmDigitalSign') AND name = 'SignedDocUrl')
        ALTER TABLE BILRG_AdmDigitalSign ADD SignedDocUrl VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_SignedDocUrl DEFAULT('');
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AdmDigitalSign') AND name = 'IsArchived')
        ALTER TABLE BILRG_AdmDigitalSign ADD IsArchived BIT NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_IsArchived DEFAULT(0);
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AdmDigitalSign') AND name = 'ArchiveId')
        ALTER TABLE BILRG_AdmDigitalSign ADD ArchiveId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_ArchiveId DEFAULT('');
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AdmDigitalSign') AND name = 'ArchiveDate')
        ALTER TABLE BILRG_AdmDigitalSign ADD ArchiveDate DATETIME NOT NULL CONSTRAINT DF_BILRG_AdmDigitalSign_ArchiveDate DEFAULT('3000-01-01');
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

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_AdmDigitalSign_OftaDocId'
      AND object_id = OBJECT_ID('BILRG_AdmDigitalSign'))
BEGIN
    CREATE INDEX IX_BILRG_AdmDigitalSign_OftaDocId
        ON BILRG_AdmDigitalSign (OftaDocId)
        WITH (FILLFACTOR = 90);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_AdmDigitalSign_ExternalDocumentId'
      AND object_id = OBJECT_ID('BILRG_AdmDigitalSign'))
BEGIN
    CREATE INDEX IX_BILRG_AdmDigitalSign_ExternalDocumentId
        ON BILRG_AdmDigitalSign (ExternalDocumentId)
        WITH (FILLFACTOR = 90);
END
GO
        