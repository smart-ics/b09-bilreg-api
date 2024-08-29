CREATE TABLE ta_kelurahan (
    fs_kd_kelurahan VARCHAR(10) NOT NULL CONSTRAINT DF_ta_kelurahan_fs_kd_kelurahan DEFAULT(''),
    fs_nm_kelurahan VARCHAR(255) NOT NULL CONSTRAINT DF_ta_kelurahan_fs_nm_kelurahan DEFAULT(''),
    fs_kd_kecamatan VARCHAR(7) NOT NULL CONSTRAINT DF_ta_kelurahan_fs_kd_kecamatan DEFAULT(''),
    fs_kd_pos VARCHAR(5) NOT NULL CONSTRAINT DF_ta_kelurahan_fs_kd_pos DEFAULT('')
    
    CONSTRAINT PK_ta_kelurahan PRIMARY KEY CLUSTERED(fs_kd_kelurahan)
)