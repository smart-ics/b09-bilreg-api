-- tb_barang_layanan.sql
-- Table: tb_barang_layanan (Konfigurasi Buffer Stok & Lokasi Rak Barang per Unit Layanan)
-- Legacy table documentation for Logistik Medis Live Stock Canvas (ISSUE-LOGISTIK-MEDIS-001)

IF OBJECT_ID('tb_barang_layanan', 'U') IS NULL
BEGIN
    CREATE TABLE tb_barang_layanan
    (
        fn_no_urut      INT             NOT NULL CONSTRAINT DF_tb_barang_layanan_fn_no_urut DEFAULT(0),
        fs_kd_barang    VARCHAR(13)     NOT NULL CONSTRAINT DF_tb_barang_layanan_fs_kd_barang DEFAULT(''),
        fs_kd_layanan   VARCHAR(5)      NOT NULL CONSTRAINT DF_tb_barang_layanan_fs_kd_layanan DEFAULT(''),
        fn_stok_min     DECIMAL(18, 2)  NOT NULL CONSTRAINT DF_tb_barang_layanan_fn_stok_min DEFAULT(0),
        fn_stok_max     DECIMAL(18, 2)  NOT NULL CONSTRAINT DF_tb_barang_layanan_fn_stok_max DEFAULT(0),
        fs_kd_rak       VARCHAR(10)     NOT NULL CONSTRAINT DF_tb_barang_layanan_fs_kd_rak DEFAULT(''),
        fs_ket_lokasi   VARCHAR(100)    NOT NULL CONSTRAINT DF_tb_barang_layanan_fs_ket_lokasi DEFAULT(''),

        CRTTGL          VARCHAR(10)     NOT NULL CONSTRAINT DF_tb_barang_layanan_CRTTGL DEFAULT(''),
        CRTJAM          VARCHAR(8)      NOT NULL CONSTRAINT DF_tb_barang_layanan_CRTJAM DEFAULT(''),
        CRTIPA          VARCHAR(40)     NOT NULL CONSTRAINT DF_tb_barang_layanan_CRTIPA DEFAULT(''),
        CRTVER          VARCHAR(12)     NOT NULL CONSTRAINT DF_tb_barang_layanan_CRTVER DEFAULT(''),
        CRTUSR          VARCHAR(10)     NOT NULL CONSTRAINT DF_tb_barang_layanan_CRTUSR DEFAULT(''),
        UPDTGL          VARCHAR(10)     NOT NULL CONSTRAINT DF_tb_barang_layanan_UPDTGL DEFAULT(''),
        UPDJAM          VARCHAR(8)      NOT NULL CONSTRAINT DF_tb_barang_layanan_UPDJAM DEFAULT(''),
        UPDIPA          VARCHAR(40)     NOT NULL CONSTRAINT DF_tb_barang_layanan_UPDIPA DEFAULT(''),
        UPDVER          VARCHAR(12)     NOT NULL CONSTRAINT DF_tb_barang_layanan_UPDVER DEFAULT(''),
        UPDUSR          VARCHAR(10)     NOT NULL CONSTRAINT DF_tb_barang_layanan_UPDUSR DEFAULT(''),

        CONSTRAINT PK_tb_barang_layanan PRIMARY KEY CLUSTERED (fs_kd_barang, fs_kd_layanan)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_tb_barang_layanan_Layanan'
      AND object_id = OBJECT_ID('tb_barang_layanan'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tb_barang_layanan_Layanan
        ON tb_barang_layanan (fs_kd_layanan, fs_kd_barang)
        INCLUDE (fn_stok_min, fn_stok_max, fs_kd_rak, fs_ket_lokasi);
END
GO
