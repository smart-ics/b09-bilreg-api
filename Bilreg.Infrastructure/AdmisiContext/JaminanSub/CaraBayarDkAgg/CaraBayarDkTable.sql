CREATE TABLE ta_cara_bayar_dk (
    fs_kd_cara_bayar_dk VARCHAR(2) NOT NULL CONSTRAINT DF_ta_cara_bayar_dk_fs_kd_cara_bayar_dk DEFAULT(''),
    fs_nm_cara_bayar_dk VARCHAR(35) NOT NULL CONSTRAINT DF_ta_cara_bayar_dk_fs_nm_cara_bayar_dk DEFAULT(''),
    
    CONSTRAINT PK_ta_cara_bayar_dk PRIMARY KEY CLUSTERED(fs_kd_cara_bayar_dk)
)