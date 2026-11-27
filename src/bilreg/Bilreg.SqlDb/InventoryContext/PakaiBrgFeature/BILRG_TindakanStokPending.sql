CREATE TABLE BILRG_TindakanStokPending
(
    PendingId           VARCHAR(26)   NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_PendingId DEFAULT(''),
    TindakanId          VARCHAR(26)   NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_TindakanId DEFAULT(''),
    TindakanBarangId    VARCHAR(26)   NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_TindakanBarangId DEFAULT(''),
    NoUrut              INT           NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_NoUrut DEFAULT(0),
    RegId               VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_RegId DEFAULT(''),
    LayananId           VARCHAR(5)    NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_LayananId DEFAULT(''),
    BrgId               VARCHAR(13)   NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_BrgId DEFAULT(''),
    SatuanKecilId       VARCHAR(5)    NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_SatuanKecilId DEFAULT(''),
    QtyPending          DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_QtyPending DEFAULT(0),
    QtySettled          DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_QtySettled DEFAULT(0),
    StatusPotStok       BIT           NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_StatusPotStok DEFAULT(0),
    PakaiBrgId          VARCHAR(13)   NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_PakaiBrgId DEFAULT(''),
    IsVoid              BIT           NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_IsVoid DEFAULT(0),
    SettledDate         DATETIME      NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_SettledDate DEFAULT('3000-01-01'),
    VoidDate            DATETIME      NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_VoidDate DEFAULT('3000-01-01'),

    CrtUser             VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_CrtUser DEFAULT(''),
    CrtDate             DATETIME      NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_CrtDate DEFAULT('3000-01-01'),
    UpdUser             VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_UpdUser DEFAULT(''),
    UpdDate             DATETIME      NOT NULL CONSTRAINT DF_BILRG_TindakanStokPending_UpdDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_TindakanStokPending PRIMARY KEY CLUSTERED (PendingId)
);
GO

CREATE NONCLUSTERED INDEX IX_BILRG_TindakanStokPending_FifoAktif 
    ON BILRG_TindakanStokPending (LayananId, BrgId, StatusPotStok, IsVoid, CrtDate);
GO
