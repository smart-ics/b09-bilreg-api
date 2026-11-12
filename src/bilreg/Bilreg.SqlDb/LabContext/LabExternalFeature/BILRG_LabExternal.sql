CREATE TABLE BILRG_LabExternal (
    RegExternalId       VARCHAR(15)     NOT NULL CONSTRAINT DF_BILRG_LabExternal_RegExternalId DEFAULT(''),
    ExternalMrNo        VARCHAR(20)     NOT NULL CONSTRAINT DF_BILRG_LabExternal_ExternalMrNo DEFAULT(''),
    PatientName         VARCHAR(100)    NOT NULL CONSTRAINT DF_BILRG_LabExternal_PatientName DEFAULT(''),
    Nik                 VARCHAR(20)     NOT NULL CONSTRAINT DF_BILRG_LabExternal_Nik DEFAULT(''),
    BirthDate           DATETIME        NOT NULL CONSTRAINT DF_BILRG_LabExternal_BirthDate DEFAULT('3000-01-01'),
    Gender              VARCHAR(1)      NOT NULL CONSTRAINT DF_BILRG_LabExternal_Gender DEFAULT(''),
    Phone               VARCHAR(30)     NOT NULL CONSTRAINT DF_BILRG_LabExternal_Phone DEFAULT(''),
    Address             VARCHAR(250)    NOT NULL CONSTRAINT DF_BILRG_LabExternal_Address DEFAULT(''),
    
    ReferralType        INT             NOT NULL CONSTRAINT DF_BILRG_LabExternal_ReferralType DEFAULT(1),
    ReferralDoctor      VARCHAR(100)    NOT NULL CONSTRAINT DF_BILRG_LabExternal_ReferralDoctor DEFAULT(''),
    ReferralOrigin      VARCHAR(100)    NOT NULL CONSTRAINT DF_BILRG_LabExternal_ReferralOrigin DEFAULT(''),
    ClinicalNotes       VARCHAR(250)    NOT NULL CONSTRAINT DF_BILRG_LabExternal_ClinicalNotes DEFAULT(''),
    
    BillingAccountId    VARCHAR(15)     NOT NULL CONSTRAINT DF_BILRG_LabExternal_BillingAccountId DEFAULT(''),
    Status              INT             NOT NULL CONSTRAINT DF_BILRG_LabExternal_Status DEFAULT(1),
    
    CancelReason        VARCHAR(250)    NOT NULL CONSTRAINT DF_BILRG_LabExternal_CancelReason DEFAULT(''),
    CancelledUserId     VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_LabExternal_CancelledUserId DEFAULT(''),
    CancelledDate       DATETIME        NOT NULL CONSTRAINT DF_BILRG_LabExternal_CancelledDate DEFAULT('3000-01-01'),
    
    CrtUser             VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_LabExternal_CrtUser DEFAULT(''),
    CrtDate             DATETIME        NOT NULL CONSTRAINT DF_BILRG_LabExternal_CrtDate DEFAULT('3000-01-01'),
    UpdUser             VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_LabExternal_UpdUser DEFAULT(''),
    UpdDate             DATETIME        NOT NULL CONSTRAINT DF_BILRG_LabExternal_UpdDate DEFAULT('3000-01-01'),
    VodUser             VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_LabExternal_VodUser DEFAULT(''),
    VodDate             DATETIME        NOT NULL CONSTRAINT DF_BILRG_LabExternal_VodDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_LabExternal PRIMARY KEY CLUSTERED (RegExternalId)
);
GO

-- Indeks Keunikan Nomor Rekam Medis Operasional Sekali Pakai
CREATE UNIQUE NONCLUSTERED INDEX IX_BILRG_LabExternal_ExternalMrNo 
ON BILRG_LabExternal(ExternalMrNo);
GO

-- Indeks Pencarian Omni-Search (Nama & Nomor Telepon)
CREATE NONCLUSTERED INDEX IX_BILRG_LabExternal_Search 
ON BILRG_LabExternal(PatientName, Phone, Status);
GO

-- Indeks Filter Status & Tanggal Pendaftaran
CREATE NONCLUSTERED INDEX IX_BILRG_LabExternal_StatusDate 
ON BILRG_LabExternal(Status, CrtDate);
GO
