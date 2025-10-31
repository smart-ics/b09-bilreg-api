CREATE TABLE ta_jaminan (
    fs_kd_jaminan VARCHAR(3) NOT NULL CONSTRAINT DF_ta_jaminan_fs_kd_jaminan DEFAULT (''),
    fs_nm_jaminan VARCHAR(64) NOT NULL CONSTRAINT DF_ta_jaminan_fs_nm_jaminan DEFAULT (''),
    fs_alm1_jaminan VARCHAR(40) NOT NULL CONSTRAINT DF_ta_jaminan_fs_alm1_jaminan DEFAULT (''),
    fs_alm2_jaminan VARCHAR(40) NOT NULL CONSTRAINT DF_ta_jaminan_fs_alm2_jaminan DEFAULT (''),
    fs_kota_jaminan VARCHAR(20) NOT NULL CONSTRAINT DF_ta_jaminan_fs_kota_jaminan DEFAULT (''),
    fb_aktif BIT NOT NULL CONSTRAINT DF_ta_jaminan_fb_aktif DEFAULT (0),
    fs_kd_cara_bayar_dk VARCHAR(2) NOT NULL CONSTRAINT DF_ta_jaminan_fs_kd_cara_bayar_dk DEFAULT (''),
    fs_kd_grup_jaminan VARCHAR(3) NOT NULL CONSTRAINT DF_ta_jaminan_fs_kd_grup_jaminan DEFAULT (''),
    fs_benefit_mou VARCHAR(8000) NOT NULL CONSTRAINT DF_ta_jaminan_fs_benefit_mou DEFAULT (''),
    
    CONSTRAINT PK_ta_jaminan PRIMARY KEY CLUSTERED (fs_kd_jaminan)
)