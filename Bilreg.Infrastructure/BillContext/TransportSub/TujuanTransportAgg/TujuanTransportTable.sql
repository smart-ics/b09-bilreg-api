CREATE TABLE ta_tujuan_transport (
    fs_kd_tujuan_transport VARCHAR(3) NOT NULL CONSTRAINT DF_ta_tujuan_transport_fs_kd_tujuan_transport DEFAULT (''),
    fs_nm_tujuan_transport VARCHAR(30) NOT NULL CONSTRAINT DF_ta_tujuan_transport_fs_nm_tujuan_transport DEFAULT (''),
    fn_konstanta DECIMAL(20) NOT NULL CONSTRAINT DF_ta_tujuan_transport_fn_konstanta DEFAULT (0),
    fb_perkiraan BIT NOT NULL CONSTRAINT DF_ta_tujuan_transport_fb_perkiraan DEFAULT (0),
    fs_kd_default_transport VARCHAR(3) NOT NULL CONSTRAINT DF_ta_tujuan_transport_fs_kd_default_transport DEFAULT (''),
    
    CONSTRAINT PK_ta_tujuan_transport PRIMARY KEY CLUSTERED (fs_kd_tujuan_transport)
)