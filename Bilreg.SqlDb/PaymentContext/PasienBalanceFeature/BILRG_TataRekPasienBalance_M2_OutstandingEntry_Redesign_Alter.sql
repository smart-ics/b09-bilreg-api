IF OBJECT_ID('BILRG_TataRekPasienBalanceHistory', 'U') IS NOT NULL
BEGIN
    DROP TABLE BILRG_TataRekPasienBalanceHistory;
END
GO

IF COL_LENGTH('BILRG_TataRekPasienBalance', 'CurrentJasaBalance') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_TataRekPasienBalance DROP CONSTRAINT DF_BILRG_TataRekPasienBalance_CurrentJasaBalance;
    ALTER TABLE BILRG_TataRekPasienBalance DROP CONSTRAINT DF_BILRG_TataRekPasienBalance_CurrentObatBalance;
    ALTER TABLE BILRG_TataRekPasienBalance DROP COLUMN CurrentJasaBalance;
    ALTER TABLE BILRG_TataRekPasienBalance DROP COLUMN CurrentObatBalance;
END
GO

IF COL_LENGTH('BILRG_TataRekPasienBalance', 'LastHistoryId') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_TataRekPasienBalance DROP CONSTRAINT DF_BILRG_TataRekPasienBalance_LastHistoryId;
    ALTER TABLE BILRG_TataRekPasienBalance DROP COLUMN LastHistoryId;
END
GO

IF COL_LENGTH('BILRG_TataRekPasienBalance', 'UpdatedAt') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_TataRekPasienBalance DROP CONSTRAINT DF_BILRG_TataRekPasienBalance_UpdatedAt;
    ALTER TABLE BILRG_TataRekPasienBalance DROP COLUMN UpdatedAt;
END
GO

IF OBJECT_ID('BILRG_TataRekPasienBalanceOutstanding', 'U') IS NULL
BEGIN
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

    CREATE UNIQUE INDEX UX_BILRG_TataRekPasienBalanceOutstanding_PasienId_RegId
        ON BILRG_TataRekPasienBalanceOutstanding (PasienId, RegId);

    CREATE INDEX IX_BILRG_TataRekPasienBalanceOutstanding_PasienId
        ON BILRG_TataRekPasienBalanceOutstanding (PasienId);
END
GO
