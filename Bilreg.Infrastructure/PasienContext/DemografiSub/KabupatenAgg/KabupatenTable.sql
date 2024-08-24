CREATE TABLE ta_kabupaten(
    fs_kd_kabupaten VARCHAR(4) NOT NULL CONSTRAINT DF_ta_kabupaten_fs_kd_kabuapten DEFAULT(''),
    fs_nm_kabupaten VARCHAR(30) NOT NULl CONSTRAINT DF_ta_kabupaten_fs_nm_kabupaten DEFAULT(''),
    
    CONSTRAINT PK_ta_kabupaten PRIMARY KEY CLUSTERED(fs_kd_kabupaten)
)
GO

