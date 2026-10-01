CREATE TABLE trs_tindakan_lanjut (
    TindakanLanjutId    VARCHAR(26)   NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_TindakanLanjutId DEFAULT(''),
    TindakanLanjutDate  DATETIME      NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_TindakanLanjutDate DEFAULT('3000-01-01'),

    RegId               VARCHAR(10)   NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_RegId DEFAULT(''),
    PasienId            VARCHAR(15)   NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_PasienId DEFAULT(''),
    PasienName          VARCHAR(60)   NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_PasienName DEFAULT(''),

    OrderType           VARCHAR(20)   NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_OrderType DEFAULT(''),
    LayananId           VARCHAR(5)    NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_LayananId DEFAULT(''),
    LayananName         VARCHAR(40)   NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_LayananName DEFAULT(''),

    OrderState          INT           NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_OrderState DEFAULT(0),
    SentDate            DATETIME      NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_SentDate DEFAULT('3000-01-01'),
    ReceivedDate        DATETIME      NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_ReceivedDate DEFAULT('3000-01-01'),
    ReceivedBy          VARCHAR(50)   NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_ReceivedBy DEFAULT(''),
    CancelReason        VARCHAR(200)  NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_CancelReason DEFAULT(''),
    RowVersion          INT           NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_RowVersion DEFAULT(0),

    CrtUser             VARCHAR(50)   NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_CrtUser DEFAULT(''),
    CrtDate             DATETIME      NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_CrtDate DEFAULT('3000-01-01'),
    UpdUser             VARCHAR(50)   NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_UpdUser DEFAULT(''),
    UpdDate             DATETIME      NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_UpdDate DEFAULT('3000-01-01'),
    VodUser             VARCHAR(50)   NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_VodUser DEFAULT(''),
    VodDate             DATETIME      NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_VodDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_trs_tindakan_lanjut PRIMARY KEY CLUSTERED (TindakanLanjutId),
    CONSTRAINT CK_trs_tindakan_lanjut_OrderState CHECK (OrderState IN (0, 1, 2, 3))
);
GO

CREATE INDEX IX_trs_tindakan_lanjut_Reg ON trs_tindakan_lanjut(RegId, OrderState) WHERE RegId <> '';
GO

CREATE INDEX IX_trs_tindakan_lanjut_Outstanding ON trs_tindakan_lanjut(OrderState, CrtDate);
GO
