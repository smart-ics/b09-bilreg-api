CREATE TABLE BILRG_MutasiBrgItem
(
    MutasiBrgId     VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_MutasiBrgId DEFAULT(''),
    NoUrut          INT             NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_NoUrut DEFAULT(0),
    BrgId           VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_BrgId DEFAULT(''),
    BrgName         VARCHAR(100)    NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_BrgName DEFAULT(''),
    BatchNo         VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_BatchNo DEFAULT(''),
    ExpiredDate     VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_ExpiredDate DEFAULT(''),
    QtyKirim        DECIMAL(18, 4)  NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_QtyKirim DEFAULT(0),
    SatuanId        VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_SatuanId DEFAULT(''),
    SatuanName      VARCHAR(50)     NOT NULL CONSTRAINT DF_BILRG_MutasiBrgItem_SatuanName DEFAULT(''),

    CONSTRAINT PK_BILRG_MutasiBrgItem PRIMARY KEY CLUSTERED (MutasiBrgId, NoUrut),
    CONSTRAINT FK_BILRG_MutasiBrgItem_MutasiBrg FOREIGN KEY (MutasiBrgId) REFERENCES BILRG_MutasiBrg(MutasiBrgId) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_BILRG_MutasiBrgItem_BrgId
    ON BILRG_MutasiBrgItem (BrgId);
GO
