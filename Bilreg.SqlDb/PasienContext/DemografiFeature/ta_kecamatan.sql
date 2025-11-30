CREATE TABLE ta_kecamatan (
    fs_kd_kecamatan VARCHAR(7) NOT NULL CONSTRAINT DF_ta_kecamatan_fs_kd_kecamatan DEFAULT(''),
    fs_nm_kecamatan VARCHAR(255) NOT NULL CONSTRAINT DF_ta_kecamatan_fs_nm_kecamatan DEFAULT(''),
    fs_kd_kabupaten VARCHAR(4) NOT NULL CONSTRAINT DF_ta_kecamatan_fs_kd_kabupaten DEFAULT(''),
    
    CONSTRAINT PK_ta_kecamatan PRIMARY KEY CLUSTERED(fs_kd_kecamatan)
)