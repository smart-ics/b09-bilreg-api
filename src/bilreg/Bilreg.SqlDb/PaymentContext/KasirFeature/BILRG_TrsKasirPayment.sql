CREATE TABLE BILRG_TrsKasirPayment (
    PaymentRowId VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_PaymentRowId DEFAULT(''),
    TrsKasirId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_TrsKasirId DEFAULT(''),
    NoUrut INT NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_NoUrut DEFAULT(1),
    MetodeBayar VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_MetodeBayar DEFAULT(''),
    KodeJenisPayment VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_KodeJenisPayment DEFAULT(''),
    NamaMetodeBayar VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_NamaMetodeBayar DEFAULT(''),
    CoaId VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_CoaId DEFAULT(''),
    NilaiBayar DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_NilaiBayar DEFAULT(0),
    NilaiKembalian DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_NilaiKembalian DEFAULT(0),
    NilaiNetto DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_NilaiNetto DEFAULT(0),
    NomorKartu VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_NomorKartu DEFAULT(''),
    NomorReferensi VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_NomorReferensi DEFAULT(''),
    NamaBank VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_NamaBank DEFAULT(''),

    CONSTRAINT PK_BILRG_TrsKasirPayment PRIMARY KEY CLUSTERED (PaymentRowId)
);
GO

CREATE NONCLUSTERED INDEX IX_BILRG_TrsKasirPayment_TrsKasirId
    ON BILRG_TrsKasirPayment (TrsKasirId, NoUrut);
GO
