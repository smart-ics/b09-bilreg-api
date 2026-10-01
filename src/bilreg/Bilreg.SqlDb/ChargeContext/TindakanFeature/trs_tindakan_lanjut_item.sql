CREATE TABLE trs_tindakan_lanjut_item (
    TindakanLanjutId    VARCHAR(26)   NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_item_TindakanLanjutId DEFAULT(''),
    ItemNo              INT           NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_item_ItemNo DEFAULT(0),
    ItemCode            VARCHAR(20)   NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_item_ItemCode DEFAULT(''),
    ItemName            VARCHAR(80)   NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_item_ItemName DEFAULT(''),
    Qty                 DECIMAL(18,2) NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_item_Qty DEFAULT(0),
    Note                VARCHAR(200)  NOT NULL CONSTRAINT DF_trs_tindakan_lanjut_item_Note DEFAULT(''),

    CONSTRAINT PK_trs_tindakan_lanjut_item PRIMARY KEY CLUSTERED (TindakanLanjutId, ItemNo)
);
GO
