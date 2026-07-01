CREATE TABLE ta_cara_masuk_dk(
    fs_kd_cara_masuk_dk VARCHAR(1) NOT NULL CONSTRAINT DF_ta_cara_masuk_dk_fs_kd_cara_masuk_dk DEFAULT(''),
    fs_nm_cara_masuk_dk VARCHAR(20) NOT NULL CONSTRAINT DF_ta_cara_masuk_dk_fs_nm_cara_masuk_dk DEFAULT(''),

    CONSTRAINT PK_ta_cara_masuk_dk PRIMARY KEY CLUSTERED(fs_kd_cara_masuk_dk)
)
