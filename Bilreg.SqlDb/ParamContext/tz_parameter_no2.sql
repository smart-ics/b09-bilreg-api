CREATE TABLE tz_parameter_no2(
    fs_prefix VARCHAR(2) NOT NULL CONSTRAINT DF_tz_parameter_no2_fs_prefix DEFAULT (''),
    fn_value DECIMAL(18,0) NOT NULL CONSTRAINT DF_tz_parameter_no2_fn_value DEFAULT (0),
    fs_modul VARCHAR(10) NOT NULl CONSTRAINT DF_tz_parameter_no2_fs_modul DEFAULT ('')
    
    CONSTRAINT PK_tz_parameter_no2 PRIMARY KEY(fs_prefix)
)