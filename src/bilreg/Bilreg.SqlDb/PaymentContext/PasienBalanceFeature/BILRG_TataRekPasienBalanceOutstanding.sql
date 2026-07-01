CREATE TABLE BILRG_TataRekPasienBalanceOutstanding
(
    EntryId             VARCHAR(12) NOT NULL,
    PasienId            VARCHAR(15) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceOutstanding_PasienId DEFAULT(''),
    RegId               VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceOutstanding_RegId DEFAULT(''),
    OutstandingJasa     DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceOutstanding_OutstandingJasa DEFAULT(0),
    OutstandingObat     DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceOutstanding_OutstandingObat DEFAULT(0),
    LastTransactionDate DATETIME NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceOutstanding_LastTransactionDate DEFAULT('3000-01-01'),
    SourceReference     VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceOutstanding_SourceReference DEFAULT(''),
    CreatedAt           DATETIME NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceOutstanding_CreatedAt DEFAULT('3000-01-01'),
    CreatedBy           VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceOutstanding_CreatedBy DEFAULT(''),

    CONSTRAINT PK_BILRG_TataRekPasienBalanceOutstanding PRIMARY KEY CLUSTERED (EntryId)
);
GO

CREATE UNIQUE INDEX UX_BILRG_TataRekPasienBalanceOutstanding_PasienId_RegId
    ON BILRG_TataRekPasienBalanceOutstanding (PasienId, RegId);
GO

CREATE INDEX IX_BILRG_TataRekPasienBalanceOutstanding_PasienId
    ON BILRG_TataRekPasienBalanceOutstanding (PasienId);
GO
