CREATE TABLE tz_parameter_sistem(
    fs_kd_parameter VARCHAR(30) NOT NULL CONSTRAINT DF_tz_parameter_sistem_fs_kd_parameter DEFAULT(''),
    fs_nm_parameter VARCHAR(255) NOT NULL CONSTRAINT DF_tz_parameter_sistem_fs_nm_parameter DEFAULT(''),
    fs_value VARCHAR(128) NOT NULL CONSTRAINT DF_tz_parameter_sistem_fs_value DEFAULT('')
    
    CONSTRAINT PK_tz_parameter_sistem PRIMARY KEY CLUSTERED (fs_kd_parameter)
)
GO
