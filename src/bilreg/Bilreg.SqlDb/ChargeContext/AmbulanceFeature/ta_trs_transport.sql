CREATE TABLE ta_trs_transport (
    fs_kd_trs VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_trs DEFAULT (''),
    fs_kd_reg VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_reg DEFAULT (''),
    fs_kd_layanan VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_layanan DEFAULT (''),
    fd_tgl_trs VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport_fd_tgl_trs DEFAULT ('3000-01-01'),
    fs_jam_trs VARCHAR(8) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_jam_trs DEFAULT ('00:00:00'),
    fd_tgl_jam_trs VARCHAR(19) NOT NULL CONSTRAINT DF_ta_trs_transport_fd_tgl_jam_trs DEFAULT ('3000-01-01 00:00:00'),
    fs_kd_petugas VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_petugas DEFAULT (''),
    fd_tgl_void VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport_fd_tgl_void DEFAULT ('3000-01-01'),
    fs_jam_void VARCHAR(8) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_jam_void DEFAULT ('00:00:00'),
    fs_kd_petugas_void VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_petugas_void DEFAULT (''),
    fs_kd_transport VARCHAR(3) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_transport DEFAULT (''),
    fs_kd_driver VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_driver DEFAULT (''),
    fs_no_polisi VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_no_polisi DEFAULT (''),
    fs_kd_tujuan_transport VARCHAR(3) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_tujuan_transport DEFAULT (''),
    fn_konstanta DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport_fn_konstanta DEFAULT (0),
    fn_sub_total DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport_fn_sub_total DEFAULT (0),
    fn_diskon DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport_fn_diskon DEFAULT (0),
    fn_total DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport_fn_total DEFAULT (0),
    fn_kas DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport_fn_kas DEFAULT (0),
    fs_kd_rek_kas VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_rek_kas DEFAULT (''),
    fn_bank DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport_fn_bank DEFAULT (0),
    fs_kd_rek_bank VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_rek_bank DEFAULT (''),
    fn_kartu_kredit DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport_fn_kartu_kredit DEFAULT (0),
    fs_kd_rek_kartu_kredit VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_rek_kartu_kredit DEFAULT (''),
    fs_no_kartu_kredit VARCHAR(30) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_no_kartu_kredit DEFAULT (''),
    fs_no_bank VARCHAR(30) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_no_bank DEFAULT (''),
    fs_kd_jenis_kartu_kredit VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_jenis_kartu_kredit DEFAULT (''),
    fs_kd_jenis_bank VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_jenis_bank DEFAULT (''),
    fn_sisa DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport_fn_sisa DEFAULT (0),
    fs_kd_rek_sisa VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_rek_sisa DEFAULT (''),
    fs_kd_petugas_kasir VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_petugas_kasir DEFAULT (''),
    fs_kd_kelas VARCHAR(5) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_kelas DEFAULT (''),
    fs_kd_trs_charge VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_trs_charge DEFAULT (''),
    fs_kd_trs_deposit VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_trs_deposit DEFAULT (''),
    fs_kd_trs_kwitansi VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_trs_kwitansi DEFAULT (''),
    fn_genkeu DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport_fn_genkeu DEFAULT (0),
    fs_kd_tipe_jaminan VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_tipe_jaminan DEFAULT (''),
    fs_nm_pasien VARCHAR(50) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_nm_pasien DEFAULT (''),
    fs_alm1_pasien VARCHAR(100) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_alm1_pasien DEFAULT (''),
    fs_alm2_pasien VARCHAR(100) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_alm2_pasien DEFAULT (''),
    fs_kota_pasien VARCHAR(50) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kota_pasien DEFAULT (''),
    fs_sex VARCHAR(1) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_sex DEFAULT (''),
    fs_keterangan VARCHAR(255) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_keterangan DEFAULT (''),

    -- Extended Ambulance Feature Columns
    fs_status VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_status DEFAULT ('Requested'),
    fs_tipe_perjalanan VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_tipe_perjalanan DEFAULT ('Outbound'),
    fs_metode_tarif VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_metode_tarif DEFAULT ('Wilayah'),
    fs_kd_perawat VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_perawat DEFAULT (''),
    fs_kd_dokter VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_dokter DEFAULT (''),
    fs_lokasi_asal VARCHAR(100) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_lokasi_asal DEFAULT (''),
    fs_lokasi_tujuan VARCHAR(100) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_lokasi_tujuan DEFAULT (''),
    fd_tgl_request VARCHAR(19) NOT NULL CONSTRAINT DF_ta_trs_transport_fd_tgl_request DEFAULT (''),
    fd_tgl_dispatch VARCHAR(19) NOT NULL CONSTRAINT DF_ta_trs_transport_fd_tgl_dispatch DEFAULT (''),
    fd_tgl_arrival VARCHAR(19) NOT NULL CONSTRAINT DF_ta_trs_transport_fd_tgl_arrival DEFAULT (''),
    fd_tgl_return VARCHAR(19) NOT NULL CONSTRAINT DF_ta_trs_transport_fd_tgl_return DEFAULT (''),
    fn_odometer_awal DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport_fn_odometer_awal DEFAULT (0),
    fn_odometer_akhir DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport_fn_odometer_akhir DEFAULT (0),
    fs_kd_jaminan VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_kd_jaminan DEFAULT (''),
    fb_non_reg BIT NOT NULL CONSTRAINT DF_ta_trs_transport_fb_non_reg DEFAULT (0),
    fb_posted_billing BIT NOT NULL CONSTRAINT DF_ta_trs_transport_fb_posted_billing DEFAULT (0),
    fs_alasan_batal VARCHAR(255) NOT NULL CONSTRAINT DF_ta_trs_transport_fs_alasan_batal DEFAULT (''),
    fn_biaya_batal DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport_fn_biaya_batal DEFAULT (0),

    CONSTRAINT PK_ta_trs_transport PRIMARY KEY CLUSTERED (fs_kd_trs)
)
GO

CREATE INDEX IX_ta_trs_transport_fs_kd_reg
    ON ta_trs_transport(fs_kd_reg)
GO

CREATE INDEX IX_ta_trs_transport_fd_tgl_trs
    ON ta_trs_transport(fd_tgl_trs)
GO

CREATE INDEX IX_ta_trs_transport_fs_status
    ON ta_trs_transport(fs_status)
GO

CREATE INDEX IX_ta_trs_transport_fs_kd_transport
    ON ta_trs_transport(fs_kd_transport)
GO
