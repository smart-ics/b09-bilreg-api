CREATE TABLE ta_no_antrian_map_hdr(
    fs_kd_antrian_map VARCHAR(26) NOT NULL CONSTRAINT DF_ta_no_antrian_map_hdr_fs_kd_antrian_map DEFAULT(''),
    fs_kd_jadwal	varchar	(50) NOT NULL CONSTRAINT DF_ta_no_antrian_map_hdr_fs_kd_jadwal DEFAULT(''),
    fs_kd_jadwal_harian varchar(12) NULL,
    fs_kd_dokter	varchar	(50) NOT NULL CONSTRAINT DF_ta_no_antrian_map_hdr_fs_kd_dokter DEFAULT(''),
    fs_kd_layanan	varchar	(50) NOT NULL CONSTRAINT DF_ta_no_antrian_map_hdr_fs_kd_layanan DEFAULT(''),
    fd_tgl_jadwal	datetime NOT NULL CONSTRAINT DF_ta_no_antrian_map_hdr_fd_tgl_jadwal DEFAULT('3000-01-01'),
    fs_jam_jadwal	varchar	(10) NOT NULL CONSTRAINT DF_ta_no_antrian_map_hdr_fs_jam_jadwal DEFAULT('00:00'),
    fs_jam_praktek	varchar	(10) NOT NULL CONSTRAINT DF_ta_no_antrian_map_hdr_fs_jam_praktek DEFAULT('00:00'),
    fs_pattern varchar(256) NOT NULL CONSTRAINT DF_ta_no_antrian_map_hdr_fs_pattern DEFAULT(''),
    fn_max INT NOT NULL CONSTRAINT DF_ta_no_antrian_map_hdr_fn_max DEFAULT(0),
    
    CONSTRAINT PK_ta_no_antrian_map_hdr PRIMARY KEY CLUSTERED (fs_kd_antrian_map)
)