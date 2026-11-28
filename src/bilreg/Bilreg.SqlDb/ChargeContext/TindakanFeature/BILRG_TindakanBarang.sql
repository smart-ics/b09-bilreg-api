CREATE TABLE BILRG_TindakanBarang
(
    TindakanBarangId    VARCHAR(26)   NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_TindakanBarangId DEFAULT(''),
    TindakanId          VARCHAR(26)   NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_TindakanId DEFAULT(''),
    NoUrut              INT           NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_NoUrut DEFAULT(0),
    BrgId               VARCHAR(13)   NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_BrgId DEFAULT(''),
    SatuanBomId         VARCHAR(5)    NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_SatuanBomId DEFAULT(''),
    QtyBom              DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_QtyBom DEFAULT(0),
    SatuanKecilId       VARCHAR(5)    NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_SatuanKecilId DEFAULT(''),
    QtyKecilRequested   DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_QtyKecilRequested DEFAULT(0),
    QtyKecilDeducted    DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_QtyKecilDeducted DEFAULT(0),
    QtyKecilPending     DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_QtyKecilPending DEFAULT(0),
    IsChargeBarang      BIT           NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_IsChargeBarang DEFAULT(0),
    HargaJualTotal      DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_HargaJualTotal DEFAULT(0),
    PakaiBrgId          VARCHAR(13)   NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_PakaiBrgId DEFAULT(''),
    TrsBillingId        VARCHAR(26)   NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_TrsBillingId DEFAULT(''),
    IsVoid              BIT           NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_IsVoid DEFAULT(0),

    CRTTGL              VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_CRTTGL DEFAULT('3000-01-01'),
    CRTJAM              VARCHAR(8)    NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_CRTJAM DEFAULT('00:00:00'),
    CRTIPA              VARCHAR(40)   NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_CRTIPA DEFAULT(''),
    CRTVER              VARCHAR(12)   NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_CRTVER DEFAULT(''),
    CRTUSR              VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_CRTUSR DEFAULT(''),
    UPDTGL              VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_UPDTGL DEFAULT('3000-01-01'),
    UPDJAM              VARCHAR(8)    NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_UPDJAM DEFAULT('00:00:00'),
    UPDIPA              VARCHAR(40)   NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_UPDIPA DEFAULT(''),
    UPDVER              VARCHAR(12)   NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_UPDVER DEFAULT(''),
    UPDUSR              VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_TindakanBarang_UPDUSR DEFAULT(''),

    CONSTRAINT PK_BILRG_TindakanBarang PRIMARY KEY CLUSTERED (TindakanBarangId)
);
GO

CREATE NONCLUSTERED INDEX IX_BILRG_TindakanBarang_TindakanId 
    ON BILRG_TindakanBarang (TindakanId, NoUrut);
GO

CREATE NONCLUSTERED INDEX IX_BILRG_TindakanBarang_PakaiBrgId 
    ON BILRG_TindakanBarang (PakaiBrgId);
GO
