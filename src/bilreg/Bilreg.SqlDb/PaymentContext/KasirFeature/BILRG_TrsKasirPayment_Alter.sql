-- Update data lama jika PaymentRowId masih berupa UUID (> 20 karakter)
IF EXISTS (SELECT 1 FROM BILRG_TrsKasirPayment WHERE LEN(PaymentRowId) > 20)
BEGIN
    UPDATE BILRG_TrsKasirPayment 
    SET PaymentRowId = TrsKasirId + '-' + RIGHT('0' + CAST(NoUrut AS VARCHAR(2)), 2)
    WHERE LEN(PaymentRowId) > 20;
END
GO

-- Ubah panjang kolom PaymentRowId menjadi VARCHAR(20) jika belum
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'BILRG_TrsKasirPayment' 
      AND COLUMN_NAME = 'PaymentRowId' 
      AND CHARACTER_MAXIMUM_LENGTH <> 20
)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_BILRG_TrsKasirPayment' AND parent_object_id = OBJECT_ID('BILRG_TrsKasirPayment'))
    BEGIN
        ALTER TABLE BILRG_TrsKasirPayment DROP CONSTRAINT PK_BILRG_TrsKasirPayment;
    END

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_BILRG_TrsKasirPayment_PaymentRowId' AND parent_object_id = OBJECT_ID('BILRG_TrsKasirPayment'))
    BEGIN
        ALTER TABLE BILRG_TrsKasirPayment DROP CONSTRAINT DF_BILRG_TrsKasirPayment_PaymentRowId;
    END

    ALTER TABLE BILRG_TrsKasirPayment ALTER COLUMN PaymentRowId VARCHAR(20) NOT NULL;

    ALTER TABLE BILRG_TrsKasirPayment ADD CONSTRAINT DF_BILRG_TrsKasirPayment_PaymentRowId DEFAULT('') FOR PaymentRowId;
    ALTER TABLE BILRG_TrsKasirPayment ADD CONSTRAINT PK_BILRG_TrsKasirPayment PRIMARY KEY CLUSTERED (PaymentRowId);
END
GO

-- Tambahkan kolom baru pendukung akuntansi dan pelaporan setoran kasir
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_TrsKasirPayment') AND name = 'KodeJenisPayment')
BEGIN
    ALTER TABLE BILRG_TrsKasirPayment ADD KodeJenisPayment VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_KodeJenisPayment DEFAULT('');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_TrsKasirPayment') AND name = 'NamaMetodeBayar')
BEGIN
    ALTER TABLE BILRG_TrsKasirPayment ADD NamaMetodeBayar VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_NamaMetodeBayar DEFAULT('');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_TrsKasirPayment') AND name = 'CoaId')
BEGIN
    ALTER TABLE BILRG_TrsKasirPayment ADD CoaId VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_CoaId DEFAULT('');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_TrsKasirPayment') AND name = 'NilaiNetto')
BEGIN
    ALTER TABLE BILRG_TrsKasirPayment ADD NilaiNetto DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TrsKasirPayment_NilaiNetto DEFAULT(0);
END
GO

-- Backfill NilaiNetto jika ada data lama yang nilainya masih 0
UPDATE BILRG_TrsKasirPayment
SET NilaiNetto = NilaiBayar - NilaiKembalian
WHERE NilaiNetto = 0 AND (NilaiBayar <> 0 OR NilaiKembalian <> 0);
GO
