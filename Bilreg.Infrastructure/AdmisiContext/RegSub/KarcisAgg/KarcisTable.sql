CREATE TABLE ta_karcis (
    fs_kd_karcis VARCHAR(2) NOT NULL CONSTRAINT DF_ta_karcis_fs_kd_karcis DEFAULT (''),
    fs_nm_karcis VARCHAR(30) NOT NULL CONSTRAINT DF_ta_karcis_fs_nm_karcis DEFAULT (''),
    fn_karcis DECIMAL NOT NULL CONSTRAINT DF_ta_karcis_fn_karcis DEFAULT (0),
    fs_kd_instalasi_dk VARCHAR(1) NOT NULL CONSTRAINT DF_ta_karcis_fs_kd_instalasi_dk DEFAULT (''),
    fs_kd_rekap_cetak VARCHAR(2) NOT NULL CONSTRAINT DF_ta_karcis_fs_kd_rekap_cetak DEFAULT (''),
    fs_kd_tarif VARCHAR(10) NOT NULL CONSTRAINT DF_ta_karcis_fs_kd_tarif DEFAULT (''),
    fb_aktif BIT NOT NULL CONSTRAINT DF_ta_karcis_fb_aktif DEFAULT (0),
    
    CONSTRAINT PK_ta_karcis PRIMARY KEY CLUSTERED (fs_kd_karcis)
)