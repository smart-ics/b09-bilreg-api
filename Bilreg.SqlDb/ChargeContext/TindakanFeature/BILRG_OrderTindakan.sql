CREATE TABLE BILRG_OrderTindakan (
    OrderId        VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_OrderId DEFAULT(''),
    OrderDate      DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_OrderDate DEFAULT('3000-01-01'),
    PpaId          VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_PpaId DEFAULT(''),
    PpaName        VARCHAR(60) NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_PpaName DEFAULT(''),
    RegId          VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_RegId DEFAULT(''),
    PasienId       VARCHAR(15) NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_PasienId DEFAULT(''),
    PasienName     VARCHAR(60) NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_PasienName DEFAULT(''),
    LayananId      VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_LayananId DEFAULT(''),
    LayananName    VARCHAR(40) NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_LayananName DEFAULT(''),
    StatusOrder    INT NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_StatusOrder DEFAULT(0),
    TarifId        VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_TarifId DEFAULT(''),
    TindakanName   VARCHAR(60) NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_TindakanName DEFAULT(''),
    CrtUser        VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_CrtUser DEFAULT(''),
    CrtDate        DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_CrtDate DEFAULT('3000-01-01'),
    UpdUser        VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_UpdUser DEFAULT(''),
    UpdDate        DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_UpdDate DEFAULT('3000-01-01'),
    VodUser        VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_VodUser DEFAULT(''),
    VodDate        DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderTindakan_VodDate DEFAULT('3000-01-01'),
    
    CONSTRAINT PK_BILRG_OrderTindakan PRIMARY KEY CLUSTERED (OrderId)
);