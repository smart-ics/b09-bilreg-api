CREATE TABLE ta_layanan(
    fs_kd_layanan VARCHAR(5) NOT NULL CONSTRAINT DF_ta_layanan_fs_kd_layanan DEFAULT(''),
    fs_nm_layanan VARCHAR(30) NOT NULL CONSTRAINT DF_ta_layanan_fs_nm_layanan DEFAULT(''),
    
    CONSTRAINT PK_ta_layanan PRIMARY KEY CLUSTERED(fs_kd_layanan)
)