-- Migration: IGD-AMBULANCE (Perluasan Skema Tabel Legacy Transport & Transaksi Ambulans)
-- Slice: P1-S01
-- Target Database: HOSPITAL_PKL / Production Legacy DB

-- ==========================================================================================
-- 1. Perluasan Tabel Master ta_transport
-- ==========================================================================================
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ta_transport')
BEGIN
    -- 1.1 fs_no_polisi
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('ta_transport') AND name = 'fs_no_polisi'
    )
    BEGIN
        ALTER TABLE ta_transport 
        ADD fs_no_polisi VARCHAR(20) NOT NULL CONSTRAINT DF_ta_transport_fs_no_polisi DEFAULT ('');
    END

    -- 1.2 fs_tipe_armada (Transport, ALS, Jenazah)
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('ta_transport') AND name = 'fs_tipe_armada'
    )
    BEGIN
        ALTER TABLE ta_transport 
        ADD fs_tipe_armada VARCHAR(20) NOT NULL CONSTRAINT DF_ta_transport_fs_tipe_armada DEFAULT ('Transport');
    END

    -- 1.3 fs_status_armada (Available, InUse, Maintenance)
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('ta_transport') AND name = 'fs_status_armada'
    )
    BEGIN
        ALTER TABLE ta_transport 
        ADD fs_status_armada VARCHAR(20) NOT NULL CONSTRAINT DF_ta_transport_fs_status_armada DEFAULT ('Available');
    END
END
GO

-- Backfill data historis master ta_transport
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ta_transport')
BEGIN
    UPDATE ta_transport
    SET fs_tipe_armada = 'Transport'
    WHERE fs_tipe_armada IS NULL OR fs_tipe_armada = '';

    UPDATE ta_transport
    SET fs_status_armada = 'Available'
    WHERE fs_status_armada IS NULL OR fs_status_armada = '';
END
GO

-- ==========================================================================================
-- 2. Pembuatan Tabel Master ta_transport3 (Jika belum ada)
-- ==========================================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ta_transport3')
BEGIN
    CREATE TABLE ta_transport3 (
        fs_kd_transport VARCHAR(3) NOT NULL CONSTRAINT DF_ta_transport3_fs_kd_transport DEFAULT (''),
        fs_kd_tujuan_transport VARCHAR(3) NOT NULL CONSTRAINT DF_ta_transport3_fs_kd_tujuan_transport DEFAULT (''),
        fn_konstanta DECIMAL(20) NOT NULL CONSTRAINT DF_ta_transport3_fn_konstanta DEFAULT (0),
        
        CONSTRAINT PK_ta_transport3 PRIMARY KEY CLUSTERED (fs_kd_transport, fs_kd_tujuan_transport)
    );

    CREATE INDEX IX_ta_transport3_fs_kd_transport
        ON ta_transport3(fs_kd_transport);
END
GO

