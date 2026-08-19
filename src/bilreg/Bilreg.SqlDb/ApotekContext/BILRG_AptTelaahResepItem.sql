CREATE TABLE BILRG_AptTelaahResepItem (
    TelaahResepId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_TelaahResepId DEFAULT(''),
    ItemNo INT NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_ItemNo DEFAULT(0),
    ResepKerjaItemNo INT NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_ResepKerjaItemNo DEFAULT(0),
    Disposition INT NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_Disposition DEFAULT(0),
    AcceptedBrgId VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_AcceptedBrgId DEFAULT(''),
    AcceptedBrgName VARCHAR(80) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_AcceptedBrgName DEFAULT(''),
    AcceptedQty DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_AcceptedQty DEFAULT(0),
    Reason VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_Reason DEFAULT(''),
    PharmacistId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_PharmacistId DEFAULT(''),
    CONSTRAINT PK_BILRG_AptTelaahResepItem PRIMARY KEY CLUSTERED (TelaahResepId, ItemNo)
);
GO
