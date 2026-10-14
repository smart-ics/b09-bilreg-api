IF OBJECT_ID('BILRG_AptTelaahResepItem') IS NULL
CREATE TABLE BILRG_AptTelaahResepItem (
    TelaahResepId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_TelaahResepId DEFAULT(''),
    ItemNo INT NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_ItemNo DEFAULT(0),
    ResepItemNo INT NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_ResepItemNo DEFAULT(0),
    PrescribedBrgId VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_PrescribedBrgId DEFAULT(''),
    PrescribedBrgName VARCHAR(80) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_PrescribedBrgName DEFAULT(''),
    PrescribedQty DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_PrescribedQty DEFAULT(0),
    Disposition INT NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_Disposition DEFAULT(0),
    AcceptedBrgId VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_AcceptedBrgId DEFAULT(''),
    AcceptedBrgName VARCHAR(80) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_AcceptedBrgName DEFAULT(''),
    AcceptedQty DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_AcceptedQty DEFAULT(0),
    Reason VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_Reason DEFAULT(''),
    PharmacistId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_PharmacistId DEFAULT(''),
    CONSTRAINT PK_BILRG_AptTelaahResepItem PRIMARY KEY CLUSTERED (TelaahResepId, ItemNo)
);
GO

IF COL_LENGTH('BILRG_AptTelaahResepItem', 'PrescribedBrgId') IS NULL
BEGIN
    ALTER TABLE BILRG_AptTelaahResepItem
        ADD PrescribedBrgId VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_PrescribedBrgId DEFAULT('');
END
GO

IF COL_LENGTH('BILRG_AptTelaahResepItem', 'PrescribedBrgName') IS NULL
BEGIN
    ALTER TABLE BILRG_AptTelaahResepItem
        ADD PrescribedBrgName VARCHAR(80) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_PrescribedBrgName DEFAULT('');
END
GO

IF COL_LENGTH('BILRG_AptTelaahResepItem', 'PrescribedQty') IS NULL
BEGIN
    ALTER TABLE BILRG_AptTelaahResepItem
        ADD PrescribedQty DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepItem_PrescribedQty DEFAULT(0);
END
GO
