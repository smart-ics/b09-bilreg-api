CREATE TABLE ta_tipe_jaminan(
	fs_kd_tipe_jaminan VARCHAR(5) NOT NULL CONSTRAINT DF_ta_tipe_jaminan_fs_kd_tipe_jaminan DEFAULT(''),
	fs_nm_tipe_jaminan VARCHAR(50) NOT NULL CONSTRAINT DF_ta_tipe_jaminan_fs_nm_tipe_jaminan DEFAULT(''),
	fb_aktif BIT NOT NULL CONSTRAINT DF_ta_tipe_jaminan_fb_aktif DEFAULT(0),
	fs_kd_jaminan VARCHAR(3) NOT NULL CONSTRAINT DF_ta_tipe_jaminan_fs_kd_jaminan DEFAULT('')

	CONSTRAINT PK_ta_tipe_jaminan PRIMARY KEY(fs_kd_tipe_jaminan)
)