CREATE TABLE ta_status_kawin_dk(
	fs_kd_status_kawin_dk VARCHAR(12) NOT NULL CONSTRAINT DF_fs_kd_status_kawin_dk DEFAULT(''),
	fs_nm_status_kawin_dk VARCHAR(12) NOT NULL CONSTRAINT DF_fs_nm_status_kawin_dk DEFAULT(''),

	CONSTRAINT PK_ta_status_kawin_dk PRIMARY KEY CLUSTERED(fs_kd_status_kawin_dk)
)