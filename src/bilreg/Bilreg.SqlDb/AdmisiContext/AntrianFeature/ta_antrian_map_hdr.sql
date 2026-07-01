CREATE TABLE ta_no_antrian_map_hdr
(
    fs_kd_jadwal     VARCHAR(50) NOT NULL CONSTRAINT DF_ta_no_antrian_map_hdr_fs_kd_jadwal DEFAULT(''),
    fs_kd_dokter     VARCHAR(50) NOT NULL CONSTRAINT DF_ta_no_antrian_map_hdr_fs_kd_dokter DEFAULT(''),
    fs_kd_layanan    VARCHAR(50) NOT NULL CONSTRAINT DF_ta_no_antrian_map_hdr_fs_kd_layanan DEFAULT(''),
    fd_tgl_jadwal    DATETIME    NOT NULL CONSTRAINT DF_ta_no_antrian_map_hdr_fd_tgl_jadwal DEFAULT('3000-01-01'),
    fs_jam_jadwal    VARCHAR(10) NOT NULL CONSTRAINT DF_ta_no_antrian_map_hdr_fs_jam_jadwal DEFAULT(''),
    fs_jam_praktek   VARCHAR(10) NOT NULL CONSTRAINT DF_ta_no_antrian_map_hdr_fs_jam_praktek DEFAULT(''),

	CONSTRAINT PK_ta_no_antrian_map_hdr PRIMARY KEY CLUSTERED (fs_kd_jadwal, fs_kd_dokter, fs_kd_layanan, fd_tgl_jadwal, fs_jam_jadwal)
)
