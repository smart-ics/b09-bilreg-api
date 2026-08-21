CREATE TABLE BILRG_AptResepKerjaComponent (
    ResepKerjaId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AptResepKerjaComponent_ResepKerjaId DEFAULT(''),
    ItemNo INT NOT NULL CONSTRAINT DF_BILRG_AptResepKerjaComponent_ItemNo DEFAULT(0),
    ComponentNo INT NOT NULL CONSTRAINT DF_BILRG_AptResepKerjaComponent_ComponentNo DEFAULT(0),
    BrgId VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_AptResepKerjaComponent_BrgId DEFAULT(''),
    BrgName VARCHAR(80) NOT NULL CONSTRAINT DF_BILRG_AptResepKerjaComponent_BrgName DEFAULT(''),
    SatuanId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_AptResepKerjaComponent_SatuanId DEFAULT(''),
    Qty DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_AptResepKerjaComponent_Qty DEFAULT(0),
    CONSTRAINT PK_BILRG_AptResepKerjaComponent PRIMARY KEY CLUSTERED (ResepKerjaId, ItemNo, ComponentNo)
);
GO
