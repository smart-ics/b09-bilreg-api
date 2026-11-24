-- ============================================================================
-- Tabel Fisik: BILRG_OrderOpResource
-- Bounded Context: KamarOperasiFeature (KMO-JADWAL / KMO-ORDER)
-- Deskripsi: Menyimpan kebutuhan sumber daya penunjang operasi (Darah, Implan, Alkes Khusus)
-- Standar: DATABASE.md §4, §5, §6, §10, §12
-- Idempotent Migration Script
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BILRG_OrderOpResource')
BEGIN
    CREATE TABLE BILRG_OrderOpResource
    (
        OrderOpResourceId VARCHAR(36)  NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_OrderOpResourceId DEFAULT(''),
        OrderOpId         VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_OrderOpId         DEFAULT(''),
        ScheduleOpId      VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_ScheduleOpId      DEFAULT(''),
        ResourceType      INT          NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_ResourceType      DEFAULT(1),
        Category          VARCHAR(30)  NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Category          DEFAULT(''),
        ItemName          VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_ItemName          DEFAULT(''),
        Jumlah            INT          NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Jumlah            DEFAULT(1),
        Satuan            VARCHAR(20)  NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Satuan            DEFAULT(''),
        Keterangan        VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Keterangan        DEFAULT(''),

        CrtUser           VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_CrtUser           DEFAULT(''),
        CrtDate           DATETIME     NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_CrtDate           DEFAULT('3000-01-01'),
        UpdUser           VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_UpdUser           DEFAULT(''),
        UpdDate           DATETIME     NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_UpdDate           DEFAULT('3000-01-01'),
        VodUser           VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_VodUser           DEFAULT(''),
        VodDate           DATETIME     NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_VodDate           DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_OrderOpResource PRIMARY KEY CLUSTERED(OrderOpResourceId)
    );
END
ELSE
BEGIN
    -- Tambahkan kolom ScheduleOpId jika belum ada
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOpResource') AND name = 'ScheduleOpId')
    BEGIN
        ALTER TABLE BILRG_OrderOpResource 
            ADD ScheduleOpId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_ScheduleOpId DEFAULT('');
    END;

    -- Tambahkan kolom Category jika belum ada
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOpResource') AND name = 'Category')
    BEGIN
        ALTER TABLE BILRG_OrderOpResource 
            ADD Category VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_Category DEFAULT('');
    END;

    -- Tambahkan kolom audit CrtUser jika belum ada
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOpResource') AND name = 'CrtUser')
    BEGIN
        ALTER TABLE BILRG_OrderOpResource 
            ADD CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_CrtUser DEFAULT('');
        ALTER TABLE BILRG_OrderOpResource 
            ADD CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_CrtDate DEFAULT('3000-01-01');
        ALTER TABLE BILRG_OrderOpResource 
            ADD UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_UpdUser DEFAULT('');
        ALTER TABLE BILRG_OrderOpResource 
            ADD UpdDate DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_UpdDate DEFAULT('3000-01-01');
        ALTER TABLE BILRG_OrderOpResource 
            ADD VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_VodUser DEFAULT('');
        ALTER TABLE BILRG_OrderOpResource 
            ADD VodDate DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderOpResource_VodDate DEFAULT('3000-01-01');
    END;
END;
GO

-- Backfill Category untuk data existing
UPDATE BILRG_OrderOpResource
SET Category = CASE ResourceType
    WHEN 1 THEN 'BLOOD'
    WHEN 2 THEN 'IMPLANT'
    WHEN 3 THEN 'SPECIAL_EQUIPMENT'
    ELSE 'OTHER'
END
WHERE Category = '' OR Category IS NULL;
GO

-- Index penunjang kueri OrderOpId
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_OrderOpResource_OrderOpId' AND object_id = OBJECT_ID('BILRG_OrderOpResource'))
BEGIN
    CREATE INDEX IX_BILRG_OrderOpResource_OrderOpId
        ON BILRG_OrderOpResource (OrderOpId, VodDate)
        INCLUDE (OrderOpResourceId, ScheduleOpId, ResourceType, Category, ItemName, Jumlah, Satuan)
        WITH (FILLFACTOR = 95);
END;
GO

-- Index penunjang kueri ScheduleOpId
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_OrderOpResource_ScheduleOpId' AND object_id = OBJECT_ID('BILRG_OrderOpResource'))
BEGIN
    CREATE INDEX IX_BILRG_OrderOpResource_ScheduleOpId
        ON BILRG_OrderOpResource (ScheduleOpId, VodDate)
        INCLUDE (OrderOpResourceId, OrderOpId)
        WITH (FILLFACTOR = 95);
END;
GO
