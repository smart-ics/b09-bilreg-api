IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BILRG_LoketKasir' AND type = 'U')
BEGIN
    CREATE TABLE BILRG_LoketKasir (
        LoketId VARCHAR(10) NOT NULL,
        NamaLoket VARCHAR(50) NOT NULL,
        CoaId VARCHAR(20) NOT NULL,
        NamaCoa VARCHAR(100) NOT NULL,
        IsAktif BIT NOT NULL CONSTRAINT DF_BILRG_LoketKasir_IsAktif DEFAULT(1),
        Keterangan VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_LoketKasir_Keterangan DEFAULT(''),

        CONSTRAINT PK_BILRG_LoketKasir PRIMARY KEY CLUSTERED (LoketId)
    );
END
GO

-- Seeding data loket kasir rumah sakit
IF NOT EXISTS (SELECT 1 FROM BILRG_LoketKasir WHERE LoketId = 'LOK-INAP')
BEGIN
    INSERT INTO BILRG_LoketKasir (LoketId, NamaLoket, CoaId, NamaCoa, IsAktif, Keterangan)
    VALUES ('LOK-INAP', 'Loket Kasir Rawat Inap', '1110201', 'KAS KASIR RAWAT INAP', 1, 'Loket Kasir Gedung Rawat Inap');
END
GO

IF NOT EXISTS (SELECT 1 FROM BILRG_LoketKasir WHERE LoketId = 'LOK-IGD')
BEGIN
    INSERT INTO BILRG_LoketKasir (LoketId, NamaLoket, CoaId, NamaCoa, IsAktif, Keterangan)
    VALUES ('LOK-IGD', 'Loket Kasir IGD & Penunjang', '1110202', 'KAS KASIR IGD & PENUNJANG', 1, 'Loket Kasir Instalasi Gawat Darurat');
END
GO

IF NOT EXISTS (SELECT 1 FROM BILRG_LoketKasir WHERE LoketId = 'LOK-POLI')
BEGIN
    INSERT INTO BILRG_LoketKasir (LoketId, NamaLoket, CoaId, NamaCoa, IsAktif, Keterangan)
    VALUES ('LOK-POLI', 'Loket Kasir Poliklinik', '1110203', 'KAS KASIR POLIKLINIK', 1, 'Loket Kasir Gedung Rawat Jalan');
END
GO
