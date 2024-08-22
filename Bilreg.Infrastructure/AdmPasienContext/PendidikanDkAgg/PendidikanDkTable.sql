CREATE TABLE ta_pendidikan_dk(
    fs_kd_pendidikan_dk VARCHAR(1) NOT NULL CONSTRAINT DF_ta_pendidikan_dk_fs_kd_pendidikan_dk DEFAULT(''),
    fs_nm_pendidikan_dk VARCHAR(30) NOT NULL CONSTRAINT DF_ta_pendidikan_dk_fs_nm_pendidikan_dk DEFAULT(''),
    
    CONSTRAINT PK_ta_pendidikan_dk PRIMARY KEY CLUSTERED(fs_kd_pendidikan_dk)
)