CREATE TABLE tz_parameter_no(
    fs_kd_parameter VARCHAR(30) NOT NULL CONSTRAINT DF_tz_parameter_no_fs_kd_parameter DEFAULT(''),
    fs_nm_parameter VARCHAR(255) NOT NULL CONSTRAINT DF_tz_parameter_no_fs_nm_parameter DEFAULT(''),
    fn_value DECIMAL(18,0) NOT NULL CONSTRAINT DF_tz_parameter_no_fn_value DEFAULT(0)
    
    CONSTRAINT PK_tz_parameter_no PRIMARY KEY CLUSTERED (fs_kd_parameter)
)
GO

