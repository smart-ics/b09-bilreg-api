CREATE TABLE BILRG_TerimaMutasi
(
    TerimaMutasiId      VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_TerimaMutasiId DEFAULT(''),
    TerimaMutasiDate    DATETIME        NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_TerimaMutasiDate DEFAULT('3000-01-01'),
    State               INT             NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_State DEFAULT(0),

    MutasiBrgId         VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_MutasiBrgId DEFAULT(''),
    OrderMutasiId       VARCHAR(26)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_OrderMutasiId DEFAULT(''),

    LayananAsalId       VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_LayananAsalId DEFAULT(''),
    LayananAsalName     VARCHAR(100)    NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_LayananAsalName DEFAULT(''),
    LayananTujuanId     VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_LayananTujuanId DEFAULT(''),
    LayananTujuanName   VARCHAR(100)    NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_LayananTujuanName DEFAULT(''),

    ReceiverUserId      VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_ReceiverUserId DEFAULT(''),
    Note                VARCHAR(255)    NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_Note DEFAULT(''),

    CrtDate             DATETIME        NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_CrtDate DEFAULT('3000-01-01'),
    CrtUser             VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_CrtUser DEFAULT(''),
    UpdDate             DATETIME        NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_UpdDate DEFAULT('3000-01-01'),
    UpdUser             VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_TerimaMutasi_UpdUser DEFAULT(''),
    VodDate             DATETIME        NULL     CONSTRAINT DF_BILRG_TerimaMutasi_VodDate DEFAULT(NULL),
    VodUser             VARCHAR(50)     NULL     CONSTRAINT DF_BILRG_TerimaMutasi_VodUser DEFAULT(''),

    CONSTRAINT PK_BILRG_TerimaMutasi PRIMARY KEY CLUSTERED (TerimaMutasiId),
    CONSTRAINT FK_BILRG_TerimaMutasi_MutasiBrg FOREIGN KEY (MutasiBrgId) REFERENCES BILRG_MutasiBrg(MutasiBrgId)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_BILRG_TerimaMutasi_MutasiBrgId
    ON BILRG_TerimaMutasi (MutasiBrgId);
GO

CREATE NONCLUSTERED INDEX IX_BILRG_TerimaMutasi_Periode
    ON BILRG_TerimaMutasi (TerimaMutasiDate, LayananTujuanId);
GO
