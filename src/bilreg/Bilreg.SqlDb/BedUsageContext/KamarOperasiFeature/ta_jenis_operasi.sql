CREATE TABLE ta_Jenis_operasi(
    fs_kd_jenis_operasi VARCHAR(2) NOT NULL CONSTRAINT DF_ta_jenis_operasi_fs_kd_jenis_operasi DEFAULT(''),
    fs_nm_jenis_operasi VARCHAR(30) NOT NULL CONSTRAINT DF_ta_jenis_operasi_fs_nm_jenis_operasi DEFAULT('')
    
    CONSTRAINT PK_ta_jenis_operasi PRIMARY KEY CLUSTERED(fs_kd_jenis_operasi)
)