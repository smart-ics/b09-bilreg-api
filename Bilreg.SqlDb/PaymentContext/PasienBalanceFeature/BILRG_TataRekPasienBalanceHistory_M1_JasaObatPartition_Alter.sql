IF COL_LENGTH('BILRG_TataRekPasienBalanceHistory', 'OpeningJasaBalance') IS NULL
BEGIN
    ALTER TABLE BILRG_TataRekPasienBalanceHistory
        ADD OpeningJasaBalance DECIMAL(18, 2) NOT NULL
            CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_OpeningJasaBalance DEFAULT(0),
            OpeningObatBalance DECIMAL(18, 2) NOT NULL
            CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_OpeningObatBalance DEFAULT(0),
            ChargeJasa DECIMAL(18, 2) NOT NULL
            CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_ChargeJasa DEFAULT(0),
            ChargeObat DECIMAL(18, 2) NOT NULL
            CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_ChargeObat DEFAULT(0),
            PaymentJasa DECIMAL(18, 2) NOT NULL
            CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_PaymentJasa DEFAULT(0),
            PaymentObat DECIMAL(18, 2) NOT NULL
            CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_PaymentObat DEFAULT(0),
            ClosingJasaBalance DECIMAL(18, 2) NOT NULL
            CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_ClosingJasaBalance DEFAULT(0),
            ClosingObatBalance DECIMAL(18, 2) NOT NULL
            CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_ClosingObatBalance DEFAULT(0);
END
GO

IF COL_LENGTH('BILRG_TataRekPasienBalanceHistory', 'OpeningBalance') IS NOT NULL
BEGIN
    UPDATE BILRG_TataRekPasienBalanceHistory
    SET OpeningJasaBalance = OpeningBalance,
        ChargeJasa = ChargeAmount,
        PaymentJasa = PaymentAmount,
        ClosingJasaBalance = ClosingBalance
    WHERE OpeningJasaBalance = 0 AND OpeningObatBalance = 0
      AND ChargeJasa = 0 AND ChargeObat = 0;

    ALTER TABLE BILRG_TataRekPasienBalanceHistory DROP CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_OpeningBalance;
    ALTER TABLE BILRG_TataRekPasienBalanceHistory DROP CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_ChargeAmount;
    ALTER TABLE BILRG_TataRekPasienBalanceHistory DROP CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_PaymentAmount;
    ALTER TABLE BILRG_TataRekPasienBalanceHistory DROP CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_ClosingBalance;
    ALTER TABLE BILRG_TataRekPasienBalanceHistory DROP COLUMN OpeningBalance;
    ALTER TABLE BILRG_TataRekPasienBalanceHistory DROP COLUMN ChargeAmount;
    ALTER TABLE BILRG_TataRekPasienBalanceHistory DROP COLUMN PaymentAmount;
    ALTER TABLE BILRG_TataRekPasienBalanceHistory DROP COLUMN ClosingBalance;
END
GO
