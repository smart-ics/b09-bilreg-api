IF COL_LENGTH('BILRG_TataRekPasienBalance', 'CurrentJasaBalance') IS NULL
BEGIN
    ALTER TABLE BILRG_TataRekPasienBalance
        ADD CurrentJasaBalance DECIMAL(18, 2) NOT NULL
            CONSTRAINT DF_BILRG_TataRekPasienBalance_CurrentJasaBalance DEFAULT(0),
            CurrentObatBalance DECIMAL(18, 2) NOT NULL
            CONSTRAINT DF_BILRG_TataRekPasienBalance_CurrentObatBalance DEFAULT(0);
END
GO

IF COL_LENGTH('BILRG_TataRekPasienBalance', 'CurrentBalance') IS NOT NULL
BEGIN
    UPDATE BILRG_TataRekPasienBalance
    SET CurrentJasaBalance = CurrentBalance,
        CurrentObatBalance = 0
    WHERE CurrentJasaBalance = 0 AND CurrentObatBalance = 0;

    ALTER TABLE BILRG_TataRekPasienBalance DROP CONSTRAINT DF_BILRG_TataRekPasienBalance_CurrentBalance;
    ALTER TABLE BILRG_TataRekPasienBalance DROP COLUMN CurrentBalance;
END
GO
