IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('BILRG_OrderOpResource'))
BEGIN
    CREATE TABLE BILRG_OrderOpResource
    (
        OrderOpResourceId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Id DEFAULT(''),
        OrderOpId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_OrderOpId DEFAULT(''),
        ResourceType INT NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_ResourceType DEFAULT(1), -- 1: Darah, 2: Implan, 3: AlatKhusus
        ItemName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_ItemName DEFAULT(''),
        Jumlah INT NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Jumlah DEFAULT(1),
        Satuan VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Satuan DEFAULT('') ,
        Keterangan VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Keterangan DEFAULT(''),

        CONSTRAINT PK_BILRG_OrderOpResource PRIMARY KEY CLUSTERED(OrderOpResourceId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('BILRG_OrderOpResource') AND name = 'IX_BILRG_OrderOpResource_OrderOpId')
BEGIN
    CREATE INDEX IX_BILRG_OrderOpResource_OrderOpId
        ON BILRG_OrderOpResource(OrderOpId)
        WITH(FILLFACTOR=95);
END
GO
