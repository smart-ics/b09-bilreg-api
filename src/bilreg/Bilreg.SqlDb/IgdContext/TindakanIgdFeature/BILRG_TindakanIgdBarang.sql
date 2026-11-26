CREATE TABLE BILRG_TindakanIgdBarang (
    TindakanIgdId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_TindakanIgdId DEFAULT(''),
    NoUrut INT NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_NoUrut DEFAULT(0),
    BrgId VARCHAR(13) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_BrgId DEFAULT(''),
    SatuanBomId VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_SatuanBomId DEFAULT(''),
    QtyBom DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_QtyBom DEFAULT(0),
    SatuanKecilId VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_SatuanKecilId DEFAULT(''),
    QtyKecilRequested DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_QtyKecilRequested DEFAULT(0),
    QtyKecilDeducted DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_QtyKecilDeducted DEFAULT(0),
    QtyKecilPending DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_QtyKecilPending DEFAULT(0),
    IsChargeBarang BIT NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_IsChargeBarang DEFAULT(0),
    PakaiBrgId VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_PakaiBrgId DEFAULT(''),
    TrsBillingId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_TrsBillingId DEFAULT(''),
    IsVoid BIT NOT NULL CONSTRAINT DF_BILRG_TindakanIgdBarang_IsVoid DEFAULT(0),

    CONSTRAINT PK_BILRG_TindakanIgdBarang PRIMARY KEY CLUSTERED (TindakanIgdId, NoUrut)
);
GO

CREATE INDEX IX_BILRG_TindakanIgdBarang_TindakanIgdId ON BILRG_TindakanIgdBarang(TindakanIgdId);
GO

CREATE INDEX IX_BILRG_TindakanIgdBarang_PakaiBrgId ON BILRG_TindakanIgdBarang(PakaiBrgId);
GO
