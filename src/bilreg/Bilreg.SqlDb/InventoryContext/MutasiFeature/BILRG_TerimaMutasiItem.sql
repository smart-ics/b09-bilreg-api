CREATE TABLE BILRG_TerimaMutasiItem
(
    TerimaMutasiId  VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_TerimaMutasiId DEFAULT(''),
    NoUrut          INT             NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_NoUrut DEFAULT(0),
    BrgId           VARCHAR(13)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_BrgId DEFAULT(''),
    BrgName         VARCHAR(100)    NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_BrgName DEFAULT(''),
    BatchNo         VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_BatchNo DEFAULT(''),
    ExpiredDate     VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_ExpiredDate DEFAULT(''),
    QtyTerima       DECIMAL(18, 4)  NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_QtyTerima DEFAULT(0),
    SatuanId        VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_SatuanId DEFAULT(''),
    SatuanName      VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasiItem_SatuanName DEFAULT(''),

    CONSTRAINT PK_BILRG_TerimaMutasiItem PRIMARY KEY CLUSTERED (TerimaMutasiId, NoUrut),
    CONSTRAINT FK_BILRG_TerimaMutasiItem_TerimaMutasi FOREIGN KEY (TerimaMutasiId) REFERENCES BILRG_TerimaMutasi(TerimaMutasiId) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_BILRG_TerimaMutasiItem_BrgId
    ON BILRG_TerimaMutasiItem (BrgId);
GO
