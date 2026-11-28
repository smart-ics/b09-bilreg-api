-- BILRG_LoketKasir.dataseed.sql
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
