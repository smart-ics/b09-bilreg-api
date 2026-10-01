CREATE TABLE trs_tujuan_lanjut (
    OrderType           VARCHAR(20)   NOT NULL CONSTRAINT DF_trs_tujuan_lanjut_OrderType DEFAULT(''),
    LayananId           VARCHAR(5)    NOT NULL CONSTRAINT DF_trs_tujuan_lanjut_LayananId DEFAULT(''),
    LayananName         VARCHAR(40)   NOT NULL CONSTRAINT DF_trs_tujuan_lanjut_LayananName DEFAULT(''),
    IsActive            BIT           NOT NULL CONSTRAINT DF_trs_tujuan_lanjut_IsActive DEFAULT(1),

    CONSTRAINT PK_trs_tujuan_lanjut PRIMARY KEY CLUSTERED (OrderType)
);
GO