-- ==========================================================================================
-- 3. Perluasan Tabel Transaksi ta_trs_transport
-- ==========================================================================================
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ta_trs_transport')
BEGIN
    -- 3.1 fs_status (Requested, Dispatched, Completed, Cancelled)
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fs_status')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fs_status VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_status DEFAULT ('Requested');
    END

    -- 3.2 fs_tipe_perjalanan (Inbound, Outbound, Discharge, Hearse)
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fs_tipe_perjalanan')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fs_tipe_perjalanan VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_tipe_perjalanan DEFAULT ('Outbound');
    END

    -- 3.3 fs_metode_tarif (Wilayah, Jarak)
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fs_metode_tarif')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fs_metode_tarif VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_metode_tarif DEFAULT ('Wilayah');
    END

    -- 3.4 fs_kd_perawat
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fs_kd_perawat')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fs_kd_perawat VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_perawat DEFAULT ('');
    END

    -- 3.5 fs_kd_dokter
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fs_kd_dokter')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fs_kd_dokter VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_dokter DEFAULT ('');
    END

    -- 3.6 fs_lokasi_asal
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fs_lokasi_asal')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fs_lokasi_asal VARCHAR(100) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_lokasi_asal DEFAULT ('');
    END

    -- 3.7 fs_lokasi_tujuan
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fs_lokasi_tujuan')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fs_lokasi_tujuan VARCHAR(100) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_lokasi_tujuan DEFAULT ('');
    END

    -- 3.8 Logistik Waktu: fd_tgl_request, fd_tgl_dispatch, fd_tgl_arrival, fd_tgl_return
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fd_tgl_request')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fd_tgl_request VARCHAR(19) NOT NULL CONSTRAINT DF_ta_trs_transport_fd_tgl_request DEFAULT ('');
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fd_tgl_dispatch')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fd_tgl_dispatch VARCHAR(19) NOT NULL CONSTRAINT DF_ta_trs_transport_fd_tgl_dispatch DEFAULT ('');
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fd_tgl_arrival')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fd_tgl_arrival VARCHAR(19) NOT NULL CONSTRAINT DF_ta_trs_transport_fd_tgl_arrival DEFAULT ('');
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fd_tgl_return')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fd_tgl_return VARCHAR(19) NOT NULL CONSTRAINT DF_ta_trs_transport_fd_tgl_return DEFAULT ('');
    END

    -- 3.9 Odometer: fn_odometer_awal, fn_odometer_akhir
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fn_odometer_awal')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fn_odometer_awal DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport_fn_odometer_awal DEFAULT (0);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fn_odometer_akhir')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fn_odometer_akhir DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport_fn_odometer_akhir DEFAULT (0);
    END

    -- 3.10 fs_kd_jaminan
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fs_kd_jaminan')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fs_kd_jaminan VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_jaminan DEFAULT ('');
    END

    -- 3.11 fb_non_reg
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fb_non_reg')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fb_non_reg BIT NOT NULL CONSTRAINT DF_ta_trs_transport_fb_non_reg DEFAULT (0);
    END

    -- 3.12 fb_posted_billing
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fb_posted_billing')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fb_posted_billing BIT NOT NULL CONSTRAINT DF_ta_trs_transport_fb_posted_billing DEFAULT (0);
    END

    -- 3.13 fs_alasan_batal & fn_biaya_batal
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fs_alasan_batal')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fs_alasan_batal VARCHAR(255) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_alasan_batal DEFAULT ('');
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'fn_biaya_batal')
    BEGIN
        ALTER TABLE ta_trs_transport
        ADD fn_biaya_batal DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport_fn_biaya_batal DEFAULT (0);
    END

    -- Indeks performa kueri
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('ta_trs_transport') AND name = 'IX_ta_trs_transport_fs_status')
    BEGIN
        CREATE INDEX IX_ta_trs_transport_fs_status ON ta_trs_transport(fs_status);
    END
END
GO

-- Backfill data historis transaksi ta_trs_transport
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ta_trs_transport')
BEGIN
    -- Transaksi historis yang sudah ada ditandai sebagai Completed atau Cancelled (bila void)
    UPDATE ta_trs_transport
    SET fs_status = CASE 
            WHEN fd_tgl_void <> '3000-01-01' AND fd_tgl_void <> '' THEN 'Cancelled'
            ELSE 'Completed'
        END,
        fb_posted_billing = CASE 
            WHEN fd_tgl_void <> '3000-01-01' AND fd_tgl_void <> '' THEN 0
            ELSE 1
        END
    WHERE fs_status IS NULL OR fs_status = '' OR fs_status = 'Requested';

    UPDATE ta_trs_transport
    SET fs_metode_tarif = 'Wilayah'
    WHERE fs_metode_tarif IS NULL OR fs_metode_tarif = '';

    UPDATE ta_trs_transport
    SET fs_tipe_perjalanan = 'Outbound'
    WHERE fs_tipe_perjalanan IS NULL OR fs_tipe_perjalanan = '';
END
GO

-- ==========================================================================================
-- 4. Perluasan Tabel Transaksi Detail ta_trs_transport2
-- ==========================================================================================
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ta_trs_transport2')
BEGIN
    -- 4.1 fb_void
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport2') AND name = 'fb_void')
    BEGIN
        ALTER TABLE ta_trs_transport2
        ADD fb_void BIT NOT NULL CONSTRAINT DF_ta_trs_transport2_fb_void DEFAULT (0);
    END

    -- 4.2 fs_keterangan
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ta_trs_transport2') AND name = 'fs_keterangan')
    BEGIN
        ALTER TABLE ta_trs_transport2
        ADD fs_keterangan VARCHAR(100) NOT NULL CONSTRAINT DF_ta_trs_transport2_fs_keterangan DEFAULT ('');
    END
END
GO
