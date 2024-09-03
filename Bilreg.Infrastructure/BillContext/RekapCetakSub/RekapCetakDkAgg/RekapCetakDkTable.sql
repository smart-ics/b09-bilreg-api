CREATE TABLE ta_rekap_cetak_dk (
	fs_kd_rekap_cetak_dk VARCHAR(2) NOT NULL CONSTRAINT DC_ta_rekap_cetak_dk_fs_kd_rekap_cetak_dk DEFAULT(''),
	fs_nm_rekap_cetak_dk VARCHAR(15) NOT NULL CONSTRAINT DC_ta_rekap_cetak_dk_fs_nm_rekap_cetak_dk DEFAULT(''),

	CONSTRAINT PK_ta_rekap_cetak_dk PRIMARY KEY CLUSTERED (fs_kd_rekap_cetak_dk)
);
