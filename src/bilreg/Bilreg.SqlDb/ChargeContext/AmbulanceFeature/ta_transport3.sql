CREATE TABLE ta_transport3 (
    fs_kd_transport VARCHAR(3) NOT NULL CONSTRAINT DF_ta_transport3_fs_kd_transport DEFAULT (''),
    fs_kd_tujuan_transport VARCHAR(3) NOT NULL CONSTRAINT DF_ta_transport3_fs_kd_tujuan_transport DEFAULT (''),
    fn_konstanta DECIMAL(20) NOT NULL CONSTRAINT DF_ta_transport3_fn_konstanta DEFAULT (0),
    
    CONSTRAINT PK_ta_transport3 PRIMARY KEY CLUSTERED (fs_kd_transport, fs_kd_tujuan_transport)
)
GO

CREATE INDEX IX_ta_transport3_fs_kd_transport
    ON ta_transport3(fs_kd_transport)
GO
