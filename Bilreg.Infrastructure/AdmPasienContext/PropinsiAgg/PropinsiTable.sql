CREATE TABLE ta_propinsi(
    fs_kd_propinsi VARCHAR(2) NOT NULL CONSTRAINT DF_ta_propinsi_fs_kd_propinsi DEFAULT(''),
    fs_nm_propinsi VARCHAR(30) NOT NULL CONSTRAINT DF_ta_propinsi_fs_nm_propinsi DEFAULT(''),
    
    CONSTRAINT PK_ta_propinsi PRIMARY KEY(fs_kd_propinsi)
)