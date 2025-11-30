CREATE TABLE ta_grup_rekap_cetak (
		fs_kd_grup_rekap_cetak VARCHAR (2) NOT NULL CONSTRAINT DC_ta_grup_rekap_cetak_fs_kd_grup_rekap_cetak DEFAULT(''),
		fs_nm_grup_rekap_cetak VARCHAR (30) NOT NULL CONSTRAINT DC_ta_grup_rekap_cetak_fs_nm_grup_rekap_cetak DEFAULT(''),

		CONSTRAINT PK_ta_grup_rekap_cetak PRIMARY KEY CLUSTERED (fs_kd_grup_rekap_cetak)
);