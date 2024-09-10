CREATE TABLE TA_LAYANAN_DK (
    fs_kd_layanan_dk VARCHAR(3) NOT NULL      CONSTRAINT DF_ta_layanan_dk_fs_kd_layanan_dk DEFAULT(''),
    fs_nm_layanan_dk VARCHAR(30) NOT NULL     CONSTRAINT DF_ta_layanan_dk_fs_nm_layanan_dk DEFAULT(''),
    fn_rawat_inap DECIMAL(18, 0) NOT NULL     CONSTRAINT DF_ta_layanan_dk_fn_rawat_inap DEFAULT 0,
    fn_rawat_jalan DECIMAL(18, 0) NOT NULL    CONSTRAINT DF_ta_layanan_dk_fn_rawat_jalan DEFAULT 0,
    fn_kesehatan_jiwa DECIMAL(18, 0) NOT NULL CONSTRAINT DF_ta_layanan_dk_fn_kesehatan_jiwa DEFAULT 0,
    fn_bedah DECIMAL(18, 0) NOT NULL          CONSTRAINT DF_ta_layanan_dk_fn_bedah DEFAULT 0,
    fn_rujukan DECIMAL(18, 0) NOT NULL        CONSTRAINT DF_ta_layanan_dk_fn_rujukan DEFAULT 0,
    fn_kunj_rumah DECIMAL(18, 0) NOT NULL     CONSTRAINT DF_ta_layanan_dk_fn_kunj_rumah DEFAULT 0,
    fn_layanan_sub DECIMAL(18, 0) NOT NULL    CONSTRAINT DF_ta_layanan_dk_fn_layanan_sub DEFAULT 0
    
    CONSTRAINT PK_TA_LAYANAN_DK PRIMARY KEY (fs_kd_layanan_dk)
);