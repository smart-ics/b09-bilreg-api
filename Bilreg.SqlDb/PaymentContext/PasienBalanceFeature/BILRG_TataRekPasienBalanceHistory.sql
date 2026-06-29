CREATE TABLE BILRG_TataRekPasienBalanceHistory
(
    HistoryId           VARCHAR(12) NOT NULL,
    PasienId            VARCHAR(15) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_PasienId DEFAULT(''),
    RegId               VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_RegId DEFAULT(''),
    TrsDate             DATETIME NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_TrsDate DEFAULT('3000-01-01'),

    OpeningJasaBalance  DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_OpeningJasaBalance DEFAULT(0),
    OpeningObatBalance  DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_OpeningObatBalance DEFAULT(0),

    ChargeJasa          DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_ChargeJasa DEFAULT(0),
    ChargeObat          DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_ChargeObat DEFAULT(0),
    PaymentJasa         DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_PaymentJasa DEFAULT(0),
    PaymentObat         DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_PaymentObat DEFAULT(0),

    ClosingJasaBalance  DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_ClosingJasaBalance DEFAULT(0),
    ClosingObatBalance  DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_ClosingObatBalance DEFAULT(0),

    Remarks             VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_Remarks DEFAULT(''),
    CreatedAt           DATETIME NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_CreatedAt DEFAULT('3000-01-01'),
    CreatedBy           VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_TataRekPasienBalanceHistory_CreatedBy DEFAULT(''),

    CONSTRAINT PK_BILRG_TataRekPasienBalanceHistory PRIMARY KEY CLUSTERED (HistoryId)
);
GO

CREATE INDEX IX_BILRG_TataRekPasienBalanceHistory_PasienId_TrsDate
    ON BILRG_TataRekPasienBalanceHistory (PasienId, TrsDate, HistoryId);
GO
