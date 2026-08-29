CREATE TABLE BILRG_AptInvoiceItemCharge (
    InvoiceId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AptInvoiceItemCharge_InvoiceId DEFAULT(''),
    ItemNo INT NOT NULL CONSTRAINT DF_BILRG_AptInvoiceItemCharge_ItemNo DEFAULT(0),
    ChargeNo INT NOT NULL CONSTRAINT DF_BILRG_AptInvoiceItemCharge_ChargeNo DEFAULT(0),
    ChargeName VARCHAR(80) NOT NULL CONSTRAINT DF_BILRG_AptInvoiceItemCharge_ChargeName DEFAULT(''),
    Amount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_AptInvoiceItemCharge_Amount DEFAULT(0),
    CONSTRAINT PK_BILRG_AptInvoiceItemCharge PRIMARY KEY CLUSTERED (InvoiceId, ItemNo, ChargeNo)
);
GO
