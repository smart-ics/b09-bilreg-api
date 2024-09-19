CREATE   TABLE ta_rekap_cetak_tarif(
       fs_kd_rekap_cetak_tarif VARCHAR(2) NOT NULL CONSTRAINT DF_ta_rekap_cetak_tarif_fs_kd_rekap_cetak_tarif DEFAULT(''),
       fs_nm_rekap_cetak_tarif VARCHAR(30) NOT NULL CONSTRAINT DF_ta_rekap_cetak_tarif_fs_nm_rekap_cetak_tarif DEFAULT(''),
       fs_urut VARCHAR(2) NOT NULL CONSTRAINT DF_ta_rekap_cetak_tarif_fs_urut DEFAULT(''),
       fb_grup_baru BIT NOT NULL CONSTRAINT DF_ta_rekap_cetak_tarif_fb_grup_baru DEFAULT(0),
       fn_level DECIMAL(18,0) NOT NULL CONSTRAINT DF_ta_rekap_cetak_tarif_fn_level DEFAULT(0),
       fs_kd_grup_rekap_cetak VARCHAR(2) NOT NULL CONSTRAINT DF_ta_rekap_cetak_tarif_fs_kd_grup_rekap_cetak DEFAULT(''),
       fs_kd_rekap_cetak_dk VARCHAR(2) NOT NULL CONSTRAINT DF_ta_rekap_cetak_tarif_fs_kd_rekap_cetak_dk DEFAULT(''),
       
       CONSTRAINT PK_ta_rekap_cetak_tarif PRIMARY KEY CLUSTERED(fs_kd_rekap_cetak_tarif)
)