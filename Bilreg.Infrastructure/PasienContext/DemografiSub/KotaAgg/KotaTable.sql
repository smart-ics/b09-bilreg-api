CREATE TABLE ta_kota (
    fs_kd_kota VARCHAR(3) NOT NULL CONSTRAINT DF_ta_kota_fs_kd_kota DEFAULT (''),
    fs_nm_kota VARCHAR(20) NOT NULL CONSTRAINT DF_ta_kota_fs_nm_kota DEFAULT (''),
    
    CONSTRAINT PK_ta_kota PRIMARY KEY CLUSTERED (fs_kd_kota)
)