CREATE TABLE dbo.ta_no_antrian_map
(
    fs_kd_antrian_map  VARCHAR(26)  NOT NULL CONSTRAINT DF_ta_no_antrian_map_fs_kd_antrian_map DEFAULT(''), 
    fs_kd_dokter       VARCHAR(10) NOT NULL CONSTRAINT DF_ta_no_antrian_map_fs_kd_dokter DEFAULT(''),  
    fs_kd_layanan      VARCHAR(5) NOT NULL CONSTRAINT DF_ta_no_antrian_map_fs_kd_layanan DEFAULT(''),   
    fd_tgl_jadwal      VARCHAR(10) NOT NULL CONSTRAINT DF_ta_no_antrian_map_fd_tgl_jadwal DEFAULT('3000-01-01'),  
    fs_jam_jadwal      VARCHAR(8) NOT NULL CONSTRAINT DF_ta_no_antrian_map_fs_jam_jadwal DEFAULT('00:00:00'),   
    fn_no_antrian      DECIMAL(20,0) NOT NULL CONSTRAINT DF_ta_no_antrian_map_fn_no_antrian DEFAULT(0),     
    fs_flag            VARCHAR(15) NOT NULL CONSTRAINT DF_ta_no_antrian_map_fs_flag DEFAULT(''),  
    fb_terpakai        BIT NOT NULL CONSTRAINT DF_ta_no_antrian_map_fb_terpakai DEFAULT(0),               
    fs_mr              VARCHAR(15) NOT NULL CONSTRAINT DF_ta_no_antrian_map_fs_mr DEFAULT(''),  
    FS_NM_PASIEN       VARCHAR(100) NOT NULL CONSTRAINT DF_ta_no_antrian_map_FS_NM_PASIEN DEFAULT(''), 
    fs_kd_trs_gen      VARCHAR(26) NOT NULL CONSTRAINT DF_ta_no_antrian_map_fs_kd_trs_gen DEFAULT(''),  
    fs_ket_trs_gen     VARCHAR(15) NOT NULL CONSTRAINT DF_ta_no_antrian_map_fs_ket_trs_gen DEFAULT(''),  
    fn_status_antrian  DECIMAL(18,0) NOT NULL CONSTRAINT DF_ta_no_antrian_map_fn_status_antrian DEFAULT(''), 
    CRTTGL             VARCHAR(10) NOT NULL CONSTRAINT DF_ta_no_antrian_map_CRTTGL DEFAULT('3000-01-01'),  
    CRTJAM             VARCHAR(8) NOT NULL CONSTRAINT DF_ta_no_antrian_map_CRTJAM DEFAULT('00:00:00'),   
    CRTIPA             VARCHAR(40) NOT NULL CONSTRAINT DF_ta_no_antrian_map_CRTIPA DEFAULT(''),  
    CRTVER             VARCHAR(12) NOT NULL CONSTRAINT DF_ta_no_antrian_map_CRTVER DEFAULT(''),  
    CRTUSR             VARCHAR(10) NOT NULL CONSTRAINT DF_ta_no_antrian_map_CRTUSR DEFAULT(''),  
    UPDTGL             VARCHAR(10) NOT NULL CONSTRAINT DF_ta_no_antrian_map_UPDTGL DEFAULT('3000-01-01'),  
    UPDJAM             VARCHAR(8) NOT NULL CONSTRAINT DF_ta_no_antrian_map_UPDJAM DEFAULT('00:00:00'),   
    UPDIPA             VARCHAR(40) NOT NULL CONSTRAINT DF_ta_no_antrian_map_UPDIPA DEFAULT(''),  
    UPDVER             VARCHAR(12) NOT NULL CONSTRAINT DF_ta_no_antrian_map_UPDVER DEFAULT(''),  
    UPDUSR             VARCHAR(10) NOT NULL CONSTRAINT DF_ta_no_antrian_map_UPDUSR DEFAULT(''),
    
    CONSTRAINT PK_ta_no_antrian_map PRIMARY KEY CLUSTERED (fs_kd_antrian_map, fn_no_antrian)
);
