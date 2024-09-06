CREATE TABLE ta_rekap_komponen (
    fs_kd_rekap_komponen VARCHAR(12) NOT NULL CONSTRAINT DF_ta_rekap_komponen_fs_kd_rekap_komponen DEFAULT '',
    fs_nm_rekap_komponen VARCHAR(12) NOT NULL CONSTRAINT DF_ta_rekap_komponen_fs_nm_rekap_komponen DEFAULT '',
    fn_urut DECIMAL(3, 0) NOT NULL CONSTRAINT DF_ta_rekap_komponen_fn_urut DEFAULT 0,

    CONSTRAINT PK_ta_rekap_komponen PRIMARY KEY (fs_kd_rekap_komponen)
);
