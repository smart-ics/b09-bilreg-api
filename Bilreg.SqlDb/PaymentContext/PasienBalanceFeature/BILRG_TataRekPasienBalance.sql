CREATE TABLE BILRG_TataRekPasienBalance
(
    PasienId            VARCHAR(15) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalance_PasienId DEFAULT(''),
    CurrentJasaBalance  DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalance_CurrentJasaBalance DEFAULT(0),
    CurrentObatBalance  DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalance_CurrentObatBalance DEFAULT(0),
    LastHistoryId       VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalance_LastHistoryId DEFAULT(''),
    UpdatedAt           DATETIME NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalance_UpdatedAt DEFAULT('3000-01-01'),
    Version             INT NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalance_Version DEFAULT(0),

    CrtUser             VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalance_CrtUser DEFAULT(''),
    CrtDate             DATETIME NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalance_CrtDate DEFAULT('3000-01-01'),
    UpdUser             VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalance_UpdUser DEFAULT(''),
    UpdDate             DATETIME NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalance_UpdDate DEFAULT('3000-01-01'),
    VodUser             VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalance_VodUser DEFAULT(''),
    VodDate             DATETIME NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalance_VodDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_TataRekPasienBalance PRIMARY KEY CLUSTERED (PasienId)
);
GO

CREATE INDEX IX_BILRG_TataRekPasienBalance_UpdatedAt
    ON BILRG_TataRekPasienBalance (UpdatedAt);
GO
