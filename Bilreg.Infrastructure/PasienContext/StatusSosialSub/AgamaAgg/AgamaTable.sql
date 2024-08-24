CREATE TABLE ta_agama(
	fs_kd_agama VARCHAR(1) NOT NULL CONSTRAINT DF_ta_agama_fs_kd_agama DEFAULT(''),
	fs_nm_agama VARCHAR(25) NOT NULL CONSTRAINT DF_ta_agama_fs_nm_agama DEFAULT(''),

	CONSTRAINT PK_ta_agama PRIMARY KEY CLUSTERED(fs_kd_agama)
)