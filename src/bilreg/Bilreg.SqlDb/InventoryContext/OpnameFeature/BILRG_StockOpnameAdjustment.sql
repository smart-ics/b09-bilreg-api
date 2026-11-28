CREATE TABLE BILRG_StockOpnameAdjustment
(
    AdjustmentId        VARCHAR(12)     NOT NULL,
    SessionId           VARCHAR(30)     NOT NULL,
    BrgId               VARCHAR(13)     NOT NULL,
    BatchNo             VARCHAR(30)     NOT NULL,
    ExpiredDate         VARCHAR(10)     NOT NULL,
    LayananId           VARCHAR(10)     NOT NULL,
    AdjustmentQty       DECIMAL(18, 0)  NOT NULL,
    BaseSatuanId        VARCHAR(10)     NOT NULL,
    Hpp                 DECIMAL(18, 2)  NOT NULL,
    NilaiFinansial      DECIMAL(18, 2)  NOT NULL,
    StokMutasiId        VARCHAR(12)     NOT NULL,
    LegacyBukuId        VARCHAR(10)     NOT NULL,
    MovementKind        INT             NOT NULL,
    ExecutedAt          DATETIME        NOT NULL,
    ExecutedBy          VARCHAR(50)     NOT NULL,

    CONSTRAINT PK_BILRG_StockOpnameAdjustment PRIMARY KEY CLUSTERED (AdjustmentId)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_BILRG_StockOpnameAdjustment_SessionTuple
    ON BILRG_StockOpnameAdjustment (SessionId, BrgId, BatchNo, ExpiredDate);
GO

CREATE NONCLUSTERED INDEX IX_BILRG_StockOpnameAdjustment_SessionId
    ON BILRG_StockOpnameAdjustment (SessionId);
GO
