CREATE TABLE ta_suku(
	fs_kd_suku VARCHAR(3) NOT NULL CONSTRAINT DF_ta_suku_fs_kd_suku DEFAULT(''),
	fs_nm_suku VARCHAR(20) NOT NULL CONSTRAINT DF_ta_suku_fs_nm_suku DEFAULT(''),

	CONSTRAINT PK_ta_suku PRIMARY KEY CLUSTERED(fs_kd_suku)
)