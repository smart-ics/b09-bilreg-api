CREATE TABLE ta_jaminan
(
    fs_kd_jaminan                    VARCHAR(3) NOT NULL CONSTRAINT DF_ta_jaminan_fs_kd_jaminan DEFAULT(''),
    fs_nm_jaminan                    VARCHAR(64) NOT NULL CONSTRAINT DF_ta_jaminan_fs_nm_jaminan DEFAULT(''),
    fb_aktif                         BIT NOT NULL CONSTRAINT DF_ta_jaminan_fb_aktif DEFAULT(0),
    fs_alm1_jaminan                  VARCHAR(40) NOT NULL CONSTRAINT DF_ta_jaminan_fs_alm1_jaminan DEFAULT(''),
    fs_alm2_jaminan                  VARCHAR(40) NOT NULL CONSTRAINT DF_ta_jaminan_fs_alm2_jaminan DEFAULT(''),
    fs_kota_jaminan                  VARCHAR(20) NOT NULL CONSTRAINT DF_ta_jaminan_fs_kota_jaminan DEFAULT(''),

    fs_kd_cara_bayar_dk              VARCHAR(2) NOT NULL CONSTRAINT DF_ta_jaminan_fs_kd_cara_bayar_dk DEFAULT(''),
    fs_kd_grup_jaminan               VARCHAR(3) NOT NULL CONSTRAINT DF_ta_jaminan_fs_kd_grup_jaminan DEFAULT(''),
    fs_kd_tipe_tarif_rawat_jalan     VARCHAR(2) NOT NULL CONSTRAINT DF_ta_jaminan_fs_kd_tipe_tarif_rawat_jalan DEFAULT(''),
    fs_kd_tipe_tarif_rawat_inap      VARCHAR(2) NOT NULL CONSTRAINT DF_ta_jaminan_fs_kd_tipe_tarif_rawat_inap DEFAULT(''),
    fs_piut_rawat                    VARCHAR(20) NOT NULL CONSTRAINT DF_ta_jaminan_fs_piut_rawat DEFAULT(''),
    fs_piut_obat_rawat               VARCHAR(20) NOT NULL CONSTRAINT DF_ta_jaminan_fs_piut_obat_rawat DEFAULT(''),
    fs_kd_rek_ppdp_jasa_ri           VARCHAR(20) NOT NULL CONSTRAINT DF_ta_jaminan_fs_kd_rek_ppdp_jasa_ri DEFAULT(''),
    fs_kd_rek_ppdp_obat_ri           VARCHAR(20) NOT NULL CONSTRAINT DF_ta_jaminan_fs_kd_rek_ppdp_obat_ri DEFAULT(''),

    CONSTRAINT PK_ta_jaminan PRIMARY KEY CLUSTERED(fs_kd_jaminan)
)