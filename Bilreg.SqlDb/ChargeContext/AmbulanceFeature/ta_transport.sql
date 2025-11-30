CREATE TABLE ta_transport (
    fs_kd_transport VARCHAR(3) NOT NULL CONSTRAINT DF_ta_transport_fs_kd_transport DEFAULT (''),
    fs_nm_transport VARCHAR(30) NOT NULL CONSTRAINT DF_ta_transport_fs_nm_transport DEFAULT (''),
    fb_aktif BIT NOT NULL CONSTRAINT DF_ta_transport_fb_aktif DEFAULT (0),
    fn_abonemen DECIMAL(20) NOT NULL CONSTRAINT DF_ta_transport_fn_abonemen DEFAULT (0),
    
    CONSTRAINT PK_ta_transport PRIMARY KEY CLUSTERED (fs_kd_transport)
)