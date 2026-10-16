IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('BILRG_OrderOp'))
BEGIN
    CREATE TABLE BILRG_OrderOp
    (
        OrderOpId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_OrderOp_OrderOpId DEFAULT(''),
        OrderDate DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderOp_OrderDate DEFAULT('3000-01-01'),
        PasienId VARCHAR(15) NOT NULL CONSTRAINT DF_BILRG_OrderOp_PasienId DEFAULT(''),
        NamaOperasi VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_OrderOp_NamaOperasi DEFAULT(''),

        RegId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_RegId DEFAULT(''),
        Icd10Id VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_OrderOp_Icd10Id DEFAULT(''),
        JenisOperasiId VARCHAR(2) NOT NULL CONSTRAINT DF_BILRG_OrderOp_JenisOperasiId DEFAULT(''),
        UrgencyLevel INT NOT NULL CONSTRAINT DF_BILRG_OrderOp_UrgencyLevel DEFAULT(0),
        TarifId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_TarifId DEFAULT(''),
        TarifName VARCHAR(60) NOT NULL CONSTRAINT DF_BILRG_OrderOp_TarifName DEFAULT(''),

        DokterId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_DokterId DEFAULT(''),
        EstimasiDurasi INT NOT NULL CONSTRAINT DF_BILRG_OrderOp_EstimasiDurasi DEFAULT(0),
        PreferedDate DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderOp_PreferedDate DEFAULT('3000-01-01'),
        SpecialEquipment VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_OrderOp_SpecialEquipment DEFAULT(''),

        DokterPengorderId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_DokterPengorderId DEFAULT(''),
        LayananPengorderId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_LayananPengorderId DEFAULT(''),
        DokterOperatorId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_DokterOperatorId DEFAULT(''),
        ProsedurTambahan VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_OrderOp_ProsedurTambahan DEFAULT(''),
        KlasifikasiOperasi INT NOT NULL CONSTRAINT DF_BILRG_OrderOp_KlasifikasiOperasi DEFAULT(1),
        AnestesiPlan INT NOT NULL CONSTRAINT DF_BILRG_OrderOp_AnestesiPlan DEFAULT(1),
        PostOpCareUnit INT NOT NULL CONSTRAINT DF_BILRG_OrderOp_PostOpCareUnit DEFAULT(1),
        AlasanBatal VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_OrderOp_AlasanBatal DEFAULT(''),

        CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_OrderOp_CreateUserId DEFAULT(''),
        CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderOp_CreateTimestamp DEFAULT('3000-01-01'),
        UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_OrderOp_UpdateUserId DEFAULT(''),
        UpdDate DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderOp_UpdateTimestamp DEFAULT('3000-01-01'),
        VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_OrderOp_VoidUserId DEFAULT(''),
        VodDate DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderOp_VoidTimestamp DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_OrderOp PRIMARY KEY CLUSTERED(OrderOpId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'IX_BILRG_OrderOp_OrderDate')
BEGIN
    CREATE INDEX IX_BILRG_OrderOp_OrderDate
        ON BILRG_OrderOp (OrderDate, OrderOpId)
        WITH(FILLFACTOR=98);
END
GO

-- Penambahan Kolom Tanggung Jawab Medis & Unit
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'DokterPengorderId')
    ALTER TABLE BILRG_OrderOp ADD DokterPengorderId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_DokterPengorderId DEFAULT('');

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'LayananPengorderId')
    ALTER TABLE BILRG_OrderOp ADD LayananPengorderId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_LayananPengorderId DEFAULT('');

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'DokterOperatorId')
    ALTER TABLE BILRG_OrderOp ADD DokterOperatorId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_DokterOperatorId DEFAULT('');

-- Penambahan Rincian Prosedur & Kompleksitas
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'ProsedurTambahan')
    ALTER TABLE BILRG_OrderOp ADD ProsedurTambahan VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_OrderOp_ProsedurTambahan DEFAULT('');

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'KlasifikasiOperasi')
    ALTER TABLE BILRG_OrderOp ADD KlasifikasiOperasi INT NOT NULL CONSTRAINT DF_BILRG_OrderOp_KlasifikasiOperasi DEFAULT(1);

-- Penambahan Rencana Anestesi & Bed Pasca-Bedah
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'AnestesiPlan')
    ALTER TABLE BILRG_OrderOp ADD AnestesiPlan INT NOT NULL CONSTRAINT DF_BILRG_OrderOp_AnestesiPlan DEFAULT(1);

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'PostOpCareUnit')
    ALTER TABLE BILRG_OrderOp ADD PostOpCareUnit INT NOT NULL CONSTRAINT DF_BILRG_OrderOp_PostOpCareUnit DEFAULT(1);

-- Penambahan Alasan Pembatalan
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'AlasanBatal')
    ALTER TABLE BILRG_OrderOp ADD AlasanBatal VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_OrderOp_AlasanBatal DEFAULT('');
GO
